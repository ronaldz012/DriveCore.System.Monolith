using Inventory.Infrastructure.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Inventory.Infrastructure;

// Infrastructure/Notifications/SignalRStockNotifier.cs
public class InventorySignalRStockNotifier(IHubContext<NotificationHub> hub)
{
    public async Task NotifyLowStock(int productId, string productName, int currentStock, int minStock)
    {
        await hub.Clients.Group("inventory-managers").SendAsync("ReceiveNotification", new
        {
            type = "LOW_STOCK",
            productId,
            productName,
            currentStock,
            message = $"⚠️ Stock bajo: {productName} ({currentStock} unidades)"
        });
    }
    public async Task NotifyProductCreated(string productName)
    {
        await hub.Clients.Group("inventory-managers").SendAsync("ReceiveNotification", new
        {
            type = "NEW_PRODUCT_CREATED",
            name =  productName
        });
    }
}