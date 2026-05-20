using EventMatch.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EventMatch;

public partial class GroupsPage : ContentPage
{
    public ObservableCollection<Group> Groups { get; } = new();
    private readonly Services.HybridGroupService _groupService;
    private readonly Services.UserDatabase _userDb;
    private readonly Services.CloudGroupService _cloudService;

    public GroupsPage()
    {
        InitializeComponent();
        _groupService = Application.Current?.Handler?.MauiContext?.Services.GetService<Services.HybridGroupService>()!;
        _userDb = Application.Current?.Handler?.MauiContext?.Services.GetService<Services.UserDatabase>()!;
            _cloudService = Application.Current?.Handler?.MauiContext?.Services.GetService<Services.CloudGroupService>()!;

        BindingContext = this;
    }

    private async void OnCreateGroupClicked(object sender, EventArgs e)
    {
        var name = NewGroupName.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Error", "Group name is required", "OK");
            return;
        }

        var desc = NewGroupDescription.Text?.Trim() ?? string.Empty;
        var g = new Group { Name = name, Description = desc, MemberCount = 1, OwnerEmail = Models.Session.CurrentUserEmail };

        // Ensure hybrid service initialized so we attempt cloud save first
        try
        {
            await _groupService.InitializeAsync();
        }
        catch { }

        // Persist using hybrid service (will try cloud first then local)
        var created = await _groupService.CreateGroupAsync(g);
        if (created != null)
        {
            await LoadGroups();
            NewGroupName.Text = string.Empty;
            NewGroupDescription.Text = string.Empty;
            // Notify user whether group was created on cloud or only locally
            if (created.CloudId != 0)
                await DisplayAlert("Success", "Group created on cloud and synced!", "OK");
            else
                await DisplayAlert("Success", "Group created locally (offline). It will sync when cloud is available.", "OK");
        }
        else
        {
            await DisplayAlert("Error", "Failed to create group", "OK");
        }
    }

    private async void OnJoinClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Group g)
        {
            // Add membership using cloud sync
            var success = await _groupService.AddUserToGroupAsync(g.Id, Models.Session.CurrentUserEmail);
            if (success)
            {
                await LoadGroups();
            }
        }
    }

    private async void OnEditGroupClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Group g)
        {
            // navigate to EditGroupPage
            await Shell.Current.GoToAsync($"EditGroupPage?groupId={g.Id}");
        }
    }

    private async void OnDeleteGroupClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Group g)
        {
            var ok = await DisplayAlert("Delete", $"Delete group '{g.Name}'?", "Yes", "No");
            if (!ok) return;

            var success = await _groupService.DeleteGroupAsync(g.Id);
            if (success)
            {
                await LoadGroups();
            }
        }
    }

    private async void OnOpenChatClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Group g)
        {
            // navigate to chat page; register route 'GroupChatPage' in MauiProgram if needed
            await Shell.Current.GoToAsync($"GroupChatPage?groupId={g.Id}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // Ensure cloud connectivity is checked and local->cloud sync runs so Android will see groups
            await _groupService.InitializeAsync();
            await _groupService.SyncLocalToCloudAsync();
        }
        catch { }

        await LoadGroups();
    }

    private async Task LoadGroups()
    {
        // Prefer fetching all public groups from cloud so users can see groups created by others.
        List<Group>? list = null;

        try
        {
            var cloudAll = await _cloudService.GetAllGroupsAsync();
            if (cloudAll != null && cloudAll.Count > 0)
            {
                list = cloudAll;
            }
        }
        catch { }

        if (list == null)
        {
            // Fallback: get user's groups via hybrid service (cloud/local mix)
            list = await _groupService.GetUserGroupsAsync(Models.Session.CurrentUserEmail);
        }
        Device.BeginInvokeOnMainThread(async () =>
        {
            Groups.Clear();
            // Merge and deduplicate by Id
            var map = new Dictionary<int, Group>();
            foreach (var g in list)
            {
                // normalize emails and compare case-insensitive
                var owner = (g.OwnerEmail ?? string.Empty).Trim();
                var current = (Models.Session.CurrentUserEmail ?? string.Empty).Trim();
                g.IsOwner = !string.IsNullOrEmpty(owner) && string.Equals(owner, current, StringComparison.OrdinalIgnoreCase);
                g.IsMember = await _userDb.IsUserMemberOfGroupAsync(g.Id, Models.Session.CurrentUserEmail);
                if (!map.ContainsKey(g.Id)) map[g.Id] = g;
            }

            foreach (var kv in map)
                Groups.Add(kv.Value);
        });
    }
}

