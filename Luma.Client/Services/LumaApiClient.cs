using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Luma.Client.Models;

namespace Luma.Client.Services;

public class LumaApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public string? ActiveToken { get; set; }

    public LumaApiClient(string baseUrl = "https://luma.lacuna.cc")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        
        if (ActiveToken != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActiveToken);
        }

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        // Even if status code is not 200, we should try to read the JSON because Luma API might 
        // return JSON responses with different codes, though the documentation says:
        // "All API responses should have HTTP status code 200 and share the base parameters: { code, message }"
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            // If it's 401 Unauthorized, we might want to return a response with 'Unauthorized' code
            // to match the orchestrator expectations.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var unauthorizedResponse = JsonSerializer.Deserialize<TResponse>("{\"code\":\"Unauthorized\",\"message\":\"HTTP 401 Unauthorized\"}");
                if (unauthorizedResponse != null)
                {
                    return unauthorizedResponse;
                }
            }
            throw new HttpRequestException($"HTTP Request failed with status code {response.StatusCode}: {content}");
        }

        var result = JsonSerializer.Deserialize<TResponse>(content);
        if (result == null)
        {
            throw new JsonException($"Failed to deserialize response: {content}");
        }

        return result;
    }

    public async Task<StartResponse> StartAsync(string username, string email, bool level2 = false, CancellationToken cancellationToken = default)
    {
        var path = level2 ? "/api/start/2" : "/api/start";
        var request = CreateRequest(HttpMethod.Post, path, new StartRequest(username, email));
        return await SendAsync<StartResponse>(request, cancellationToken);
    }

    public async Task<ProbesResponse> GetProbesAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, "/api/probe");
        return await SendAsync<ProbesResponse>(request, cancellationToken);
    }

    public async Task<(SyncResponse Response, long T0, long T3)> SyncProbeAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, $"/api/probe/{probeId}/sync");
        
        long t0 = DateTimeOffset.UtcNow.Ticks;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        long t3 = DateTimeOffset.UtcNow.Ticks;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var unauthorizedResponse = JsonSerializer.Deserialize<SyncResponse>("{\"code\":\"Unauthorized\",\"message\":\"HTTP 401 Unauthorized\"}");
            return (unauthorizedResponse!, t0, t3);
        }

        // Handle specific probe unreachable scenarios that could return a custom code/status.
        // Wait, does ProbeUnreachable return 200 or 503/400? The API documentation says:
        // "All API responses should have HTTP status code 200 and share the base parameters..."
        // So it's likely a 200 response with code "ProbeUnreachable".
        // Let's just deserialize standard response.
        var result = JsonSerializer.Deserialize<SyncResponse>(content);
        if (result == null)
        {
            throw new JsonException($"Failed to deserialize sync response: {content}");
        }

        return (result, t0, t3);
    }

    public async Task<JobTakeResponse> TakeJobAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, "/api/job/take");
        return await SendAsync<JobTakeResponse>(request, cancellationToken);
    }

    public async Task<JobCheckResponse> CheckJobAsync(string jobId, string probeNow, long roundTrip, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, $"/api/job/{jobId}/check", new JobCheckRequest(probeNow, roundTrip));
        return await SendAsync<JobCheckResponse>(request, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
