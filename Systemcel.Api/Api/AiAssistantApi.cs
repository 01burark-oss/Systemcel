using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Systemcel.Api.Api;

internal static class AiAssistantApi
{
    public static void MapAiAssistantApi(this WebApplication app)
    {
        var status = app.MapGet(
            "/api/ai/durum",
            async (IAiAssistantService aiAssistant, CancellationToken ct) =>
            {
                var response = await aiAssistant.GetStatusAsync(ct);
                return Results.Ok(response);
            });

        var suggestions = app.MapGet(
            "/api/ai/oneriler",
            async (IAiAssistantService aiAssistant, CancellationToken ct) =>
            {
                var response = await aiAssistant.GetSuggestionsAsync(ct);
                return Results.Ok(response);
            });

        var chat = app.MapPost(
            "/api/ai/sohbet",
            async (
                AiAssistantChatRequest request,
                IAiAssistantService aiAssistant,
                CancellationToken ct) =>
            {
                var response = await aiAssistant.ChatAsync(request, ct);
                return Results.Ok(response);
            });

        var clerkOptions = app.Services.GetRequiredService<ClerkAuthenticationOptions>();
        if (clerkOptions.Enabled)
        {
            status.RequireAuthorization();
            suggestions.RequireAuthorization();
            chat.RequireAuthorization();
        }
    }
}
