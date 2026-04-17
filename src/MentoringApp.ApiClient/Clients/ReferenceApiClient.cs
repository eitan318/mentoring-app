using MentoringApp.ApiClient.Models;

namespace MentoringApp.ApiClient.Clients;

public class ReferenceApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<IEnumerable<SubjectResponse>> GetSubjectsAsync() =>
        GetAsync<IEnumerable<SubjectResponse>>("/api/reference/subjects");

    public Task<IEnumerable<GradeResponse>> GetGradesAsync() =>
        GetAsync<IEnumerable<GradeResponse>>("/api/reference/grades");

    public Task<IEnumerable<SchoolClassResponse>> GetSchoolClassesAsync() =>
        GetAsync<IEnumerable<SchoolClassResponse>>("/api/reference/school-classes");

    public Task<IEnumerable<SchoolClassResponse>> GetSchoolClassesBySupervisorAsync(int supervisorId) =>
        GetAsync<IEnumerable<SchoolClassResponse>>($"/api/reference/school-classes/by-supervisor/{supervisorId}");
}
