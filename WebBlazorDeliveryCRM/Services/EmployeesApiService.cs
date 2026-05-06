using System.Net.Http.Json;
using System.Net;

namespace WebBlazorDeliveryCRM.Services;

public class EmployeesApiService
{
    private readonly HttpClient _http;

    public EmployeesApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("AuthorizedClient");
    }

    public async Task<List<EmployeeDto>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<EmployeeDto>>($"/api/Employees?companyId={companyId}", cancellationToken);
            return list ?? new List<EmployeeDto>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return new List<EmployeeDto>();
        }
    }

    public async Task<bool> CreateAsync(CreateEmployeeRequestDto request, int companyId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"/api/Employees?companyId={companyId}", request, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> FireAsync(int employeeId, int companyId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"/api/Employees/{employeeId}/fire?companyId={companyId}", null, cancellationToken);
        return resp.IsSuccessStatusCode;
    }

    public async Task<(bool ok, string? error)> ChangeRoleAsync(int employeeId, int companyId, int roleId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"/api/Employees/{employeeId}/role?companyId={companyId}", new { roleId }, cancellationToken);
        if (resp.IsSuccessStatusCode)
            return (true, null);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        return (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : body);
    }

    public sealed class EmployeeDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? Patronumic { get; set; }
        public string Role { get; set; } = "";
        public bool Is_Active { get; set; }
        public bool IsFired { get; set; }
        public DateTime Created_at { get; set; }
        public int Company_id { get; set; }
        public string? Email { get; set; }
    }

    public sealed class CreateEmployeeRequestDto
    {
        public string FName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Patronymic { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public int RoleId { get; set; }
    }
}

