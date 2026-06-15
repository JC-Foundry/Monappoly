using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A loan change driven by a card — wiping (forgiving) or repaying every outstanding loan, over
/// the target player(s). Resolved against <c>PlayerModel.Loans</c> (and <c>TransactionService</c>
/// for the repay leg). See <c>design-docs/cards-design.md</c> §3 (Loans).
/// </summary>
public sealed class LoansAction : CardAction
{
    public LoanCardKind Kind { get; set; }

    /// <summary>Who it acts on (e.g. <see cref="PlayerTarget.Everyone"/> for "all players").</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;
}