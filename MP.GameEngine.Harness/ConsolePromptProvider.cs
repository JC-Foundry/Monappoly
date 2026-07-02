using MP.GameEngine.Abstractions;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Models.Prompts;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Prompts.PromptTypes.Responses;

namespace MP.GameEngine.Harness;

/// <summary>
/// An <see cref="IPromptProvider"/> that renders each engine prompt to the console and reads the
/// response back from the console — the console plays the role the SignalR client plays in the real
/// app. Because the engine awaits <see cref="RequestAsync{TResponse}"/> inline, this provider blocks on
/// <see cref="TextReader.ReadLine"/> and returns the typed response directly, so no
/// <see cref="TrySubmit"/> / pending-prompt round-trip is involved.
///
/// <para>Swapped onto a built engine via the <c>internal set</c> on <c>GameEngine.PromptProvider</c>.
/// Input/output default to <see cref="Console"/> for interactive driving in a real terminal; both can
/// be redirected (e.g. a scripted <see cref="TextReader"/> and a capturing <see cref="TextWriter"/>).</para>
/// </summary>
public sealed class ConsolePromptProvider(TextReader? input = null, TextWriter? output = null) : IPromptProvider
{
    private readonly TextReader _in = input ?? Console.In;
    private readonly TextWriter _out = output ?? Console.Out;

    public Task<TResponse> RequestAsync<TResponse>(Prompt<TResponse> prompt, CancellationToken ct = default)
        where TResponse : PromptResponse
    {
        RenderHeader(prompt);
        PromptResponse response = prompt switch
        {
            AcknowledgePrompt a => Acknowledge(a),
            DiceRollPrompt d => DiceRoll(d),
            CardOptionPrompt c => CardOption(c),
            TargetPlayerPrompt tp => TargetPlayer(tp),
            TargetPropertyPrompt tpr => TargetProperty(tpr),
            AcquirePropertyPrompt ap => AcquireProperty(ap),
            AuctionBidPrompt ab => AuctionBid(ab),
            ShortfallPrompt sf => Shortfall(sf),
            LeaveJailPrompt lj => LeaveJail(lj),
            InterruptibleWindowPrompt iw => Interruptible(iw),
            DealPrompt dl => Deal(dl),
            BuildDealPrompt bd => BuildDeal(bd),
            _ => prompt.DefaultResponse
                 ?? throw new NotSupportedException(
                     $"ConsolePromptProvider has no handler for prompt type {prompt.GetType().Name} and it carries no DefaultResponse.")
        };

        return Task.FromResult((TResponse)response);
    }

    /// <summary>Convenience acknowledge — routes through <see cref="RequestAsync{TResponse}"/> like the real provider.</summary>
    public Task<AcknowledgeResponse> Acknowledge(string playerId, string title, string body,
        TimeSpan? timeout = null, CardType? cardType = null, bool playingCard = false, CancellationToken ct = default)
    {
        var promptId = Guid.NewGuid().ToString();
        return RequestAsync(new AcknowledgePrompt
        {
            PromptId = promptId,
            PlayerId = playerId,
            Title = title,
            Body = body,
            Timeout = timeout,
            CardType = cardType,
            PlayingCard = playingCard,
            DefaultResponse = new AcknowledgeResponse { PromptId = promptId }
        }, ct);
    }

    /// <summary>
    /// Unused in the inline console model — responses are returned straight from <see cref="RequestAsync{TResponse}"/>,
    /// never submitted out of band. Present only to satisfy the interface.
    /// </summary>
    public bool TrySubmit(string submittingUserId, string concurrencyStamp, PromptResponse response) => false;

    // ── Per-prompt console interactions ──────────────────────────────────────

    private AcknowledgeResponse Acknowledge(AcknowledgePrompt p)
    {
        Read("Press Enter to acknowledge");
        return new AcknowledgeResponse { PromptId = p.PromptId };
    }

    private DiceRollResponse DiceRoll(DiceRollPrompt p)
    {
        while (true)
        {
            var parts = ReadTokens($"Enter {p.DiceCount} dice value(s) 1-6, space-separated");
            if (parts.Length == p.DiceCount && parts.All(t => ushort.TryParse(t, out var v) && v is >= 1 and <= 6))
            {
                var dice = parts.Select(ushort.Parse).ToArray();
                return new DiceRollResponse
                {
                    PromptId = p.PromptId,
                    Die1 = dice[0],
                    Die2 = p.DiceCount >= 2 ? dice[1] : null,
                    ThirdDie = p.DiceCount == 3 ? dice[2] : null
                };
            }
            _out.WriteLine($"  ! expected {p.DiceCount} value(s) in 1-6.");
        }
    }

