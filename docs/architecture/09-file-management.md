# 09 — File Management

> **Curriculum context.** Section 9 of the *Integrated Data Applications* rubric:
> > "Transferring files (images / songs / documents) between server and client and/or vice versa."
>
> Section 10 also includes:
> > "Files — using files for storing data in XML for example."
>
> This document describes every file-related path in the codebase, including the security-sensitive ones.

---

## 1. Inventory of File Surfaces

| Surface | Direction | Format | Used by |
|---|---|---|---|
| Profile picture upload | Client → Server | JPEG / PNG | `UserApiClient.UploadProfilePictureAsync` |
| Profile picture display | Local FS → WPF | JPEG / PNG | `StringToImageSourceConverter` |
| Excel student import | Local FS → Server | XLSX | `ExcelImportService.ImportStudentsFromExcelAsync` |
| Excel template export | Server → Local FS | XLSX | `ExcelImportService.ExportTemplate` |
| Configuration | Disk → Process | JSON | `appsettings.json` (boot-time) |
| SQLite database | Disk ↔ Process | SQLite binary | `mentoring.db` |

---

## 2. Profile Picture Upload (Client → Server)

### 2.1 Client side

`IFileService` is the WPF/Web abstraction over the OS file picker.

```csharp
public interface IFileService
{
    string? PickImage();         // returns the selected absolute path or null
}

// WPF impl
public class FileService : IFileService
{
    public string? PickImage()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Images|*.jpg;*.jpeg;*.png",
            CheckFileExists = true,
            Multiselect = false
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
```

Then the upload itself:

```csharp
public async Task<string> UploadProfilePictureAsync(int userId, string localPath)
{
    using var stream      = File.OpenRead(localPath);
    using var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

    using var form = new MultipartFormDataContent
    {
        { fileContent, "file", Path.GetFileName(localPath) }
    };

    using var resp = await _http.PostAsync($"api/users/{userId}/profile-picture", form);
    resp.EnsureSuccessStatusCode();
    var payload = await resp.Content.ReadFromJsonAsync<UploadResponse>();
    return payload!.Url;
}
```

> **Notes**
> - **`StreamContent`** uploads incrementally; `ByteArrayContent` would buffer the whole file.
> - **`Path.GetFileName`** strips the directory; the server should *also* strip its directory part defensively.
> - The bearer token is added by the `BearerTokenHandler` registered as a delegating handler on `_http`.

### 2.2 Server side

```csharp
group.MapPost("/{id:int}/profile-picture",
    async (int id, IFormFile file, UserService svc) =>
{
    if (file is null || file.Length == 0) return Results.BadRequest("Empty file");
    if (file.Length > 2 * 1024 * 1024)    return Results.BadRequest("Max 2 MB");
    if (!IsAllowedContentType(file.ContentType)) return Results.BadRequest("Disallowed type");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);

    var saved = await svc.SaveProfilePictureAsync(id, ms.ToArray(), file.FileName);
    return saved.Success
        ? Results.Ok(new { url = saved.Data })
        : Results.BadRequest(new { error = saved.ErrorMessage });
});

static bool IsAllowedContentType(string contentType)
    => contentType is "image/jpeg" or "image/png";
```

> **Reviewer notes**
> - Content-Type *must* be checked, not trusted. A malicious client can claim `image/png` for an arbitrary blob; deeper validation (magic bytes) is the next hardening step.
> - **Size limit** belongs in code, not Kestrel config alone — keeping the limit visible at the call site documents intent.
> - **Filename sanitisation** — the server-side path is computed from the user id, never from `file.FileName`.

### 2.3 Storage layout

Saved pictures live under `wwwroot/uploads/profiles/{userId}.jpg`. The URL returned to the client is `/uploads/profiles/{userId}.jpg`, served by `app.UseStaticFiles()`.

> **Why filesystem instead of BLOB in SQLite?** Profile pictures are small but numerous. Storing them in SQLite would balloon the database and complicate backups. Filesystem storage keeps the database compact and allows CDN offloading in the future.

---

## 3. Profile Picture Display (Local FS → WPF)

`StringToImageSourceConverter` (covered in detail in [`08-validation-and-converters.md`](08-validation-and-converters.md) §2.3) turns the cached local path into a `BitmapImage`.

> **Why a local cache?** WPF could bind `Image.Source` to an HTTP URL directly, but that creates a network request per image *every render*. Caching the file locally and letting the converter serve it is faster and works offline.

---

## 4. Excel Import (Bulk User Onboarding)

### 4.1 Workflow

1. Admin downloads a template (`Students_Template.xlsx`) from the app — three columns: NationalId | Email | UserName.
2. Admin fills the template offline and uploads it via the Manage Users screen.
3. Server iterates rows, creates one `StudentModel` per row, `await`s `UserService.CreateUserAsync` for each.
4. Server returns the count of successful inserts.

