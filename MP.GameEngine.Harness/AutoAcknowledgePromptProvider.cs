using MP.GameEngine.Abstractions;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Models.Prompts;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Prompts.PromptTypes.Responses;

namespace MP.GameEngine.Harness;

/// <summary>
/// A prompt provider used only during scenario <b>setup</b>, while cards are seeded into hands through
/// the real <c>CardService.DrawCard</c>. Drawing a keep-until-needed card opens a single "card picked up"
/// <see cref="AcknowledgePrompt"/> and then adds the card to the hand — this provider auto-answers that
/// acknowledge so seeding runs without console input. Any other prompt during setup is unexpected (a
/// held-card draw should only acknowledge), so it throws rather than silently guessing.
/// </summary>
public sealed class AutoAcknowledgePromptProvider : IPromptProvider
{
    public Task<TResponse> RequestAsync<TResponse>(Prompt<TResponse> prompt, CancellationToken ct = default)
        where TResponse : PromptResponse
    {
        if (prompt is AcknowledgePrompt)
            return Task.FromResult((TResponse)(PromptResponse)new AcknowledgeResponse { PromptId = prompt.PromptId });

        throw new InvalidOperationException(
            $"Unexpected {prompt.GetType().Name} during card seeding — only keep-until-needed draws (acknowledge only) are expected.");
    }

    public Task<AcknowledgeResponse> Acknowledge(string playerId, string title, string body,
        TimeSpan? timeout = null, CardType? cardType = null, bool playingCard = false, CancellationToken ct = default)
        => Task.FromResult(new AcknowledgeResponse { PromptId = Guid.NewGuid().ToString() });

    public bool TrySubmit(string submittingUserId, string concurrencyStamp, PromptResponse response) => false;
}