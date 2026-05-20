using EventMatch.Models;
using EventMatch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using SQLite;
using System.Collections.Generic;
using System.Linq;

namespace EventMatch;

[QueryProperty(nameof(Email), "email")]
public partial class ProfilePage : ContentPage
{
    private readonly UserDatabase _userDb;
    private EventMatch.Models.Profile? _currentProfile;
    UploadingImage uploadImage { get; set; }

    private string _photoBase64 = "";
    private List<string> _selectedTags = new List<string>();

    [PrimaryKey, AutoIncrement]
    public new int Id { get; set; }

    [Unique]
    public string UserEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int RadiusKm { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;

    public ProfilePage()
    {
        InitializeComponent();
        _userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<UserDatabase>()!;

        if (RadiusPicker != null && RadiusPicker.Items.Count > 0)
            RadiusPicker.SelectedIndex = 0;

        uploadImage = new UploadingImage();
        // Make the profile image area tappable to change/add photo
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnAddPhotoClicked;
        ProfileFrame.GestureRecognizers.Add(tap);
    }

    // email is passed in navigation as ?email=someone@example.com
    public string Email { get; set; } = string.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrWhiteSpace(Email))
            Email = Session.CurrentUserEmail;

        if (string.IsNullOrWhiteSpace(Email))
            return;

        _currentProfile = await _userDb.GetProfileByEmailAsync(Email);

