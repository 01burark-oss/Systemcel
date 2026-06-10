using CashTracker.Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace Systemcel.Api.Hubs;

public sealed class MuhasebeciSohbetHub : Hub
{
    private readonly IMuhasebeciSohbetMerkeziService _service;

    public MuhasebeciSohbetHub(IMuhasebeciSohbetMerkeziService service)
    {
        _service = service;
    }

    public static string GroupName(int sohbetId) => $"sohbet:{sohbetId}";

    public async Task JoinConversation(int sohbetId)
    {
        await _service.GetMesajlarAsync(sohbetId, limit: 1);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sohbetId));
    }

    public Task LeaveConversation(int sohbetId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sohbetId));
    }

    public Task TypingStarted(int sohbetId)
    {
        return Clients.OthersInGroup(GroupName(sohbetId)).SendAsync("TypingStarted", new { sohbetId });
    }

    public Task TypingStopped(int sohbetId)
    {
        return Clients.OthersInGroup(GroupName(sohbetId)).SendAsync("TypingStopped", new { sohbetId });
    }
}
