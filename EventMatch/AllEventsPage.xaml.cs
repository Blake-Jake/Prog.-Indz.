using EventMatch.Services;
using System.Linq;
using System.IO;
using EventMatch.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.DependencyInjection;
using SQLite;

namespace EventMatch;

public partial class AllEventsPage : ContentPage
{
    // No manual fallback fields here; rely on XAML-generated members.
    public AllEventsPage()
    {
        InitializeComponent();
    }

    private HashSet<EventPreviewItem> _selected = new HashSet<EventPreviewItem>();
    private bool _selectionMode = false;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var store = new EventStore();
        var events = store.LoadAll();

        // Get current user's preferred tags
        var currentUser = Session.CurrentUserEmail;
        var userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<UserDatabase>();
        var userProfile = userDb != null ? await userDb.GetProfileByEmailAsync(currentUser) : null;
        var userPreferredTags = userProfile?.GetPreferredTags() ?? new List<string>();

        // Separate events into two groups
        List<Event> matchingEvents = new(); // Events with matching tags
        List<Event> otherEvents = new();    // Events without matching tags

        foreach (var evt in events)
        {
            var eventTags = evt.Tags?.Select(t => t.Name?.ToLower().Trim() ?? "").ToList() ?? new List<string>();

            // Check if event has any matching tags
            bool hasMatchingTag = userPreferredTags.Count > 0 && 
                                  userPreferredTags.Any(userTag => eventTags.Contains(userTag.ToLower()));

            if (evt.FavoritedBy != null && evt.FavoritedBy.Contains(currentUser))
            {
                if (hasMatchingTag)
                    matchingEvents.Add(evt);
                else
                    otherEvents.Add(evt);
            }
        }

        // Combine: matching first, then other events
        var orderedEvents = matchingEvents.Concat(otherEvents).ToList();

        var items = orderedEvents.Select(e => new EventPreviewItem
        {
            Details = e.Details,
            CreatedAt = e.CreatedAt,
            ScheduledAt = e.ScheduledAt,
            ImageSource = string.IsNullOrEmpty(e.ImageBase64)
                ? ImageSource.FromFile("image-placeholder.png")
                : ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(e.ImageBase64))),
            HasMatchingTags = userPreferredTags.Count > 0 && 
                             e.Tags?.Any(t => userPreferredTags.Contains(t.Name?.ToLower().Trim() ?? "")) == true
        }).ToList();

        AllEventsCollectionView.ItemsSource = new ObservableCollection<EventPreviewItem>(items);

        // Ensure selection UI hidden by default
        var selectBtn = this.FindByName<Button>("SelectAllButton");
        if (selectBtn != null) selectBtn.IsVisible = false;

        // Ensure items have SelectionEnabled=false by default
        if (AllEventsCollectionView.ItemsSource is IEnumerable<EventPreviewItem> list)
        {
            foreach (var it in list)
                it.SelectionEnabled = false;
        }

        // Wire up item tapped to open single preview
        AllEventsCollectionView.SelectionMode = SelectionMode.Single;
        AllEventsCollectionView.SelectionChanged -= OnSelectionChanged;
        AllEventsCollectionView.SelectionChanged += (s, e) =>
        {
            // If we're in bulk-selection mode for 'not interested', don't open the preview on item tap
            if (_selectionMode)
            {
                // clear any selection added by tapping
                AllEventsCollectionView.SelectedItem = null;
                return;
            }

            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is EventPreviewItem it)
            {
                // Open EventPreview page for this item without favorite/cycle controls
                var page = new EventPreview(it, hideControls: true);
                // Clear selection to avoid staying highlighted
                AllEventsCollectionView.SelectedItem = null;
                Navigation.PushAsync(page);
            }
        };
    }

    private void OnUninterestedClicked(object sender, EventArgs e)
    {
        // Toggle selection mode
        _selectionMode = !_selectionMode;

        // If entering selection mode, enable selection on the CollectionView
        if (_selectionMode)
        {
            // Do NOT enable CollectionView selection mode to avoid duplicate built-in selection visuals.
            var selBtn = this.FindByName<Button>("SelectAllButton");
            if (selBtn != null) selBtn.IsVisible = true;
            var uiBtn = this.FindByName<Button>("UninterestedButton");
            if (uiBtn != null) uiBtn.Text = "Remove selected";
            _selected.Clear();

            // Enable selection checkboxes visually
            if (AllEventsCollectionView.ItemsSource is IEnumerable<EventPreviewItem> list)
            {
                foreach (var it in list)
                {
                    it.SelectionEnabled = true;
                    it.IsSelected = false;
                }
            }
        }
        else
        {
            // Remove selected items from user's favorites
            if (_selected.Count > 0)
            {
                var store = new EventStore();
                var all = store.LoadAll();
                var user = Session.CurrentUserEmail;
                foreach (var it in _selected)
                {
                    var match = all.FirstOrDefault(a => a.Details == it.Details && a.CreatedAt == it.CreatedAt);
                    if (match != null && match.FavoritedBy != null && match.FavoritedBy.Contains(user))
                    {
                        match.FavoritedBy.Remove(user);
                    }
                }
                store.SaveAll(all);
            }

            // Exit selection mode and refresh
            var selBtn2 = this.FindByName<Button>("SelectAllButton");
            if (selBtn2 != null) selBtn2.IsVisible = false;
            var uiBtn2 = this.FindByName<Button>("UninterestedButton");
            if (uiBtn2 != null) uiBtn2.Text = "I'm not interested anymore";
            // Disable selection checkboxes visually
            var list2 = AllEventsCollectionView.ItemsSource as IEnumerable<EventPreviewItem>;
            if (list2 != null)
            {
                foreach (var it in list2)
                    it.SelectionEnabled = false;
            }
            OnAppearing();
        }
    }

    private void OnSelectAllClicked(object sender, EventArgs e)
    {
        // Select or deselect all visible items and update checkboxes
        var cv = this.FindByName<CollectionView>("AllEventsCollectionView");
        if (cv != null && cv.ItemsSource is IEnumerable<EventPreviewItem> items)
        {
            var list = items.ToList();

            // If every item is already selected, deselect all. Otherwise select all.
            var allSelected = list.Count > 0 && list.All(i => i.IsSelected);

            _selected.Clear();
            foreach (var it in list)
            {
                it.IsSelected = !allSelected; // set true to select, false to deselect
                if (it.IsSelected)
                    _selected.Add(it);
            }
        }
    }

    private async void OnRemoveClicked(object sender, EventArgs e)
    {
        // Identify which item was clicked by using Button.BindingContext
        if (sender is Button btn && btn.BindingContext is EventPreviewItem item)
        {
            var store = new EventStore();
            var all = store.LoadAll();
            var match = all.FirstOrDefault(a => a.Details == item.Details && a.CreatedAt == item.CreatedAt);
            var user = Session.CurrentUserEmail;
            if (match != null && match.FavoritedBy != null && match.FavoritedBy.Contains(user))
            {
                match.FavoritedBy.Remove(user);
                store.SaveAll(all);

                // Refresh the list displayed
                OnAppearing();
            }
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_selectionMode) return;

        // Update selected items set
        _selected.Clear();
        foreach (var obj in e.CurrentSelection)
        {
            if (obj is EventPreviewItem it)
                _selected.Add(it);
        }
    }

    private void OnItemCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox cb && cb.BindingContext is EventPreviewItem item)
        {
            if (e.Value)
                _selected.Add(item);
            else
                _selected.Remove(item);
        }
    }
}
