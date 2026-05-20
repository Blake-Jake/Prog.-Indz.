using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using EventMatch.Services;
using EventMatch.Models;
using EventMatch.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EventMatch;

public partial class EventPreview : ContentPage
{
    public EventPreview()
    {
        InitializeComponent();

    }

    // When showing a single event (e.g. opened from My Events), we use this constructor
    private bool _singleEventMode = false;

    public EventPreview(EventPreviewItem item, bool hideControls = true)
    {
        InitializeComponent();

        _singleEventMode = true;
        _items = new List<EventPreviewItem> { item };
        _currentIndex = 0;

        if (hideControls)
            HideCycleAndFavoriteControls();

        UpdateDisplayedEvent();
    }

    private void HideCycleAndFavoriteControls()
    {
        var fav = this.FindByName<Button>("FavoriteButton");
        if (fav != null) fav.IsVisible = false;

        var cycle = this.FindByName<Button>("CycleEventButton");
        if (cycle != null) cycle.IsVisible = false;

        var refresh = this.FindByName<Button>("RefreshButton");
        if (refresh != null) refresh.IsVisible = false;
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        OnAppearing();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load all events, and exclude those already favorited by the current user.
        var currentUser = Session.CurrentUserEmail;
        var store = new EventStore();
        var allEvents = store.LoadAll();

        // Get user's preferred tags for filtering/prioritization
        var userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<UserDatabase>();
        var userProfile = userDb != null ? await userDb.GetProfileByEmailAsync(currentUser) : null;
        var userPreferredTags = userProfile?.GetPreferredTags() ?? new List<string>();

        // Separate events into matching and non-matching
        var matchingEvents = new List<Event>();
        var nonMatchingEvents = new List<Event>();

        foreach (var e in allEvents)
        {
            // Skip already favorited events
            if (e.FavoritedBy != null && e.FavoritedBy.Contains(currentUser))
                continue;

            // Check if event has tags matching user's preferences
            var eventTags = e.Tags?.Select(t => t.Name?.ToLower().Trim() ?? "").ToList() ?? new List<string>();
            bool hasMatchingTag = userPreferredTags.Count > 0 &&
                                  userPreferredTags.Any(userTag => eventTags.Contains(userTag.ToLower()));

            if (hasMatchingTag)
                matchingEvents.Add(e);
            else
                nonMatchingEvents.Add(e);
        }

        // Combine: matching first (prioritized), then non-matching
        var prioritizedEvents = matchingEvents.Concat(nonMatchingEvents).ToList();

        _items = prioritizedEvents
            .Select(e => new EventPreviewItem
            {
                Details = e.Details,
                CreatedAt = e.CreatedAt,
                ScheduledAt = e.ScheduledAt,
                LocationAddress = e.LocationAddress,
                Tags = e.Tags ?? new List<Tag>(),
                ImageSource = string.IsNullOrEmpty(e.ImageBase64)
                    ? ImageSource.FromFile("image-placeholder.png")
                    : ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(e.ImageBase64))),
                IsFavorite = e.FavoritedBy != null && e.FavoritedBy.Contains(currentUser)
            }).ToList();

        _currentIndex = 0;
        UpdateDisplayedEvent();

        // Ensure refresh button visibility matches whether there are events
        var refreshBtn = this.FindByName<Button>("RefreshButton");
        if (refreshBtn != null)
        {
            refreshBtn.IsVisible = (_items == null || _items.Count == 0);
        }
    }

   private List<EventPreviewItem> _items = new List<EventPreviewItem>();
    private int _currentIndex = 0;
    private EventStore _store = new EventStore();

  private void UpdateDisplayedEvent()
    {
        if (_items == null || _items.Count == 0)
        {
            // Show empty label and hide preview controls
            var empty = this.FindByName<Label>("NoEventsLabel");
            if (empty != null) empty.IsVisible = true;

            // Show message in the description box
            var detailsLabel = this.FindByName<Label>("EventDetailsLabel");
            if (detailsLabel != null)
            {
                detailsLabel.IsVisible = true;
                detailsLabel.Text = "No new events";
            }

            var eventImage = this.FindByName<Image>("EventImage");
            if (eventImage != null) eventImage.IsVisible = false;

            var createdLabel = this.FindByName<Label>("CreatedAtLabel");
            if (createdLabel != null) createdLabel.IsVisible = false;

            var locLabelEmpty = this.FindByName<Label>("LocationLabel");
            if (locLabelEmpty != null) locLabelEmpty.IsVisible = false;

            var refresh = this.FindByName<Button>("RefreshButton");
            if (refresh != null) refresh.IsVisible = true;

            return;
        }

        // We have items to show. Hide empty label and show preview controls
        var noEventsLabel = this.FindByName<Label>("NoEventsLabel");
        if (noEventsLabel != null) noEventsLabel.IsVisible = false;

        var item = _items[_currentIndex];

        // Details label
        var detailLabel = this.FindByName<Label>("EventDetailsLabel");
        if (detailLabel != null)
        {
            detailLabel.IsVisible = true;
            detailLabel.Text = item.Details;
        }

        // Image
        var img = this.FindByName<Image>("EventImage");
        if (img != null)
        {
            img.IsVisible = true;
            img.Source = item.ImageSource;
        }

        // Created at label
        var createdAtLabel = this.FindByName<Label>("CreatedAtLabel");
        if (createdAtLabel != null)
        {
            createdAtLabel.Text = item.CreatedAt.ToString("g");
        }

        // Location label
        var locationLabel = this.FindByName<Label>("LocationLabel");
        if (locationLabel != null)
        {
            if (!string.IsNullOrEmpty(item.LocationAddress))
            {
                locationLabel.Text = item.LocationAddress;
                locationLabel.IsVisible = true;
            }
            else
            {
                locationLabel.IsVisible = false;
            }
        }

        // Tags label
        var tagsLabel = this.FindByName<Label>("TagsLabel");
        if (tagsLabel != null)
        {
            if (item.Tags != null && item.Tags.Count > 0)
            {
                var tagNames = string.Join(", ", item.Tags.Select(t => t.Name));
                tagsLabel.Text = tagNames;
                tagsLabel.IsVisible = true;
            }
            else
            {
                tagsLabel.IsVisible = false;
            }
        }
    }

    private void OnCycleEventClicked(object sender, EventArgs e)
    {
        if (_items == null || _items.Count == 0)
            return;

        _currentIndex = (_currentIndex + 1) % _items.Count;
        UpdateDisplayedEvent();
    }

    private void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (_items == null || _items.Count == 0)
            return;

        // Toggle favorite on the current event in the persistent store
      var stored = _store.LoadAll();
        var current = stored.FirstOrDefault(s => s.Details == _items[_currentIndex].Details && s.CreatedAt == _items[_currentIndex].CreatedAt);
        var user = Session.CurrentUserEmail;
        if (current != null)
        {
            if (current.FavoritedBy == null)
                current.FavoritedBy = new System.Collections.Generic.List<string>();

            if (current.FavoritedBy.Contains(user))
                current.FavoritedBy.Remove(user);
            else
                current.FavoritedBy.Add(user);

            _store.SaveAll(stored);

            // Update local cache: remove this item from the preview list if now favorited by this user
            if (current.FavoritedBy.Contains(user))
            {
                _items.RemoveAt(_currentIndex);
                if (_currentIndex >= _items.Count)
                    _currentIndex = 0;
            }
        }

        // If there are no more items, show empty state and avoid modulo by zero
        if (_items == null || _items.Count == 0)
        {
            UpdateDisplayedEvent();
            return;
        }

            // After favoriting, move to the next event
            _currentIndex = (_currentIndex + 1) % _items.Count;
            UpdateDisplayedEvent();
            }

            private async void OnTestNotificationClicked(object sender, EventArgs e)
            {
                try
                {
                    await NotificationTestHelper.TriggerTestNotificationAsync();
                    await DisplayAlert("✅ Test Notification", "Check your system notifications! A test event scheduled for 1 hour from now has been created.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("❌ Error", $"Failed to trigger notification: {ex.Message}", "OK");
                }
            }
        }

public class EventPreviewItem : INotifyPropertyChanged
{
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ImageSource ImageSource { get; set; }
    public bool IsFavorite { get; set; }
    public string LocationAddress { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public bool HasMatchingTags { get; set; } = false;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    private bool _selectionEnabled;
    public bool SelectionEnabled
    {
        get => _selectionEnabled;
        set
        {
            if (_selectionEnabled != value)
            {
                _selectionEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionEnabled)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
