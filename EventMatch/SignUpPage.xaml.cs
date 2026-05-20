using Microsoft.Maui.Controls;
using EventMatch.Models;
using EventMatch.Services;

namespace EventMatch;

public partial class SignUpPage : ContentPage
{
    private readonly UserDatabase _userDb;
    private readonly CloudAuthService _cloudAuth;

    public SignUpPage()
    {
        InitializeComponent();
        _userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<UserDatabase>()!;
        _cloudAuth = Application.Current?.Handler?.MauiContext?.Services.GetService<CloudAuthService>()!;
    }


    private async void OnSignInTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please fill all fields.", "OK");
            return;
        }
        if (password != confirm)
        {
            await DisplayAlert("Error", "Passwords do not match.", "OK");
            return;
        }
        var existing = await _userDb.GetUserByEmailAsync(email);
        if (existing != null)
        {
            await DisplayAlert("Error", "User already exists.", "OK");
            return;
        }
        // Try to register user in cloud first
        var newUser = new User { Email = email, Password = password };
        try
        {
            var registered = await _cloudAuth.RegisterUserAsync(newUser);
            if (!registered)
            {
                await DisplayAlert("Error", "Registration failed on server.", "OK");
                return;
            }
        }
        catch
        {
            await DisplayAlert("Error", "Registration failed (network/server error).", "OK");
            return;
        }

        // Save locally as cache after successful cloud registration
        await _userDb.AddUserAsync(newUser);
        await DisplayAlert("Success", "Account created!", "OK");
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
