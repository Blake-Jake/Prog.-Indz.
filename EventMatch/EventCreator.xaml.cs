using EventMatch.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System;
using System.IO;
using EventMatch.Models;
using EventMatch.Services;
using Maui.GoogleMaps;

namespace EventMatch;

public partial class EventCreator : ContentPage
{
    double selectedLat;
    double selectedLng;

    private UploadingImage _uploader = new UploadingImage();
    private string? _pickedImageBase64;
    private HybridEventStore? _hybridEventStore;

    public EventCreator()
    {
        InitializeComponent();

        // Get HybridEventStore from DI
        _hybridEventStore = Application.Current?.Handler?.MauiContext?.Services.GetService<HybridEventStore>();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private string? _selectedAddress;

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Create new event and add to store
        var details = EventDetailsEditor?.Text ?? string.Empty;

        // Find pickers by name (avoids depending on generated fields)
        var datePicker = this.FindByName<DatePicker>("EventDatePicker");
        var timePicker = this.FindByName<TimePicker>("EventTimePicker");

        var date = datePicker?.Date ?? DateTime.Now.Date;
        var time = timePicker?.Time ?? DateTime.Now.TimeOfDay;
        var scheduled = date.Date + time;

        // Parse tags from entry
        var tagsRaw = this.FindByName<Entry>("TagsEntry")?.Text ?? string.Empty;
        var tags = tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                          .Select(t => t.TrimStart('#'))  // Remove # prefix if present
                          .Where(t => !string.IsNullOrEmpty(t))  // Filter out empty tags
                          .Select(t => new Tag { Name = t })
                          .ToList();

        var newEvent = new Event
        {
            Details = details,
            ImageBase64 = _pickedImageBase64 ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            ScheduledAt = scheduled,
            Latitude = selectedLat,
            Longitude = selectedLng,
            LocationAddress = _selectedAddress ?? string.Empty,
            Tags = tags
        };

        if (_hybridEventStore != null)
        {
            // Initialize with current user if available
            var currentUser = Session.CurrentUserEmail;
            if (!string.IsNullOrEmpty(currentUser))
            {
                await _hybridEventStore.InitializeAsync(currentUser);
            }

            // Use hybrid store (syncs to cloud if available, otherwise saves locally)
            await _hybridEventStore.AddAsync(newEvent);
        }
        else
        {
            // Fallback to local store if hybrid not available
            var localStore = new EventStore();
            localStore.Add(newEvent);
        }

        await DisplayAlert("Saved", "Event saved." + (_hybridEventStore?.IsCloudAvailable == true ? " (Synced to cloud)" : " (Local only)"), "OK");
        await Shell.Current.GoToAsync("..", true);
    }

    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        var file = await _uploader.OpenMediaPickerAsync();
        if (file == null)
            return;

        var imageFile = await _uploader.Upload(file);
        if (imageFile == null)
            return;

        _pickedImageBase64 = imageFile.ByteBase64;

        try
        {
            var bytes = Convert.FromBase64String(_pickedImageBase64);
            EventImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            if (ImageOverlayLabel != null)
                ImageOverlayLabel.IsVisible = false;
        }
        catch { }
    }

    private async void OnPickLocationClicked(object sender, EventArgs e)
    {
        var mapPage = new EventMapControl();

        mapPage.LocationSelected = async (lat, lng) =>
        {
            selectedLat = lat;
            selectedLng = lng;

#if WINDOWS
            var address = await ReverseGeocodeAsync(lat, lng);
#else
        var geocoder = new Geocoder();
        var positions = await geocoder.GetAddressesForPositionAsync(new Position(lat, lng));
        var address = positions.FirstOrDefault() ?? $"{lat:F4}, {lng:F4}";
#endif

            _selectedAddress = address;
            LocationLabel.Text = $"📍 {address}";
        };

        await Navigation.PushAsync(mapPage);
    }

#if WINDOWS
    private async Task<string> ReverseGeocodeAsync(double lat, double lng)
    {
        try
        {
            var apiKey = "AIzaSyA2lGsQdCDdzQlfhZWYYPVEPye9ixinTvM";
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&key={apiKey}";

            using var client = new System.Net.Http.HttpClient();
            var json = await client.GetStringAsync(url);

            // Parse the first result's formatted_address
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");

            if (results.GetArrayLength() > 0)
            {
                return results[0].GetProperty("formatted_address").GetString()
                       ?? $"{lat:F4}, {lng:F4}";
            }
        }
        catch { }

        return $"{lat:F4}, {lng:F4}";
    }
#endif
}
