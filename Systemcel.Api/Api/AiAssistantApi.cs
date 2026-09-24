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
                IIsletmeService isletmeService,
                ICurrentUserContext currentUser,
                ISecretProtector protector,
                CancellationToken ct) =>
            {
                var businessId = await isletmeService.GetActiveIdAsync();
                var userId = currentUser.GetCurrentUser()?.ProviderUserId ?? string.Empty;
                if (AiAssistantConversationToken.TryRead(
                    protector, request.ContinuationToken, businessId, userId, out var previous))
                {
                    request.ContextQuestion = previous.Question;
                    request.ContextAnswer = previous.Answer;
                }

                var response = await aiAssistant.ChatAsync(request, ct);
                if (!string.IsNullOrWhiteSpace(request.Mesaj) &&
                    !string.IsNullOrWhiteSpace(response.SafeContextQuestion) &&
                    !string.IsNullOrWhiteSpace(response.SafeContextAnswer))
                {
                    response.ContinuationToken = AiAssistantConversationToken.Create(
                        protector, businessId, userId,
                        response.SafeContextQuestion, response.SafeContextAnswer);
                }
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