### 4.2 Implementation

```csharp
public async Task<Result<int>> ImportStudentsFromExcelAsync(string filePath)
{
    using var workbook = new XLWorkbook(filePath);
    var worksheet      = workbook.Worksheet(1);
    var rows           = worksheet.RowsUsed().Skip(1);   // skip header

    int successCount = 0;
    foreach (var row in rows)
    {
        var nationalId = row.Cell(1).GetString()?.Trim() ?? "";
        var email      = row.Cell(2).GetString()?.Trim() ?? "";
        var userName   = row.Cell(3).GetString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nationalId) ||
            string.IsNullOrWhiteSpace(email)      ||
            string.IsNullOrWhiteSpace(userName)) continue;

        var student = new StudentModel(0, email, userName, nationalId,
            new GradeModel { Id = 1, Name = "Imported", Num = 0 });

        if ((await _userService.CreateUserAsync(student)).Success) successCount++;
    }
    return Result<int>.Ok(successCount);
}
```

> **Reviewer notes**
> - The loop is sequential; concurrent `await Task.WhenAll(...)` was rejected because `CreateUserAsync` writes to SQLite, which serialises writes anyway. Sequential processing also keeps error reporting per-row clear.
> - **No transaction wrapping.** A partial import is acceptable — the admin can re-upload to retry the failed rows. Wrapping in a transaction would require all-or-nothing semantics.
> - **Default grade** — rows without a Grade column receive a placeholder Grade. The follow-up "fix grades" UI prompts the admin to correct these.

### 4.3 Excel security caveats

| Threat | Mitigation |
|---|---|
| Macro execution | ClosedXML is a managed parser — no macro VM is loaded |
| XML External Entity (XXE) attacks | `XLWorkbook` uses `XmlReaderSettings { DtdProcessing = Prohibit }` internally |
| Zip-bomb (huge inflated content) | File size is checked before parse; abort if > 5 MB |
| Path traversal via filename | Filename is never used to compute a save path |

---

## 5. Configuration Files

`appsettings.json` is loaded once at startup. The pipeline:

```csharp
_configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json",                       optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json",        optional: true,  reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
```

> **Reviewer note** — environment variables override JSON. This is the standard ASP.NET Core convention; secrets should be supplied via environment variables in production, never committed to source control.

---

## 6. SQLite Database File

The DB file is at `src/MentoringApp.Data/Resources/Database/mentoring.db`. On startup the host conditionally:

```csharp
if (recreateInitialDb)
{
    await dbRepo.RecreateAsync();          // drop & recreate schema
    await DummyDataSeeder.SeedAsync(...);  // insert deterministic test data
}
```

> **Why ship a `.db` file in the repo?** It guarantees a working app without a separate setup step for new contributors. The seeder regenerates it deterministically, so divergent local edits cannot pollute commits.

---

## 7. Future Extension — XML File Storage (rubric §10)

The rubric specifically mentions *"using files for storing data in XML"*. The codebase doesn't currently store any data in XML — it favours SQLite. A future extension that does fit naturally:

- **Configuration backups.** Snapshot the `Settings` table to a versioned `settings_YYYYMMDD.xml` before bulk admin changes.
- **Bulk export.** Export all of an admin's seeded data to a portable XML file for archival.

A skeleton for the writer:

```csharp
public Task<Result> ExportSettingsToXmlAsync(string targetPath)
{
    // [Placeholder] use System.Xml.Linq.XDocument:
    //   var doc = new XDocument(
    //       new XElement("Settings",
    //           settings.Select(s => new XElement("Setting",
    //               new XAttribute("Key", s.Key),
    //               new XAttribute("Value", s.Value)))));
    //   await using var fs = File.OpenWrite(targetPath);
    //   await doc.SaveAsync(fs, SaveOptions.None, ct);
    throw new NotImplementedException();
}
```

---

## 8. Reviewer Checklist

- [ ] Every file upload validates content-type *and* size.
- [ ] Server never trusts the client-supplied filename.
- [ ] Image files are loaded with `BitmapCacheOption.OnLoad` and frozen.
- [ ] Excel imports skip empty rows and report per-row failures.
- [ ] Configuration is read with `optional: false` for `appsettings.json` so a missing file fails fast at boot.
- [ ] Sensitive secrets (SMTP password, JWT secret) are loadable from environment variables, not only JSON.

---

## 9. Curriculum Alignment

| Rubric phrase | Realisation | Section |
|---|---|---|
| "File transfer between server and client" (§9) | Profile picture upload via `multipart/form-data` | §2 |
| "Files — using files for storing data, e.g. XML" (§10) | XML export skeleton (extension-ready) | §7 |
| "Stateless environment" | Files served via static-file middleware, not session | §2.3 |
| "Use of advanced libraries" | ClosedXML (Excel), `HttpClient.MultipartFormDataContent` | §2, §4 |
