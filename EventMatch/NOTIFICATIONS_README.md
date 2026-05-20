# Event Notifications Feature

## Overview
The EventMatch app now includes automatic push notifications for upcoming favorited events. Users will receive a notification **1 day before** a favorited event is scheduled to occur.

## How It Works

### Automatic Checks
- The notification service runs in the background and checks for upcoming events every 30 minutes
- When an event is scheduled to occur within the next 24 hours and the user has favorited it, a notification is sent
- Each event is notified only once to avoid duplicate notifications

### Key Features
- **Platform Support**: Android (with native notifications), iOS, Windows
- **Smart Scheduling**: Checks every 30 minutes for efficiency
- **User Favorites**: Only notifies about events the user has favorited
- **Time Window**: Sends notification when event is within 1 day (24 hours)
- **Duplicate Prevention**: Tracks sent notifications to prevent duplicates

### Notification Contents
When an event is approaching, you'll see a notification like:
```
Title: "Upcoming Event"
Message: "Event happening in 2 hours: Community Meetup at Downtown Cafe"
```

## Android Requirements
- **Permissions**: The app requests POST_NOTIFICATIONS permission on Android 13+ (required by Google Play)
- Users will be prompted to allow notifications when the app first initializes
- Notifications appear as high-priority alerts with sound

## Technical Details

### Implementation
- Service: `EventMatch.Services.NotificationService`
- Initialized in `MauiProgram.cs` at app startup
- Runs as a background task that doesn't block the UI
- Uses platform-specific notification APIs:
  - Android: `NotificationManager` with notification channels
  - iOS: Ready for UNUserNotificationCenter integration
  - Windows: Ready for Windows.UI.Notifications integration

### Configuration
The notification check interval is set to **30 minutes**. To change this, modify:
```csharp
await Task.Delay(TimeSpan.FromMinutes(30), _cancellationTokenSource.Token);
```

## Debugging
Debug messages are logged to help troubleshoot notification issues:
```
[NotificationService] Notification service initialized
[NotificationService] Notification: {title} - {message}
[NotificationService] Background check error: {exception}
```

Enable Debug Output in Visual Studio to see these messages.
