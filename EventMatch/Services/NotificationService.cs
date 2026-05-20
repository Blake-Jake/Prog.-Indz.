using EventMatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventMatch.Services
{
    public class NotificationService
    {
        private EventStore _eventStore;
        private CancellationTokenSource _cancellationTokenSource;

        public NotificationService()
        {
            _eventStore = new EventStore();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Request permissions if needed
#if ANDROID
                await RequestAndroidPermissionsAsync();
#endif
                // Start background task to check for upcoming events
                StartBackgroundNotificationCheck();
                System.Diagnostics.Debug.WriteLine("[NotificationService] Notification service initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Init error: {ex}");
            }
        }

#if ANDROID
        private async Task RequestAndroidPermissionsAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    System.Diagnostics.Debug.WriteLine($"[NotificationService] Notification permission: {status}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Permission request error: {ex}");
            }
        }
#endif

        public async Task SendNotificationAsync(string title, string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Sending notification: {title} - {message}");

                // Display as a toast using MainThread for UI safety
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        // Use DisplayAlert as a toast-like notification
                        await Application.Current.MainPage.DisplayAlert(title, message, "OK");
                        System.Diagnostics.Debug.WriteLine($"[NotificationService] Toast notification displayed successfully");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Error sending notification: {ex}");
            }
        }

        public async Task CheckUpcomingEventsAsync()
        {
            try
            {
                var currentUser = Session.CurrentUserEmail;
                System.Diagnostics.Debug.WriteLine($"[NotificationService] CheckUpcomingEventsAsync - Current user: {currentUser}");

                if (string.IsNullOrEmpty(currentUser))
                {
                    System.Diagnostics.Debug.WriteLine("[NotificationService] No current user logged in");
                    return;
                }

                var events = _eventStore.LoadAll();
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Loaded {events.Count} total events");

                var now = DateTime.Now;
                var oneDayFromNow = now.AddDays(1);

                System.Diagnostics.Debug.WriteLine($"[NotificationService] Now: {now:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Check window: {now:yyyy-MM-dd HH:mm:ss} to {oneDayFromNow:yyyy-MM-dd HH:mm:ss}");

                // Get favorited events scheduled within the next day
                var upcomingEvents = events
                    .Where(e => e.FavoritedBy != null && e.FavoritedBy.Contains(currentUser))
                    .Where(e => e.ScheduledAt > now && e.ScheduledAt <= oneDayFromNow)
                    .Where(e => !HasNotificationBeenSent(e.Id, currentUser))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[NotificationService] Found {upcomingEvents.Count} upcoming events to notify");

                foreach (var evt in upcomingEvents)
                {
                    var timeUntilEvent = evt.ScheduledAt - now;
                    var message = $"Event happening in {FormatTimespan(timeUntilEvent)}: {evt.Details}";

                    System.Diagnostics.Debug.WriteLine($"[NotificationService] Sending notification for: {evt.Details} (scheduled: {evt.ScheduledAt:yyyy-MM-dd HH:mm:ss})");

                    await SendNotificationAsync("Upcoming Event", message);

                    // Mark notification as sent
                    MarkNotificationAsSent(evt.Id, currentUser);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Error checking upcoming events: {ex}");
            }
        }

        private void StartBackgroundNotificationCheck()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await CheckUpcomingEventsAsync();
                        // Check every 30 minutes
                        await Task.Delay(TimeSpan.FromMinutes(30), _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NotificationService] Background check error: {ex}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private string FormatTimespan(TimeSpan span)
        {
            if (span.TotalHours < 1)
                return $"{span.Minutes} minutes";
            else if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hour{((int)span.TotalHours > 1 ? "s" : "")}";
            else
                return $"{(int)span.TotalDays} day{((int)span.TotalDays > 1 ? "s" : "")}";
        }

        // Track sent notifications to avoid duplicates
        private readonly HashSet<string> _sentNotifications = new HashSet<string>();

        private bool HasNotificationBeenSent(string eventId, string userId)
        {
            var key = $"{eventId}_{userId}";
            return _sentNotifications.Contains(key);
        }

        private void MarkNotificationAsSent(string eventId, string userId)
        {
            var key = $"{eventId}_{userId}";
            _sentNotifications.Add(key);
        }
    }
}