    private CardOptionResponse CardOption(CardOptionPrompt p)
    {
        for (var i = 0; i < p.Options.Count; i++)
            _out.WriteLine($"  [{i}] {p.Options[i].Label}  (key={p.Options[i].Key})");
        var index = ReadIndex("Choose an option", p.Options.Count);
        return new CardOptionResponse { PromptId = p.PromptId, SelectedKey = p.Options[index].Key };
    }

    private TargetPlayerResponse TargetPlayer(TargetPlayerPrompt p)
    {
        for (var i = 0; i < p.EligiblePlayerIds.Count; i++)
            _out.WriteLine($"  [{i}] {p.EligiblePlayerIds[i]}");
        var picks = ReadIndexes($"Select {p.Count} player(s), space-separated indexes", p.EligiblePlayerIds.Count, p.Count);
        return new TargetPlayerResponse
        {
            PromptId = p.PromptId,
            SelectedPlayerIds = picks.Select(i => p.EligiblePlayerIds[i]).ToList()
        };
    }

    private TargetPropertyResponse TargetProperty(TargetPropertyPrompt p)
    {
        for (var i = 0; i < p.EligibleBoardIndexes.Count; i++)
            _out.WriteLine($"  [{i}] board index {p.EligibleBoardIndexes[i]}");
        var picks = ReadIndexes($"Select {p.Count} property/ies, space-separated indexes", p.EligibleBoardIndexes.Count, p.Count);
        return new TargetPropertyResponse
        {
            PromptId = p.PromptId,
            SelectedBoardIndexes = picks.Select(i => p.EligibleBoardIndexes[i]).ToList()
        };
    }

    private AcquirePropertyResponse AcquireProperty(AcquirePropertyPrompt p)
        => new() { PromptId = p.PromptId, Accept = ReadYesNo($"{p.Type} board index {p.BoardIndex} for £{p.Cost}?") };

    private AuctionBidResponse AuctionBid(AuctionBidPrompt p)
    {
        _out.WriteLine($"  high bid £{p.CurrentHighBid} (bidder {p.CurrentHighBidderId ?? "none"}), balance £{p.PlayerBalance}");
        _out.WriteLine("  [p] pass");
        for (var i = 0; i < p.AllowedIncrements.Count; i++)
            _out.WriteLine($"  [{i}] +£{p.AllowedIncrements[i]} → £{p.CurrentHighBid + p.AllowedIncrements[i]}");

        while (true)
        {
            var token = ReadTokens("Bid (index) or pass ([p])").FirstOrDefault() ?? "";
            if (token.Equals("p", StringComparison.OrdinalIgnoreCase))
                return new AuctionBidResponse { PromptId = p.PromptId, Action = AuctionBidAction.Pass };
            if (int.TryParse(token, out var i) && i >= 0 && i < p.AllowedIncrements.Count)
                return new AuctionBidResponse
                {
                    PromptId = p.PromptId,
                    Action = AuctionBidAction.Bid,
                    BidAmount = p.CurrentHighBid + p.AllowedIncrements[i]
                };
            _out.WriteLine("  ! enter a listed index or 'p'.");
        }
    }

    private ShortfallResponse Shortfall(ShortfallPrompt p)
    {
        _out.WriteLine($"  owe £{p.AmountOwed} (cost £{p.Cost}, balance £{p.PlayerBalance}, creditor {p.OwedToPlayerId ?? "bank"})");
        var action = ReadEnum<ShortfallAction>("Choose");
        return new ShortfallResponse { PromptId = p.PromptId, Action = action };
    }

    private LeaveJailResponse LeaveJail(LeaveJailPrompt p)
    {
        _out.WriteLine($"  fee £{p.Cost}, holds release card: {p.HasCard}");
        var action = ReadEnum<LeaveJailAction>("Choose");
        return new LeaveJailResponse { PromptId = p.PromptId, Action = action };
    }

