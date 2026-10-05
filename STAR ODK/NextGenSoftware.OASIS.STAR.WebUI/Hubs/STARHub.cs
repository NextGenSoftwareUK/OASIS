using Microsoft.AspNetCore.SignalR;

namespace NextGenSoftware.OASIS.STAR.WebUI.Hubs
{
    public class STARHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendSTARStatus(string status)
        {
            await Clients.All.SendAsync("STARStatusUpdate", status);
        }

        public async Task SendProgressUpdate(string operation, int progress, string message)
        {
            await Clients.All.SendAsync("ProgressUpdate", new { Operation = operation, Progress = progress, Message = message });
        }

        public async Task SendError(string error)
        {
            await Clients.All.SendAsync("Error", error);
        }

        public async Task SendSuccess(string message)
        {
            await Clients.All.SendAsync("Success", message);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
