using EventMatch.Services;
using EventMatch.Utils;
using System.Diagnostics;

namespace EventMatch;

public partial class DebugPage : ContentPage
{
    private readonly UserDatabase _database;
    private DataStatusUtil _statusUtil;

    public DebugPage()
    {
        InitializeComponent();

        // Get database from DI container
        _database = (UserDatabase)Application.Current.Handler.MauiContext.Services.GetService(typeof(UserDatabase));
        _statusUtil = new DataStatusUtil(_database);

        // Load data on page open
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await RefreshDataDisplayAsync();
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await RefreshDataDisplayAsync();
        });
    }

    private async Task RefreshDataDisplayAsync()
    {
        try
        {
            // Get summary
            var summary = await _statusUtil.GetSummaryAsync();
            DataLabel.Text = summary;

            // Get counts
            var users = await _database.GetAllUsersAsync();
            var groups = await _database.GetGroupsAsync();

            UserCountLabel.Text = users.Count.ToString();
            GroupCountLabel.Text = groups.Count.ToString();

            // Count messages
            int totalMessages = 0;
            foreach (var group in groups)
            {
                var messages = await _database.GetMessagesForGroupAsync(group.Id);
                totalMessages += messages.Count;
            }
            MessageCountLabel.Text = totalMessages.ToString();
        }
        catch (Exception ex)
        {
            DataLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshDataDisplayAsync();
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
        await _statusUtil.PrintAllDataAsync();
        await DisplayAlert("Success", "Data printed to Debug Output. Check Output window.", "OK");
    }

    private async void OnClearDataClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Warning", 
            "This will delete ALL local data (users, groups, profiles, friends). This cannot be undone!",
            "Yes, delete", 
            "Cancel");

        if (confirm)
        {
            try
            {
                await _database.ClearAllDataAsync();
                DataLabel.Text = "All local data has been deleted.";
                UserCountLabel.Text = "0";
                GroupCountLabel.Text = "0";
                MessageCountLabel.Text = "0";

                await DisplayAlert("Success", "All local data deleted.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete data: {ex.Message}", "OK");
            }
        }
    }

    private async void OnTestNotificationClicked(object sender, EventArgs e)
    {
        try
        {
            await NotificationTestHelper.TriggerTestNotificationAsync();
            await DisplayAlert("Test", "Check your system notifications for 'An event is coming up'!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnDeleteAllEventsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Warning",
            "This will delete ALL created events. This cannot be undone!",
            "Yes, delete",
            "Cancel");

        if (confirm)
        {
            try
            {
                var eventStore = new EventStore();
                eventStore.DeleteAll();
                await DisplayAlert("Success", "All events have been deleted.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete events: {ex.Message}", "OK");
            }
        }
    }
}
