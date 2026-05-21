using EventMatch.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventMatch.Services
{
    /// <summary>
    /// Syncs events between Windows and Android via local file storage.
    /// Events are exported to a JSON file that can be shared/copied between devices.
    /// </summary>
    public class EventSyncService
    {
        private EventStore _eventStore = new EventStore();
        private const string SYNC_FILENAME = "eventmatch_sync.json";

        /// <summary>
        /// Get the path where sync files are stored
        /// Windows: User's Documents folder (accessible via file explorer)
        /// Android: App's shared cache directory
        /// </summary>
        public string GetSyncDirectory()
        {
#if ANDROID
            // On Android, use app's cache directory (accessible to all apps)
            var cacheDir = Android.App.Application.Context.CacheDir.AbsolutePath;
            return cacheDir;
#else
            // On Windows, use Documents folder for easy access
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
        }

        /// <summary>
        /// Get the full path to the sync file
        /// </summary>
        public string GetSyncFilePath()
        {
            return Path.Combine(GetSyncDirectory(), SYNC_FILENAME);
        }

        /// <summary>
        /// Export all current events to a sync file
        /// Returns the path to the created file
        /// </summary>
        public string ExportEvents()
        {
            try
            {
                var events = _eventStore.LoadAll();
                string syncPath = GetSyncFilePath();

                var syncData = new
                {
                    ExportedAt = DateTime.UtcNow,
                    Platform = DeviceInfo.Current.Platform.ToString(),
                    EventCount = events.Count,
                    Events = events
                };

                var json = JsonSerializer.Serialize(syncData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(syncPath, json);

                System.Diagnostics.Debug.WriteLine($"[EventSync] Exported {events.Count} events to: {syncPath}");
                return syncPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventSync] Export error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Import events from a sync file
        /// Merges with existing events (doesn't duplicate by ID)
        /// Returns the number of NEW events imported
        /// </summary>
        public int ImportEvents()
        {
            try
            {
                string syncPath = GetSyncFilePath();

                if (!File.Exists(syncPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[EventSync] Sync file not found: {syncPath}");
                    return 0;
                }

                var json = File.ReadAllText(syncPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Events", out var eventsArray))
                {
                    System.Diagnostics.Debug.WriteLine("[EventSync] No Events array in sync file");
                    return 0;
                }

                // Load current events ONCE
                var currentEvents = _eventStore.LoadAll();
                var currentIds = new HashSet<string>(currentEvents.Select(e => e.Id));
                var eventsToAdd = new List<Event>();

                // Deserialize all new events first
                foreach (var eventElement in eventsArray.EnumerateArray())
                {
                    try
                    {
                        var eventJson = eventElement.GetRawText();
                        var importedEvent = JsonSerializer.Deserialize<Event>(eventJson, options);

                        // Only import if we don't already have this event
                        if (!currentIds.Contains(importedEvent.Id))
                        {
                            eventsToAdd.Add(importedEvent);
                            currentIds.Add(importedEvent.Id);
                            System.Diagnostics.Debug.WriteLine($"[EventSync] ✓ Will import: {importedEvent.Details}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[EventSync] - Skipped (exists): {importedEvent.Details}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EventSync] Error deserializing event: {ex.Message}");
                    }
                }

                // If we have new events, add them all at once and save
                if (eventsToAdd.Count > 0)
                {
                    currentEvents.AddRange(eventsToAdd);
                    _eventStore.SaveAll(currentEvents);
                    System.Diagnostics.Debug.WriteLine($"[EventSync] ✅ Saved all {eventsToAdd.Count} new events to store");
                }

                System.Diagnostics.Debug.WriteLine($"[EventSync] Import complete: {eventsToAdd.Count} new events imported");
                return eventsToAdd.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventSync] Import error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Check if sync file exists
        /// </summary>
        public bool SyncFileExists()
        {
            return File.Exists(GetSyncFilePath());
        }

        /// <summary>
        /// Open the sync file location in file explorer
        /// </summary>
        public void OpenSyncLocation()
        {
            try
            {
                string syncPath = GetSyncFilePath();
                string directory = Path.GetDirectoryName(syncPath);

#if ANDROID
                System.Diagnostics.Debug.WriteLine($"[EventSync] Sync location: {directory}");
                // On Android, just log the path (user can access via file manager)
#else
                // On Windows, open Explorer to the file
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{syncPath}\"");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventSync] Error opening location: {ex}");
            }
        }

        /// <summary>
        /// Get info about the sync file (size, event count, etc)
        /// </summary>
        public string GetSyncInfo()
        {
            try
            {
                string syncPath = GetSyncFilePath();

                if (!File.Exists(syncPath))
                {
                    return "No sync file exists yet.\nClick 'Export Events' to create one.";
                }

                var info = new FileInfo(syncPath);
                var json = File.ReadAllText(syncPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int eventCount = 0;
                if (root.TryGetProperty("Events", out var eventsArray))
                {
                    eventCount = eventsArray.GetArrayLength();
                }

                var exportedAt = root.TryGetProperty("ExportedAt", out var timeElement) 
                    ? timeElement.GetString() 
                    : "Unknown";

                return $"Sync File Information:\n\n" +
                       $"Location: {syncPath}\n" +
                       $"File Size: {info.Length} bytes\n" +
                       $"Events: {eventCount}\n" +
                       $"Last Exported: {exportedAt}";
            }
            catch (Exception ex)
            {
                return $"Error reading sync file: {ex.Message}";
            }
        }
    }
}
