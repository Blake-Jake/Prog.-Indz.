using EventMatch.Services;
using EventMatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventMatch.Utils
{
    public static class NotificationTestHelper
    {
        /// <summary>
        /// Create a test event happening in 10 seconds and send a notification for it.
        /// Call this from the Debug page or any page to test notifications.
        /// </summary>
        public static async Task TriggerTestNotificationAsync()
        {
            try
            {
                // Get the notification service from the DI container
                var notificationService = (NotificationService)Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(NotificationService));

                if (notificationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestHelper] Notification service not available");
                    return;
                }

                // Ensure we have a logged-in user
                var currentUser = Session.CurrentUserEmail;
                if (string.IsNullOrEmpty(currentUser))
                {
                    System.Diagnostics.Debug.WriteLine("[TestHelper] No logged-in user");
                    return;
                }

                // Create a test event that's happening in 10 seconds from now
                var scheduledTime = DateTime.Now.AddSeconds(10);
                var testEvent = new Event
                {
                    Id = $"test_notification_{Guid.NewGuid()}",
                    Details = "An event is coming up",
                    ScheduledAt = scheduledTime,
                    CreatedAt = DateTime.UtcNow,
                    LocationAddress = "Test Location",
                    ImageBase64 = string.Empty,
                    FavoritedBy = new List<string> { currentUser },
                    Tags = new List<Tag>()
                };

                // Add the test event to the store
                var store = new EventStore();
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Adding test event: {testEvent.Details}");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Event ID: {testEvent.Id}");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Scheduled for: {testEvent.ScheduledAt:yyyy-MM-dd HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Time until event: {(testEvent.ScheduledAt - DateTime.Now).TotalSeconds:F2} seconds");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Favorited by: {currentUser}");

                store.Add(testEvent);
                System.Diagnostics.Debug.WriteLine("[TestHelper] Test event saved to store");

                // Send the notification directly instead of waiting for the background check
                System.Diagnostics.Debug.WriteLine("[TestHelper] Sending notification directly");
                await notificationService.SendNotificationAsync("An event is coming up", $"{testEvent.Details} - Scheduled in 10 seconds");

                System.Diagnostics.Debug.WriteLine("[TestHelper] Test notification sent successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Error triggering test notification: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TestHelper] Stack trace: {ex.StackTrace}");
            }
        }
    }
}
