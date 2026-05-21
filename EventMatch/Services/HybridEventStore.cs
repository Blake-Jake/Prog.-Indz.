using EventMatch.Models;
using System.Diagnostics;
using System.Linq;

namespace EventMatch.Services;

/// <summary>
/// Hybrid event store that syncs between cloud and local storage
/// Events are stored locally and synced to cloud when available
/// If cloud is unavailable, works entirely from local storage
/// </summary>
public class HybridEventStore
{
    private readonly CloudEventService _cloudEventService;
    private readonly EventStore _localEventStore;
    private bool _isCloudAvailable = false;
    private string? _currentUserEmail = null;

    public HybridEventStore(CloudEventService cloudEventService, EventStore localEventStore)
    {
        _cloudEventService = cloudEventService;
        _localEventStore = localEventStore;
        Debug.WriteLine("[HybridEventStore] Initialized");
    }

    /// <summary>
    /// Initialize cloud connectivity and set current user
    /// </summary>
    public async Task InitializeAsync(string userEmail)
    {
        _currentUserEmail = userEmail;
        _isCloudAvailable = await _cloudEventService.CheckCloudConnectivityAsync();
        Debug.WriteLine($"[HybridEventStore] Initialized for user {userEmail}, cloud available: {_isCloudAvailable}");
    }

    /// <summary>
    /// Load all events - tries cloud first, falls back to local
    /// </summary>
    public async Task<List<Event>> LoadAllAsync()
    {
        try
        {
            // Try to get events from cloud first
            if (_isCloudAvailable && !string.IsNullOrEmpty(_currentUserEmail))
            {
                Debug.WriteLine("[HybridEventStore] Attempting to load from cloud");
                var cloudEvents = await _cloudEventService.DownloadEventsAsync(_currentUserEmail);

                if (cloudEvents != null && cloudEvents.Count > 0)
                {
                    Debug.WriteLine($"[HybridEventStore] Loaded {cloudEvents.Count} events from cloud");
                    // Update local cache with cloud data
                    _localEventStore.SaveAll(cloudEvents);
                    return cloudEvents;
                }
            }

            // Fall back to local storage
            Debug.WriteLine("[HybridEventStore] Loading from local storage");
            return _localEventStore.LoadAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] LoadAll error: {ex.Message}, falling back to local");
            return _localEventStore.LoadAll();
        }
    }

    /// <summary>
    /// Add event - saves locally and syncs to cloud if available
    /// </summary>
    public async Task<bool> AddAsync(Event e)
    {
        try
        {
            // Always save locally first
            _localEventStore.Add(e);
            Debug.WriteLine($"[HybridEventStore] Event saved locally: {e.Id}");

            // Try to sync to cloud
            if (_isCloudAvailable && !string.IsNullOrEmpty(_currentUserEmail))
            {
                var all = _localEventStore.LoadAll();
                var success = await _cloudEventService.UploadEventsAsync(_currentUserEmail, all);
                if (success)
                {
                    Debug.WriteLine($"[HybridEventStore] Event synced to cloud: {e.Id}");
                }
                else
                {
                    Debug.WriteLine($"[HybridEventStore] Cloud sync failed, event saved locally only");
                }
            }
            else
            {
                Debug.WriteLine("[HybridEventStore] Cloud not available, event saved locally only");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] AddAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save all events - updates locally and syncs to cloud
    /// </summary>
    public async Task<bool> SaveAllAsync(List<Event> events)
    {
        try
        {
            // Save locally first
            _localEventStore.SaveAll(events);
            Debug.WriteLine($"[HybridEventStore] Saved {events.Count} events locally");

            // Sync to cloud if available
            if (_isCloudAvailable && !string.IsNullOrEmpty(_currentUserEmail))
            {
                var success = await _cloudEventService.UploadEventsAsync(_currentUserEmail, events);
                if (success)
                {
                    Debug.WriteLine($"[HybridEventStore] {events.Count} events synced to cloud");
                }
                else
                {
                    Debug.WriteLine($"[HybridEventStore] Cloud sync failed");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] SaveAllAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sync local events to cloud
    /// </summary>
    public async Task<bool> SyncLocalToCloudAsync()
    {
        try
        {
            if (!_isCloudAvailable)
            {
                Debug.WriteLine("[HybridEventStore] Cloud not available, skipping sync");
                return false;
            }

            if (string.IsNullOrEmpty(_currentUserEmail))
            {
                Debug.WriteLine("[HybridEventStore] No current user, skipping sync");
                return false;
            }

            var events = _localEventStore.LoadAll();
            Debug.WriteLine($"[HybridEventStore] Syncing {events.Count} local events to cloud");

            var success = await _cloudEventService.UploadEventsAsync(_currentUserEmail, events);
            if (success)
            {
                Debug.WriteLine("[HybridEventStore] Sync to cloud successful");
            }
            else
            {
                Debug.WriteLine("[HybridEventStore] Sync to cloud failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] SyncLocalToCloudAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sync cloud events to local storage
    /// </summary>
    public async Task<bool> SyncCloudToLocalAsync()
    {
        try
        {
            if (!_isCloudAvailable || string.IsNullOrEmpty(_currentUserEmail))
            {
                Debug.WriteLine("[HybridEventStore] Cloud not available, skipping sync");
                return false;
            }

            Debug.WriteLine("[HybridEventStore] Syncing cloud events to local");
            var cloudEvents = await _cloudEventService.DownloadEventsAsync(_currentUserEmail);

            if (cloudEvents == null)
            {
                Debug.WriteLine("[HybridEventStore] Failed to download from cloud");
                return false;
            }

            _localEventStore.SaveAll(cloudEvents);
            Debug.WriteLine($"[HybridEventStore] Synced {cloudEvents.Count} events from cloud");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] SyncCloudToLocalAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get all public events from cloud (shared between all users/devices)
    /// </summary>
    public async Task<List<Event>> GetAllPublicEventsAsync()
    {
        try
        {
            if (!_isCloudAvailable)
            {
                Debug.WriteLine("[HybridEventStore] Cloud not available for public events");
                return new List<Event>();
            }

            var events = await _cloudEventService.GetAllPublicEventsAsync();
            return events ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridEventStore] GetAllPublicEventsAsync error: {ex.Message}");
            return new List<Event>();
        }
    }

    /// <summary>
    /// Check if cloud is currently available
    /// </summary>
    public bool IsCloudAvailable => _isCloudAvailable;
}