        if (_currentProfile != null)
        {
            UsernameEntry.Text = _currentProfile.Username;
            DescriptionEditor.Text = _currentProfile.Description;

            if (!string.IsNullOrEmpty(_currentProfile.PhotoPath))
            {
                var bytes = Convert.FromBase64String(_currentProfile.PhotoPath);
                ProfileImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                PhotoOverlayLabel.IsVisible = false;
            }
            else
            {
                ProfileImage.Source = "profile-placeholder.png";
                PhotoOverlayLabel.IsVisible = true;
                PhotoOverlayLabel.Text = "Add Photo";
            }

            var index = RadiusPicker.Items.IndexOf(_currentProfile.RadiusKm.ToString());
            RadiusPicker.SelectedIndex = index >= 0 ? index : 0;

            // Load selected tags
            _selectedTags = _currentProfile.GetPreferredTags();
            DisplaySelectedTags();
        }
    }

    private void DisplaySelectedTags()
    {
        var tagLayout = this.FindByName<FlexLayout>("SelectedTagsFlexLayout");
        var tagContainer = this.FindByName<VerticalStackLayout>("SelectedTagsContainer");

        if (tagLayout == null || tagContainer == null)
            return;

        tagLayout.Children.Clear();

        if (_selectedTags.Count == 0)
        {
            tagContainer.IsVisible = false;
            return;
        }

        tagContainer.IsVisible = true;

        foreach (var tag in _selectedTags.ToList())
        {
            var tagToRemove = tag; // Capture for closure

            // Create the remove label with tap gesture
            var removeLabel = new Label
            {
                Text = "×",
                TextColor = Colors.White,
                FontSize = 16,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                _selectedTags.Remove(tagToRemove);
                DisplaySelectedTags();
            };
            removeLabel.GestureRecognizers.Add(tapGesture);

            // Create tag content with label and button
            var tagContent = new HorizontalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(8, 4, 6, 4),
                Children =
                {
                    new Label
                    {
                        Text = $"#{tag}",
                        TextColor = Colors.White,
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0),
                        VerticalTextAlignment = TextAlignment.Center
                    },
                    removeLabel
                }
            };

            // Wrap in a Frame with explicit sizing
            var tagFrame = new Frame
            {
                Content = tagContent,
                BackgroundColor = Color.FromArgb("#A259FF"),
                CornerRadius = 16,
                Padding = new Thickness(0),
                HasShadow = false,
                Margin = new Thickness(4, 4, 4, 4),
                BorderColor = Color.FromArgb("#A259FF"),
                HeightRequest = 32,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };

            tagLayout.Add(tagFrame);
        }
    }

     private async void OnSelectTagsClicked(object sender, EventArgs e)
    {
        // Define 10 generic tags that are always available
        var genericTags = new List<string>
        {
            "sports",
            "music",
            "food",
            "outdoor",
            "fitness",
            "art",
            "gaming",
            "tech",
            "social",
            "learning"
        };

        // Get tags from events
        var store = new EventStore();
        var events = store.LoadAll();

        // Count tag frequencies from events
        var tagFrequency = new Dictionary<string, int>();
        foreach (var evt in events)
        {
            foreach (var tag in evt.Tags ?? new List<Tag>())
            {
                var tagName = tag.Name?.ToLower().Trim() ?? "";
                if (!string.IsNullOrEmpty(tagName))
                {
                    if (tagFrequency.ContainsKey(tagName))
                        tagFrequency[tagName]++;
                    else
                        tagFrequency[tagName] = 1;
                }
            }
        }

        // Combine generic tags with event tags
        var allTags = new HashSet<string>(genericTags);
        foreach (var eventTag in tagFrequency.Keys)
        {
            allTags.Add(eventTag);
        }

        // Sort: first by whether they're in events (frequency), then alphabetically
        var sortedTags = allTags
            .OrderByDescending(x => tagFrequency.ContainsKey(x) ? tagFrequency[x] : 0) // Events with tags first
            .ThenBy(x => x) // Then alphabetically
            .ToList();

        // Show action sheet with all available tags
        var action = await DisplayActionSheet(
            "Select a tag to add",
            "Cancel",
            null,
            sortedTags.ToArray()
        );

        if (action != null && action != "Cancel")
        {
            if (!_selectedTags.Contains(action))
            {
                _selectedTags.Add(action);
                DisplaySelectedTags();
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnFriendsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("FriendsPage");
    }

    private async void OnMyEventsClicked(object sender, EventArgs e)
    {
        // Navigate to a page that shows all events (new page)
        await Shell.Current.GoToAsync("AllEventsPage");
    }

    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {

        var img = await uploadImage.OpenMediaPickerAsync();
        if (img == null)
            return;

        var imageFile = await uploadImage.Upload(img);

        _photoBase64 = imageFile.ByteBase64;

        var bytes = Convert.FromBase64String(_photoBase64);
        ProfileImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        PhotoOverlayLabel.IsVisible = false;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Email))
            Email = Session.CurrentUserEmail;

        if (string.IsNullOrWhiteSpace(Email))
        {
            await DisplayAlert("Error", "No user is logged in.", "OK");
            return;
        }

        var radius = 10;
        if (RadiusPicker.SelectedIndex >= 0 && int.TryParse(RadiusPicker.Items[RadiusPicker.SelectedIndex], out var r))
            radius = r;

        var profileToSave = new Profile
        {
            UserEmail = Email,
            Username = UsernameEntry.Text?.Trim() ?? string.Empty,
            Tag = "", // Keep for backward compatibility
            RadiusKm = radius,
            Description = DescriptionEditor.Text ?? string.Empty,
            PhotoPath = string.IsNullOrEmpty(_photoBase64)
                ? _currentProfile?.PhotoPath ?? ""
                : _photoBase64
        };

        // Save the selected tags
        profileToSave.SetPreferredTags(_selectedTags);

        await _userDb.SaveProfileAsync(profileToSave);

        _currentProfile = await _userDb.GetProfileByEmailAsync(Email);
        await DisplayAlert("Saved", "Profile saved.", "OK");
    }

    private async void OnDeleteAllDataClicked(object sender, EventArgs e)
    {
        // Double confirmation - user must be very sure
        bool firstConfirm = await DisplayAlert(
            "⚠️ WARNING",
            "Delete ALL users and groups from Cloud AND Local?\n\nThis action CANNOT be undone!",
            "Yes, Delete Everything",
            "Cancel"
        );

        if (!firstConfirm) return;

        // Second confirmation
        bool secondConfirm = await DisplayAlert(
            "🔥 FINAL WARNING",
            "This is your LAST chance. Are you ABSOLUTELY sure?",
            "YES, DELETE EVERYTHING",
            "Cancel"
        );

        if (!secondConfirm) return;

        try
        {
            // Show loading indicator
            await DisplayAlert("⏳ Deleting...", "Please wait, this may take a moment...", "");

            // Get HybridGroupService from DI
            var hybridGroupService = Application.Current?.Handler?.MauiContext?.Services.GetService<HybridGroupService>();

            if (hybridGroupService == null)
            {
                await DisplayAlert("❌ Error", "HybridGroupService not available", "OK");
                return;
            }

            // Delete all data
            bool success = await hybridGroupService.DeleteAllDataAsync();

            await DisplayAlert(
                success ? "✅ SUCCESS" : "⚠️ Partial Success",
                success
                    ? "All users and groups deleted from Cloud and Local database!"
                    : "Deletion completed with some warnings. Check debug logs.",
                "OK"
            );

            if (success)
            {
                // Clear session and return to login
                Session.CurrentUserEmail = "";
                await Shell.Current.GoToAsync("///login");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("❌ Error", $"Deletion failed:\n{ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] Delete error: {ex}");
        }
    }
}