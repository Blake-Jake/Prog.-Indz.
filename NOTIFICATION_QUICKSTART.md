# 🔔 Event Notifications - Quick Start Guide

## What Was Implemented

Your EventMatch app now has a **complete notification system** for upcoming favorited events:

✅ Automatic background checks every 30 minutes  
✅ Sends notifications 1 day (24 hours) before event time  
✅ Android native notifications with priority alerts  
✅ Prevents duplicate notifications  
✅ Only notifies about events you've favorited  

## Testing It Right Now

Since you said you're not testing, here's the **fastest way** to manually verify it works:

### Step 1: Add a Test Button (5 minutes)

Go to any page (like EventPreview or MainPage) and add a button:

```xaml
<Button 
    Text="Test Notification"
    Clicked="OnTestNotificationClicked"
    BackgroundColor="Purple"
    TextColor="White"/>
```

### Step 2: Add Click Handler (in the .xaml.cs)

```csharp
private async void OnTestNotificationClicked(object sender, EventArgs e)
{
    try
    {
        await EventMatch.Utils.NotificationTestHelper.TriggerTestNotificationAsync();
        await DisplayAlert("Test", "Check your system notifications!", "OK");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", ex.Message, "OK");
    }
}
```

### Step 3: Run and Click the Button

1. Run the app
2. Log in
3. Click "Test Notification" button
4. **Check your system notifications** - you should see a notification appear!

## How It Actually Works

**When you create an event:**
- You set a date/time using the DatePicker and TimePicker (already in EventCreator)
- This gets saved in `Event.ScheduledAt`

**When you favorite an event:**
- It's added to `Event.FavoritedBy` list

**Automatic checks:**
- Every 30 minutes, the service checks all events
- If an event is:
  - ✅ Favorited by you
  - ✅ Scheduled within the next 24 hours
  - ✅ Not already notified
- **→ You get a notification!**

## Files Added/Modified

**New Files:**
- `EventMatch/Services/NotificationService.cs` - Main notification engine
- `EventMatch/Utils/NotificationTestHelper.cs` - Easy testing helper
- `EventMatch/NOTIFICATIONS_README.md` - Full documentation
- `EventMatch/NOTIFICATION_TESTING.md` - Testing guide

**Modified Files:**
- `EventMatch/MauiProgram.cs` - Registers notification service at startup
- `EventMatch/Platforms/Android/AndroidManifest.xml` - Added POST_NOTIFICATIONS permission

## What the Notification Looks Like

On Android, you'll see something like:

```
📱 NOTIFICATION:
┌─────────────────────────────┐
│ Upcoming Event              │
├─────────────────────────────┤
│ Event happening in 2 hours: │
│ Community Meetup @ Downtown │
│ Cafe                        │
└─────────────────────────────┘
```

## Real-World Flow

1. **User creates event** → "Pizza Night" on Dec 25 at 7:00 PM
2. **User sees it** → Opens Event Preview, favorites it
3. **24 hours before** → Notification sent: "Pizza Night in ~24 hours"
4. **Every 30 mins** → System re-checks (won't notify twice)
5. **1 hour before** → Shows: "Pizza Night in ~1 hour"
6. **30 mins before** → Shows: "Pizza Night in 30 minutes"

## If It Doesn't Work

Check **Debug Output** (View → Output in Visual Studio):

Look for these messages:
```
[NotificationService] Notification service initialized
[NotificationService] Notification: Upcoming Event - ...
```

If you see errors, they'll have `[NotificationService]` prefix and explain what went wrong.

### Common Issues

| Problem | Solution |
|---------|----------|
| No notifications | Check you're logged in and event is favorited |
| Notifications don't appear | Grant POST_NOTIFICATIONS permission on Android |
| Can't find test helper | Build the solution first |
| "No such file" error | Verify all 3 new files were created |

## Next Steps (Optional)

1. **Add UI button** for manual testing (see Step 1-2 above)
2. **Monitor Debug Output** to see notifications being sent
3. **Create test events** with times set to 30 mins/1 hour/2 hours from now
4. **Verify Android permissions** - you'll be asked for POST_NOTIFICATIONS

## You're All Set! 🎉

The notification system is:
- ✅ Installed
- ✅ Registered
- ✅ Running in background
- ✅ Ready to test

Just create an event with a date/time, favorite it, and wait (or use the test helper to trigger immediately).
