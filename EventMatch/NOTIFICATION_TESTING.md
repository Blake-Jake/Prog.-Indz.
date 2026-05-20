# Testing Notifications

## Quick Test Methods

### Method 1: Using the NotificationTestHelper (Recommended)

The `NotificationTestHelper` class provides an easy way to trigger a test notification from anywhere in your code:

```csharp
// From any page or service
await EventMatch.Utils.NotificationTestHelper.TriggerTestNotificationAsync();
```

This will:
1. Create a test event scheduled for 1 hour from now
2. Mark it as favorited by the current user
3. Manually trigger the notification check
4. Send you a test notification

#### Example: Adding to a Button Click Handler

```csharp
private async void OnTestNotificationClicked(object sender, EventArgs e)
{
    try
    {
        await NotificationTestHelper.TriggerTestNotificationAsync();
        await DisplayAlert("Test", "Notification test completed. Check your notifications!", "OK");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Test failed: {ex.Message}", "OK");
    }
}
```

### Method 2: Manual Testing in the Event Preview Page

1. Create an event in Event Creator with date/time set to **1 hour from now**
2. Favorite the event in Event Preview
3. Wait for the background check (checks every 30 minutes) OR
4. Manually add a breakpoint and call:
   ```csharp
   await NotificationTestHelper.TriggerTestNotificationAsync();
   ```

### Method 3: Direct Service Call

If you have access to the NotificationService:

```csharp
var notificationService = (NotificationService)Application.Current
    ?.Handler?.MauiContext?.Services?.GetService(typeof(NotificationService));

if (notificationService != null)
{
    await notificationService.CheckUpcomingEventsAsync();
}
```

## What to Look For

When a notification is triggered, you should see:

- **Android**: System notification with title "Upcoming Event" and the event details
- **Debug Output**: Log messages like:
  ```
  [NotificationService] Notification: Upcoming Event - Event happening in 1 hour: Test Event
  ```

## Troubleshooting

### No Notification Appears
- Check Debug Output window (View → Output) for error messages
- Ensure you're logged in (Session.CurrentUserEmail is not empty)
- Make sure notification permissions are granted on Android
- Verify event is favorited by current user
- Check that event ScheduledAt is within the next 24 hours

### "Notification service not available"
- The service may not have initialized yet
- Try waiting a few seconds after app launch
- Check MauiProgram.cs initialization logs

### Errors in Debug Output
- Look for `[NotificationService]` prefixed messages
- They will indicate what went wrong (permissions, event not found, etc.)

## Testing on Different Platforms

### Android (Emulator/Device)
- Notifications will appear in the system notification area
- Click to open or dismiss
- Grant POST_NOTIFICATIONS permission when prompted

### Windows
- Notifications will use Windows Toast notifications
- Look in Action Center (bottom right corner)

### iOS
- Notifications will use UNUserNotificationCenter
- Grant notification permission when prompted

## Automated Testing

The notification service checks every 30 minutes automatically. To test the automated system:

1. Create an event with ScheduledAt = DateTime.Now.AddMinutes(50)
2. Favorite it
3. Wait up to 30 minutes for the background check to run
4. You should receive a notification when 24 hours or less remains

## Debugging Tips

Enable maximum logging by checking the Debug Output window:
```
[NotificationService] Notification service initialized
[NotificationService] Notification: Title - Message
[NotificationService] Background check error: ...
```

If you don't see these messages, the service may not have initialized. Check MauiProgram.cs logs.
