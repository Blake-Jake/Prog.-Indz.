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
            await DisplayAlertAsync("Error", "Authentication service not initialized", "OK");
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] ERROR: Email is empty!");
            await DisplayAlertAsync("Error", "Please enter email", "OK");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] ERROR: Password is empty!");
            await DisplayAlertAsync("Error", "Please enter password", "OK");
            return;
        }

        var user = await _authService.LoginAsync(email, password);
        if (user != null)
        {
            await DisplayAlertAsync("Success", "Login successful!", "OK");

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
            await DisplayAlertAsync("Error", "Invalid email or password.", "OK");
        }
    }
    private async Task LoginWithGoogle()
    {
        try
        {
            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?client_id=713828107524-71eksm0nji1d4ii19tlnn7ans6fa5n9q.apps.googleusercontent.com" +
                "&redirect_uri=eventmatch://auth" +
                "&response_type=token" +
                "&scope=openid%20profile%20email");

            var callbackUrl = new Uri("eventmatch://auth");

            System.Diagnostics.Debug.WriteLine("BEFORE AUTH");

            var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

            System.Diagnostics.Debug.WriteLine("AFTER AUTH");

            var accessToken = result?.AccessToken;

            await DisplayAlert("SUCCESS", accessToken ?? "NO TOKEN", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", ex.ToString(), "OK");

            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }
    /*private async Task LoginWithGoogle()
    {
        var authUrl = new Uri(
            "https://accounts.google.com/o/oauth2/v2/auth" +
            "?client_id=713828107524-71eksm0nji1d4ii19tlnn7ans6fa5n9q.apps.googleusercontent.com" +
            "&redirect_uri=EventMatch://auth" +
            "&response_type=token" +
            "&scope=openid profile email");

        var callbackUrl = new Uri("EventMatch://auth");

        var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

        var accessToken = result?.AccessToken;

        if (accessToken != null)
        {
            var email = result?.Properties.ContainsKey("email") == true
                ? result.Properties["email"]
                : "emattt254@gmail.com";

            Session.CurrentUserEmail = email.ToLower().Trim();

            Preferences.Set("UserAlreadyLoggedIn", true);

            await DisplayAlertAsync("Success", "Google login successful!", "OK");

            await Shell.Current.GoToAsync("//EventPreview");
        }
        else
        {
            await DisplayAlertAsync("Error", "Google login failed", "OK");
        }
    }*/

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        await LoginWithGoogle();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        EmailEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
    }
}
