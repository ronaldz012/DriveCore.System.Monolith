using Microsoft.AspNetCore.SignalR;

namespace Inventory.Infrastructure.Notifications;

public class NotificationHub : Hub
{
    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }
}