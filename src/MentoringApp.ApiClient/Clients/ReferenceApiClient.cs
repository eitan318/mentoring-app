using MentoringApp.ApiClient.Models;
using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

public class ReferenceApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<SubjectModel>> GetSubjectsAsync() =>
        GetAsync<IEnumerable<SubjectModel>>("/api/reference/subjects");

    public Task<IEnumerable<GradeModel>> GetGradesAsync() =>
        GetAsync<IEnumerable<GradeModel>>("/api/reference/grades");

    public Task<IEnumerable<SchoolClassModel>> GetSchoolClassesAsync() =>
        GetAsync<IEnumerable<SchoolClassModel>>("/api/reference/school-classes");

    public Task<IEnumerable<SchoolClassModel>> GetSchoolClassesBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<SchoolClassModel>>($"/api/reference/school-classes/by-supervisor/{supervisorId}");

    public Task AddSchoolClassAsync(AddSchoolClassRequest request) =>
        PostAsync("/api/reference/school-classes", request);

    public Task DeleteSchoolClassAsync(int id) =>
        DeleteAsync($"/api/reference/school-classes/{id}");

    public async Task<int?> GetSupervisorIdForStudentAsync(int studentId)
    {
        try
        {
            var result = await GetAsync<SupervisorIdResponse>($"/api/reference/supervisor-for-student/{studentId}");
            return result.SupervisorId;
        }
        catch
        {
            return null;
        }
    }

    private record SupervisorIdResponse(int SupervisorId);
}
