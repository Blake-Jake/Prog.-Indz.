# 📊 Event Notification System Architecture

## System Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    EVENT CREATION FLOW                       │
├─────────────────────────────────────────────────────────────┤
│  User → EventCreator → DatePicker/TimePicker → Event        │
│                              ↓                               │
│                      Event.ScheduledAt = set                │
│                              ↓                               │
│                      Save to EventStore                      │
└─────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────┐
│                    FAVORITE EVENT FLOW                       │
├─────────────────────────────────────────────────────────────┤
│  User → EventPreview → Click Favorite → Event.FavoritedBy   │
│                              ↓                               │
│              Add currentUser to FavoritedBy list            │
│                              ↓                               │
│                      Save to EventStore                      │
└─────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────┐
│              NOTIFICATION SERVICE (Background)               │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Starts at App Launch (MauiProgram)                         │
│           ↓                                                  │
│  Background Task Loop (Every 30 minutes)                    │
│           ↓                                                  │
│  CheckUpcomingEventsAsync()                                 │
│           ↓                                                  │
│  Filter Events:                                             │
│  • FavoritedBy contains currentUser                         │
│  • ScheduledAt within next 24 hours                         │
│  • Not already notified                                     │
│           ↓                                                  │
│  SendNotificationAsync()                                    │
│           ↓                                                  │
│  Platform-specific Implementation:                          │
│  ├─ Android: NotificationManager                           │
│  ├─ iOS: UNUserNotificationCenter (ready)                  │
│  └─ Windows: Toast Notifications (ready)                   │
│           ↓                                                  │
│  System Notification Appears to User                        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Time-Based Notification Timeline

```
Event Created: Dec 25, 7:00 PM

Timeline:
┌──────────────────────────────────────────────────────────────┐
│ Dec 24, 7:00 PM - 1 day before                              │
│ ✅ NOTIFICATION SENT!                                        │
│ "Upcoming Event: Pizza Night happening in ~24 hours"        │
└──────────────────────────────────────────────────────────────┘
     ↓ (No more notifications until next check)
┌──────────────────────────────────────────────────────────────┐
│ Dec 25, 6:00 PM - 1 hour before                             │
│ ✅ NOTIFICATION SENT!                                        │
│ "Upcoming Event: Pizza Night happening in ~1 hour"          │
└──────────────────────────────────────────────────────────────┘
     ↓
┌──────────────────────────────────────────────────────────────┐
│ Dec 25, 7:00 PM - Event time                                │
│ Event occurs (no notification)                              │
└──────────────────────────────────────────────────────────────┘
```

## Component Interaction

```
┌────────────────────────────────────────────────────────────────┐
│                      APPLICATION STARTUP                        │
└────────────────────────────────────────────────────────────────┘
                             ↓
                      [MauiProgram.cs]
                             ↓
         ┌───────────────────┴───────────────────┐
         ↓                                       ↓
   [Register Services]               [Initialize Services]
         ↓                                       ↓
   - CloudAuthService              - HybridGroupService
   - CloudGroupService             - HybridAuthService
   - NotificationService ←─── Registered here!
   - UserDatabase                  - NotificationService ←─── Initialized here!
   - etc.
         ↓                                       ↓
         └───────────────────┬───────────────────┘
                             ↓
              [App is running with notifications active]

┌────────────────────────────────────────────────────────────────┐
│                   NOTIFICATION SERVICE                          │
│  (Runs in background thread)                                   │
├────────────────────────────────────────────────────────────────┤
│                                                                  │
│  StartBackgroundNotificationCheck()                            │
│  ├─ Creates CancellationTokenSource                            │
│  ├─ Starts infinite loop                                       │
│  └─ Cancellation token allows graceful shutdown                │
│                                                                  │
│  Every 30 minutes:                                             │
│  ├─ Get current user from Session                             │
│  ├─ Load all events from EventStore                           │
│  ├─ Filter by favorites, time window, not-notified           │
│  ├─ For each matching event:                                  │
│  │  ├─ Format time until event                                │
│  │  ├─ Call SendNotificationAsync()                           │
│  │  └─ Mark as notified in _sentNotifications                │
│  ├─ Sleep 30 minutes                                          │
│  └─ Repeat                                                     │
│                                                                  │
└────────────────────────────────────────────────────────────────┘
```

