using EventMatch.Services;
using System.IO;
using Microsoft.Maui.Controls;
using Maui.GoogleMaps.Hosting;
using Maui.GoogleMaps;
using System.Linq;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif
#if ANDROID
using Android.Content.PM;
#endif

namespace EventMatch
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
#if WINDOWS
Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
{
    var nativeWindow = handler.PlatformView;
    nativeWindow.Activate();

    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
    var appWindow = AppWindow.GetFromWindowId(windowId);

    // Set a larger default window size for desktop so profile UI fits
    appWindow.Resize(new SizeInt32(1200, 900));

    // Allow resizing and maximizing on desktop windows
    var presenter = appWindow.Presenter as OverlappedPresenter;
    if (presenter != null)
    {
        presenter.IsResizable = true;
        presenter.IsMaximizable = true;
        presenter.IsMinimizable = true;
    }
});
#endif
#if ANDROID
            // Configure Android to match phone emulator dimensions (portrait orientation)
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
            {
                var nativeWindow = handler.PlatformView;
                if (nativeWindow is Android.App.Activity activity)
                {
                    // Enforce portrait orientation to match Windows emulator layout
                    activity.RequestedOrientation = ScreenOrientation.Portrait;
                }
            });
#endif
            builder
                .UseMauiApp<App>()
#if ANDROID
                .UseGoogleMaps()
#elif IOS
                .UseGoogleMaps("AIzaSyCCbpV0ZR89ECwB6jakOR31PtZOpiUV5xA")
