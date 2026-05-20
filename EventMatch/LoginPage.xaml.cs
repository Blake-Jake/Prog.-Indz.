using Microsoft.Maui.Controls;
using EventMatch.Services;
using EventMatch.Models;

namespace EventMatch;

public partial class LoginPage : ContentPage
{
    private readonly HybridAuthService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<HybridAuthService>()!;
    }


    private async void OnSignUpTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//SignUpPage");
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        System.Diagnostics.Debug.WriteLine($"[LoginPage] Attempting login with email: {email}");

        if (_authService == null)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] ERROR: HybridAuthService is NULL!");
            await DisplayAlert("Error", "Authentication service not initialized", "OK");
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] ERROR: Email is empty!");
            await DisplayAlert("Error", "Please enter email", "OK");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] ERROR: Password is empty!");
            await DisplayAlert("Error", "Please enter password", "OK");
            return;
        }

        var user = await _authService.LoginAsync(email, password);
        if (user != null)
        {
            await DisplayAlert("Success", "Login successful!", "OK");

            // Store normalized email in session (lowercase and trimmed)
            Session.CurrentUserEmail = email.ToLower().Trim();
            System.Diagnostics.Debug.WriteLine($"[LoginPage] Login successful! Session.CurrentUserEmail set to: {email.ToLower().Trim()}");

            Preferences.Set("UserAlreadyLoggedIn", true);
            // After successful login, attempt to sync local users/groups to cloud
            try
            {
                var hybridGroup = Application.Current?.Handler?.MauiContext?.Services.GetService<HybridGroupService>();
                if (hybridGroup != null)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginPage] Triggering SyncLocalToCloudAsync...");
                    await hybridGroup.InitializeAsync();
                    await hybridGroup.SyncLocalToCloudAsync();
                    System.Diagnostics.Debug.WriteLine("[LoginPage] SyncLocalToCloudAsync finished");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LoginPage] HybridGroupService not available; cannot sync now");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Error while syncing to cloud: {ex.Message}");
            }


            // Ensure a profile exists locally so header can show username
            try
            {
                var userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<EventMatch.Services.UserDatabase>();
                if (userDb != null)
                {
                    var existingProfile = await userDb.GetProfileByEmailAsync(email.ToLower().Trim());
                    if (existingProfile == null)
                    {
                        var username = email.ToLower().Trim().Split('@')[0];
                        await userDb.SaveProfileAsync(new EventMatch.Models.Profile { UserEmail = email.ToLower().Trim(), Username = username });
                    }
                }

                if (Application.Current?.MainPage is AppShell shell)
                {
                    await shell.UpdateUserHeaderAsync(email.ToLower().Trim());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Error updating header/profile: {ex.Message}");
            }

            await Shell.Current.GoToAsync("//EventPreview");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] Login failed for email: {email}");
            await DisplayAlert("Error", "Invalid email or password.", "OK");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        EmailEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
    }
}