    private InterruptibleWindowResponse Interruptible(InterruptibleWindowPrompt p)
    {
        _out.WriteLine("  [c] continue (no interrupt)");
        for (var i = 0; i < p.EligiblePlays.Count; i++)
            _out.WriteLine($"  [{i}] {p.EligiblePlays[i].PlayerId} plays {p.EligiblePlays[i].CardName} ({p.EligiblePlays[i].CardId})");

        while (true)
        {
            var token = ReadTokens("Continue ([c]) or play (index)").FirstOrDefault() ?? "";
            if (token.Equals("c", StringComparison.OrdinalIgnoreCase))
                return new InterruptibleWindowResponse { PromptId = p.PromptId, Action = InterruptAction.Continue };
            if (int.TryParse(token, out var i) && i >= 0 && i < p.EligiblePlays.Count)
                return new InterruptibleWindowResponse
                {
                    PromptId = p.PromptId,
                    Action = InterruptAction.PlayCard,
                    PlayedByPlayerId = p.EligiblePlays[i].PlayerId,
                    PlayedCardId = p.EligiblePlays[i].CardId
                };
            _out.WriteLine("  ! enter a listed index or 'c'.");
        }
    }

    private DealResponse Deal(DealPrompt p)
        => new() { PromptId = p.PromptId, Accept = ReadYesNo($"Accept deal from {p.ProposerId}?") };

    private BuildDealResponse BuildDeal(BuildDealPrompt p)
    {
        // Constructing a full DealContents at the console is out of scope for now; a scenario that needs a
        // real settling deal should redirect input or supply a scripted provider. Default: cancel.
        _out.WriteLine("  (BuildDeal not interactively supported — cancelling)");
        return new BuildDealResponse { PromptId = p.PromptId, Cancelled = true };
    }

    // ── Console I/O helpers ───────────────────────────────────────────────────

    private void RenderHeader(Prompt prompt)
    {
        _out.WriteLine();
        _out.WriteLine($"── {prompt.GetType().Name} → {prompt.PlayerId} ──");
        if (!string.IsNullOrWhiteSpace(prompt.Title)) _out.WriteLine(prompt.Title);
        if (!string.IsNullOrWhiteSpace(prompt.Body)) _out.WriteLine(prompt.Body);
    }

    /// <summary>
    /// Reads a line, echoing <paramref name="label"/> as the cue. A <c>null</c> return means the input
    /// stream ended (EOF) — thrown so a finished script terminates cleanly rather than spinning the parse
    /// loops forever. An interactive terminal never hits this unless the session is closed.
    /// </summary>
    private string Read(string label)
    {
        _out.Write($"{label}: ");
        return _in.ReadLine()
               ?? throw new EndOfStreamException("Prompt input stream ended (EOF) — no more responses available.");
    }

    private string[] ReadTokens(string label)
        => Read(label).Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private bool ReadYesNo(string label)
    {
        while (true)
        {
            var t = Read($"{label} [y/n]").Trim();
            if (t.StartsWith('y') || t.StartsWith('Y')) return true;
            if (t.StartsWith('n') || t.StartsWith('N')) return false;
            _out.WriteLine("  ! enter y or n.");
        }
    }

    private int ReadIndex(string label, int count)
    {
        while (true)
        {
            if (int.TryParse(Read($"{label} [0-{count - 1}]").Trim(), out var i) && i >= 0 && i < count)
                return i;
            _out.WriteLine($"  ! enter an index in 0-{count - 1}.");
        }
    }

    private List<int> ReadIndexes(string label, int count, int required)
    {
        while (true)
        {
            var tokens = ReadTokens(label);
            var indexes = new List<int>();
            var ok = tokens.Length == required;
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out var i) && i >= 0 && i < count && !indexes.Contains(i)) indexes.Add(i);
                else ok = false;
            }
            if (ok && indexes.Count == required) return indexes;
            _out.WriteLine($"  ! enter {required} distinct index/es in 0-{count - 1}.");
        }
    }

    private TEnum ReadEnum<TEnum>(string label) where TEnum : struct, Enum
    {
        var names = Enum.GetNames<TEnum>();
        for (var i = 0; i < names.Length; i++)
            _out.WriteLine($"  [{i}] {names[i]}");
        var index = ReadIndex(label, names.Length);
        return Enum.Parse<TEnum>(names[index]);
    }
}