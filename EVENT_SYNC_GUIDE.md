# 🔄 Event Sync - Windows ↔ Android

## Overview

The Event Sync feature allows you to synchronize events between Windows and Android devices **locally** without relying on the cloud server. Events are exported to a JSON file that can be easily shared between devices.

## How It Works

### File-Based Sync
- **Export**: Events are saved to `eventmatch_sync.json` in your Documents folder
- **Import**: New events from the sync file are merged into your local store (no duplicates)
- **Location**: 
  - **Windows**: `C:\Users\YourUsername\Documents\eventmatch_sync.json`
  - **Android**: App cache directory

### Key Features
✅ **No Cloud Required** - Works completely offline  
✅ **Automatic Deduplication** - Won't import events you already have  
✅ **One-Way Sync** - Export on one device, import on another  
✅ **Simple File Sharing** - Just copy the JSON file between devices

## Step-by-Step Guide

### Scenario: Create events on Windows, view on Android

#### On Windows:
1. Create some events in Event Creator
2. Open the menu → "Sync Events"
3. Click "Export Events to File"
4. A dialog shows where the file was saved (Documents folder)
5. The file path is displayed in the sync info

#### Transfer File:
You can transfer the file using any method:
- **USB Cable** - Copy file to Android via USB
- **Email** - Email the sync file to yourself
- **Cloud Drive** - Upload to Google Drive, OneDrive, etc.
- **Bluetooth** - Share via Bluetooth
- **File Transfer App** - Use a file sync app like Syncthing

#### On Android:
1. Place the `eventmatch_sync.json` file in the app's cache directory
   - Use a file manager to navigate to: `/data/data/com.companyname.eventmatch/cache/`
   - Or use a file manager app to put it there
2. Open the app → Menu → "Sync Events"
3. Click "Import Events from File"
4. The dialog shows how many new events were imported
5. Your events are now in the app!

## Sync File Format

The exported JSON contains:
```json
{
  "ExportedAt": "2026-12-20T15:30:00Z",
  "Platform": "Windows",
  "EventCount": 5,
  "Events": [
    {
      "Id": "unique-event-id",
      "Details": "Event name",
      "ScheduledAt": "2026-12-25T19:00:00",
      "LocationAddress": "Event location",
      "FavoritedBy": ["user@email.com"],
      "Tags": [...],
      ...
    },
    ...
  ]
}
```

## UI Guide

### Sync Events Page

**📤 EXPORT EVENTS Section**
- Click "Export Events to File" to save all your events
- Creates/overwrites `eventmatch_sync.json`
- Shows confirmation with file location

**📥 IMPORT EVENTS Section**
- Click "Import Events from File" to load events
- Only imports NEW events (won't duplicate existing ones)
- Shows count of imported events

**ℹ️ SYNC FILE INFO Section**
- Shows file location
- Shows file size
- Shows number of events in the file
- Shows when it was last exported

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No Sync File" error on import | Export events on Windows first, then copy the file to Android |
| Can't find file on Android | Use a file manager app and navigate to `/data/data/com.companyname.eventmatch/cache/` |
| Events not importing | Check that the file is named exactly `eventmatch_sync.json` |
| "Duplicate events" appearing | The sync service prevents duplicates automatically - same event ID won't be imported twice |
| File transfer issues | Try using a cloud service (Google Drive, OneDrive) to transfer the file |

## Advanced Usage

### Manual File Transfer

**Windows:**
1. Export events
2. File location: `C:\Users\[Username]\Documents\eventmatch_sync.json`
3. Copy to USB drive, email, cloud, etc.

**Android:**
Using a file manager app:
1. Download the sync file
2. Open file manager
3. Navigate to: `Android/data/com.companyname.eventmatch/cache/` OR `/data/data/com.companyname.eventmatch/cache/`
4. Paste `eventmatch_sync.json`
5. Open app and import

### Backup Strategy
- Export your events regularly to create backups
- Keep the sync file as a backup of your events
- Can restore events by importing the file

## Limitations

- **One-way sync** - Only manual export/import (not automatic)
- **Manual transfer** - You need to physically transfer the file
- **Overwrite risk** - Exporting overwrites the previous sync file
- **No conflict resolution** - If same event modified on both devices, last import wins

## Future Improvements

Potential enhancements (not yet implemented):
- Automatic sync when devices connect
- Delta sync (only sync new events)
- Conflict resolution
- Cloud integration (when server works properly)
- Scheduled automatic exports

## Technical Details

### EventSyncService API

```csharp
// Export all events to sync file
string filePath = _syncService.ExportEvents();

// Import events from sync file
int importedCount = _syncService.ImportEvents();

// Check if sync file exists
bool exists = _syncService.SyncFileExists();

// Get sync file information
string info = _syncService.GetSyncInfo();

// Get sync file path
string path = _syncService.GetSyncFilePath();
```

### File Location Detection

**Windows:**
```
C:\Users\[YourUsername]\Documents\eventmatch_sync.json
```

**Android:**
```
/data/data/com.companyname.eventmatch/cache/eventmatch_sync.json
```

## Support

For issues with file transfer or sync problems:
1. Check the Debug Output for [EventSync] messages
2. Verify file exists at the expected location
3. Ensure file is named exactly `eventmatch_sync.json`
4. Try exporting and reimporting to test the mechanism