## Data Flow

```
EventStore (JSON/File)
├─ Event 1
│  ├─ Details: "Pizza Night"
│  ├─ ScheduledAt: 2026-12-25 19:00:00
│  ├─ FavoritedBy: ["user@email.com"]
│  └─ ... other fields
├─ Event 2
│  ├─ Details: "Concert"
│  ├─ ScheduledAt: 2026-12-20 18:00:00
│  ├─ FavoritedBy: []  ← Not favorited, skip
│  └─ ... other fields
└─ Event 3
   ├─ Details: "Old Event"
   ├─ ScheduledAt: 2026-12-01 19:00:00  ← Already past, skip
   ├─ FavoritedBy: ["user@email.com"]
   └─ ... other fields

         ↓ NotificationService.CheckUpcomingEventsAsync()

Current User: "user@email.com"
Current Time: 2026-12-24 18:30:00
Time Window: 2026-12-24 18:30:00 to 2026-12-25 18:30:00

         ↓ Filter

Matching Events:
├─ Event 1: ✅ In time window, favorited, not notified
│           → SEND NOTIFICATION
└─ Event 2: ✗ Not favorited
└─ Event 3: ✗ Already past

         ↓ Send

SendNotificationAsync("Upcoming Event", 
                     "Event happening in ~24 hours: Pizza Night")
```

## Testing Flow

```
You → NotificationTestHelper.TriggerTestNotificationAsync()
      ↓
Create Test Event
├─ Id: Unique
├─ Details: "🧪 TEST NOTIFICATION"
├─ ScheduledAt: DateTime.Now.AddHours(1)  ← 1 hour from now
├─ FavoritedBy: [CurrentUser]
└─ Save to EventStore
      ↓
Call notificationService.CheckUpcomingEventsAsync()
      ↓
Filter finds test event (it's in time window!)
      ↓
SendNotificationAsync()
      ↓
Platform-specific code triggers
      ↓
System Notification Appears! 🔔
```

## File Structure

```
EventMatch/
├── Services/
│   ├── NotificationService.cs ← NEW (Core logic)
│   ├── EventStore.cs (Uses existing)
│   └── ... other services
│
├── Utils/
│   ├── NotificationTestHelper.cs ← NEW (Testing)
│   └── ... other utilities
│
├── MauiProgram.cs (MODIFIED - registers + initializes)
├── Platforms/
│   └── Android/
│       └── AndroidManifest.xml (MODIFIED - added permission)
│
└── Documentation/
    ├── NOTIFICATIONS_README.md ← NEW
    ├── NOTIFICATION_TESTING.md ← NEW
    └── NOTIFICATION_QUICKSTART.md ← NEW (You are here!)
```

## State Management

```
NotificationService._sentNotifications
├─ HashSet<string> to track sent notifications
├─ Keys: "{eventId}_{userId}"
├─ Purpose: Prevent duplicate notifications for same event
└─ Lifetime: App session (clears on restart)

Example:
├─ "event123_user@email.com" → Already notified
├─ "event456_user@email.com" → Not in set, notify it
└─ "event789_user@email.com" → Already notified
```

## Permission & Platform Requirements

```
Android 13+:
├─ Permission: POST_NOTIFICATIONS
├─ Added to: AndroidManifest.xml
├─ Runtime: Requested when NotificationService initializes
└─ UI: User sees prompt

iOS:
├─ Permission: UNUserNotificationCenter.Current().RequestAuthorizationAsync()
├─ Framework ready: NotificationService has iOS method stub
└─ Needs implementation for production

Windows:
├─ Permission: Windows.UI.Notifications (OS-level)
├─ Framework ready: NotificationService has Windows method stub
└─ Needs implementation for production
```