#endif

                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Allow disabling local DB / hybrid services when running cloud-only
            var cloudOnly = Environment.GetEnvironmentVariable("EVENTMATCH_CLOUD_ONLY") == "1";
            if (cloudOnly)
            {
                System.Diagnostics.Debug.WriteLine("[MauiProgram] Running in CLOUD-ONLY mode: local DB and hybrid services will NOT be registered");
            }

            // SQLite DB path - use standard app data directory
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "users.db3");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using database path: {dbPath}");

            // Register cloud-only services always
            builder.Services.AddSingleton<CloudAuthService>();
            builder.Services.AddSingleton<CloudGroupService>();
            builder.Services.AddSingleton<CloudEventService>();
            builder.Services.AddSingleton<NotificationService>();

            // Register local event store and sync service
            builder.Services.AddSingleton<EventStore>();
            builder.Services.AddSingleton<EventSyncService>();

            // Register local DB and hybrid services only when not in cloud-only mode
            if (!cloudOnly)
            {
                builder.Services.AddSingleton(new UserDatabase(dbPath));

                builder.Services.AddSingleton<HybridAuthService>(sp =>
                {
                    var cloudAuth = sp.GetRequiredService<CloudAuthService>();
                    var userDb = sp.GetRequiredService<UserDatabase>();
                    return new HybridAuthService(cloudAuth, userDb);
                });

                builder.Services.AddSingleton<HybridGroupService>(sp =>
                {
                    var cloudGroup = sp.GetRequiredService<CloudGroupService>();
                    var userDb = sp.GetRequiredService<UserDatabase>();
                    var cloudAuth = sp.GetRequiredService<CloudAuthService>();
                    return new HybridGroupService(cloudGroup, userDb, cloudAuth);
                });

                // Register hybrid event store for cross-device event sync
                builder.Services.AddSingleton<HybridEventStore>(sp =>
                {
                    var cloudEventService = sp.GetRequiredService<CloudEventService>();
                    var eventStore = sp.GetRequiredService<EventStore>();
                    return new HybridEventStore(cloudEventService, eventStore);
                });
            }

            var app = builder.Build();

            // Initialize hybrid services in background so cloud connectivity is checked
            // and local->cloud sync can run. This ensures groups created on one platform
            // are pushed to the remote backend and visible on other platforms.
            try
            {
                var hybridGroup = app.Services.GetService<HybridGroupService>();
                if (hybridGroup != null)
                {
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await hybridGroup.InitializeAsync();
                            await hybridGroup.SyncLocalToCloudAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] HybridGroupService init/sync error: {ex.Message}");
                        }
                    });
                }
            }
            catch { }

            // Ensure navigation strings remain valid after renaming the page class.
            // Register both the old route name and the new class-based route.
            Routing.RegisterRoute("Profile", typeof(ProfilePage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            // Register FriendsPage so Shell.Current.GoToAsync("FriendsPage") can resolve the route.
            Routing.RegisterRoute("FriendsPage", typeof(FriendsPage));
            // Register EventPreview for events display
            Routing.RegisterRoute("EventPreview", typeof(EventPreview));
            // Register EventMapControl for map display
            Routing.RegisterRoute("EventMapControl", typeof(EventMapControl));
            // Register AllEventsPage for user's events
            Routing.RegisterRoute("MyEventsPage", typeof(AllEventsPage));
            // Register EventCreator for creating groups
            Routing.RegisterRoute("EventCreator", typeof(EventCreator));
            // Register LoginPage for authentication
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            // Register SignUpPage for registration
            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
            // Register GroupsPage for group functionality
            Routing.RegisterRoute("GroupsPage", typeof(GroupsPage));
            // Register group chat page
            Routing.RegisterRoute("GroupChatPage", typeof(GroupChatPage));
            // Register edit group page
            Routing.RegisterRoute("EditGroupPage", typeof(EditGroupPage));

            // Optional maintenance actions triggered via environment variables (for debugging/cleanup)
            // Set EVENTMATCH_MAINTENANCE_SEED_USERS=1 to seed test users (password 'asd')
            // Set EVENTMATCH_MAINTENANCE_DELETE_GROUP=<group name> to delete a group by name (cloud + local)

            var seedFlag = Environment.GetEnvironmentVariable("EVENTMATCH_MAINTENANCE_SEED_USERS");
            var deleteGroupName = Environment.GetEnvironmentVariable("EVENTMATCH_MAINTENANCE_DELETE_GROUP");
            var pushUsersFlag = Environment.GetEnvironmentVariable("EVENTMATCH_MAINTENANCE_PUSH_USERS");
            if (seedFlag == "1" || !string.IsNullOrEmpty(deleteGroupName) || pushUsersFlag == "1")
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var userDb = app.Services.GetService<UserDatabase>();
                        var cloudGroup = app.Services.GetService<CloudGroupService>();
                        var cloudAuth = app.Services.GetService<CloudAuthService>();

                        if (seedFlag == "1" && userDb != null)
                        {
                            var emails = new[] { "asd@gmail.com", "wer", "qwe", "asd", "phone" };
                            await userDb.SeedUsersAsync(emails, "asd");
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Seeded {emails.Length} users into local DB");
                        }

                        // Push all local users to cloud (register). Set EVENTMATCH_MAINTENANCE_PUSH_USERS=1
                        if (pushUsersFlag == "1" && userDb != null && cloudAuth != null)
                        {
                            var users = await userDb.GetAllUsersAsync();
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Pushing {users.Count} local users to cloud");
                            foreach (var u in users)
                            {
                                try
                                {
                                    var ok = await cloudAuth.RegisterUserAsync(u);
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Register user {u.Email} -> {ok}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Error registering {u.Email}: {ex.Message}");
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(deleteGroupName))
                        {
                            // Try delete from cloud by finding group with matching name
                            if (cloudGroup != null)
                            {
                                var all = await cloudGroup.GetAllGroupsAsync();
                                var found = all?.FirstOrDefault(g => g.Name == deleteGroupName);
                                if (found != null)
                                {
                                    var ok = await cloudGroup.DeleteGroupAsync(found.Id);
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Cloud delete group '{deleteGroupName}' (id {found.Id}) -> {ok}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Cloud group '{deleteGroupName}' not found");
                                }
                            }

                            // Also delete locally by name
                            if (userDb != null)
                            {
                                await userDb.DeleteGroupByNameAsync(deleteGroupName);
                                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Deleted local group (if existed) with name '{deleteGroupName}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MauiProgram] Maintenance actions failed: {ex.Message}");
                    }
                });
            }

            // Initialize notification service for upcoming event reminders
            try
            {
                var notificationService = app.Services.GetService<NotificationService>();
                if (notificationService != null)
                {
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await notificationService.InitializeAsync();
                            System.Diagnostics.Debug.WriteLine("[MauiProgram] Notification service initialized");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Notification service init error: {ex.Message}");
                        }
                    });
                }
            }
            catch { }

            return app;
        }
    }
}
