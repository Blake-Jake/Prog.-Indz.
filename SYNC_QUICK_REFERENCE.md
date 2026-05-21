# Event Sync - Quick Reference

## The Flow

```
WINDOWS SIDE:
┌─────────────────────┐
│  Create Events      │
│  (Event Creator)    │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│  Menu → Sync Events │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│  Click "Export"     │
└──────────┬──────────┘
           │
           ↓
┌──────────────────────────────────┐
│ eventmatch_sync.json created in  │
│ Documents folder                 │
└──────────┬───────────────────────┘
           │
           ↓ (Copy file)
           │
    ┌──────┴──────┐
    │ USB/Email/  │
    │ Cloud Drive │
    └──────┬──────┘
           │
           ↓
ANDROID SIDE:
┌─────────────────────────────────┐
│ Place file in:                  │
│ /data/data/com.companyname...   │
│ /eventmatch/cache/              │
└──────────┬──────────────────────┘
           │
           ↓
┌─────────────────────┐
│  Menu → Sync Events │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│  Click "Import"     │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│  Events appear! ✅  │
│  in Event Preview   │
└─────────────────────┘
```

## Step-by-Step

### Export (Windows)

1. **Have some events created**
   - Use Event Creator to add events
   - Or they were created previously

2. **Open Menu**
   - Click hamburger menu (☰)
   - Scroll down to "Sync Events"

3. **Click "Export Events to File"**
   - Button shows "Exporting..."
   - Confirmation dialog appears

4. **See the success message**
   - Shows file path in Documents

5. **Locate the file**
   - Go to: `C:\Users\[YourName]\Documents\`
   - Look for: `eventmatch_sync.json`

### Transfer File

Choose ONE method:

**Method 1: USB Cable (Easiest)**
```
1. Connect Android to Windows via USB
2. Use Android File Transfer or Windows Explorer
3. Copy eventmatch_sync.json
4. Navigate to: /data/data/com.companyname.eventmatch/cache/
5. Paste the file
```

**Method 2: Email**
```
1. Email the file to yourself from Windows
2. Open email on Android phone
3. Download the attachment
4. Open file manager, navigate to cache folder
5. Move file there
```

**Method 3: Cloud Drive**
```
1. Upload file to Google Drive / OneDrive on Windows
2. Open on Android phone
3. Download file
4. Move to cache folder using file manager
```

**Method 4: Bluetooth**
```
1. Right-click file → Send → Bluetooth
2. Select Android device
3. Accept on Android
4. Move file to cache folder
```

### Import (Android)

1. **Have the sync file in place**
   - File: `eventmatch_sync.json`
   - Location: `/data/data/com.companyname.eventmatch/cache/`

2. **Open the App Menu**
   - Click hamburger menu (☰)
   - Scroll to "Sync Events"

3. **Click "Import Events from File"**
   - Button shows "Importing..."
   - Success message shows count

4. **Check your events**
   - Go to "Events" or "My Events"
   - New events should appear! ✅

5. **Verify sync info**
   - Event count should have increased
   - Last imported timestamp shown

## File Manager Navigation (Android)

### Using Google Files App:
```
1. Open Files app
2. Tap "Browse"
3. Scroll to "Android"
4. Tap "data"
5. Find folder starting with "com.companyname"
6. Navigate to "cache" folder
7. Paste eventmatch_sync.json here
```

### Using Developer Mode (Direct Path):
```
1. Open file manager
2. Enable "Show Hidden Files" in settings
3. Type path: /data/data/com.companyname.eventmatch/cache/
4. Or use root access if available
5. Paste file here
```

## Verification Checklist

After sync, verify:

- [ ] Export button works on Windows
- [ ] File created in Documents folder
- [ ] File is valid JSON (can open in text editor)
- [ ] File contains your events
- [ ] File successfully transferred to Android
- [ ] File placed in correct cache folder
- [ ] Import button works on Android
- [ ] Success message shows "X new events imported"
- [ ] Events appear in Event Preview/My Events
- [ ] No duplicate events visible

## Common Paths

**Windows:**
```
C:\Users\tomas\Documents\eventmatch_sync.json
(Replace 'tomas' with your username)
```

**Android Cache:**
```
/data/data/com.companyname.eventmatch/cache/eventmatch_sync.json
```

**Android Alternative:**
```
/storage/emulated/0/Android/data/com.companyname.eventmatch/cache/eventmatch_sync.json
```

## Quick Troubleshooting

| Issue | Fix |
|-------|-----|
| "Export" button stuck | Wait a few seconds, then try again |
| File not appearing | Check Documents folder, refresh explorer |
| "No Sync File" on Android | Make sure file was exported first |
| Import shows "0 imported" | Events already exist (check for duplicates) |
| Can't find cache folder | Use file manager's search function |
| File transfer failed | Try a different method (email, cloud drive) |
| App crashes on import | Check file isn't corrupted, re-export |

## File Format Preview

```json
{
  "ExportedAt": "2026-12-20T15:30:45Z",
  "Platform": "Windows",
  "EventCount": 3,
  "Events": [
    {
      "Id": "event-123",
      "Details": "Pizza Night",
      "ScheduledAt": "2026-12-25T19:00:00",
      "LocationAddress": "Downtown Pizzeria",
      "FavoritedBy": ["user@email.com"],
      ...
    },
    ...
  ]
}
```

---

**Key Takeaway:** Export on Windows → Copy file → Import on Android = Same events everywhere! 🎉
