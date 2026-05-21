using EventMatch.Models;
using System.Net.Http.Json;
using System.Diagnostics;

namespace EventMatch.Services;

/// <summary>
/// Cloud event service for syncing events with remote backend
/// If cloud is unavailable, gracefully degrades to local-only storage
/// </summary>
public class CloudEventService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private const int TimeoutMs = 5000; // 5 second timeout

    public CloudEventService()
    {
        _httpClient = new HttpClient();
        _apiBaseUrl = GetApiBaseUrl();
        Debug.WriteLine($"[CloudEventService] Using API base URL: {_apiBaseUrl}");
    }

    private static string GetApiBaseUrl()
    {
        var env = Environment.GetEnvironmentVariable("EVENTMATCH_API_BASE_URL");
        if (!string.IsNullOrEmpty(env)) return env;
        return "https://eventmatch-api.onrender.com";
    }

    /// <summary>
    /// Check if cloud server is reachable
    /// </summary>
    public async Task<bool> CheckCloudConnectivityAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
            var url = $"{_apiBaseUrl}/api/health";
            Debug.WriteLine($"[CloudEventService] Checking cloud connectivity: {url}");
            var response = await _httpClient.GetAsync(url, cts.Token);
            var isAvailable = response.IsSuccessStatusCode;
            Debug.WriteLine($"[CloudEventService] Cloud connectivity check: {(isAvailable ? "✓ Available" : "✗ Unavailable")}");
            return isAvailable;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CloudEventService] Connectivity check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Upload events to cloud for a specific user
    /// </summary>
    public async Task<bool> UploadEventsAsync(string userEmail, List<Event> events)
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
            var url = $"{_apiBaseUrl}/api/events/sync";
            Debug.WriteLine($"[CloudEventService] Uploading {events.Count} events for user {userEmail}");

            var payload = new { userEmail, events };
            var response = await _httpClient.PostAsJsonAsync(url, payload, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[CloudEventService] Upload succeeded");
                return true;
            }

            Debug.WriteLine($"[CloudEventService] Upload failed: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CloudEventService] Upload error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Download events from cloud for a specific user
    /// </summary>
    public async Task<List<Event>?> DownloadEventsAsync(string userEmail)
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
            var url = $"{_apiBaseUrl}/api/events?userEmail={Uri.EscapeDataString(userEmail)}";
            Debug.WriteLine($"[CloudEventService] Downloading events for user {userEmail}");

            var response = await _httpClient.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var events = await response.Content.ReadFromJsonAsync<List<Event>>();
                Debug.WriteLine($"[CloudEventService] Downloaded {events?.Count ?? 0} events");
                return events ?? new List<Event>();
            }

            Debug.WriteLine($"[CloudEventService] Download failed: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CloudEventService] Download error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get all public events from cloud (shared across all users)
    /// </summary>
    public async Task<List<Event>?> GetAllPublicEventsAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeoutMs);
            var url = $"{_apiBaseUrl}/api/events/public";
            Debug.WriteLine($"[CloudEventService] Fetching all public events");

            var response = await _httpClient.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var events = await response.Content.ReadFromJsonAsync<List<Event>>();
                Debug.WriteLine($"[CloudEventService] Retrieved {events?.Count ?? 0} public events");
                return events ?? new List<Event>();
            }

            Debug.WriteLine($"[CloudEventService] Fetch failed: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CloudEventService] Fetch error: {ex.Message}");
            return null;
        }
    }
}
