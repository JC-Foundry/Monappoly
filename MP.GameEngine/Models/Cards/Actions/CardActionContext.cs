namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// The dynamic context a fired card carries into action resolution — the figures only known at the
/// moment the card runs, which an action can read instead of a fixed value. Today it carries the
/// <see cref="TriggerAmount"/> a held card's trigger supplied (the tax just assessed, the Free
/// Parking take, …), read by a <c>MoneyAction</c> whose <c>AmountSource</c> is
/// <see cref="Enums.Cards.AmountSource.TriggerAmount"/>.
///
/// It is an <b>optional</b> parameter threaded through <c>CardService</c> → the action handlers: a
/// resolve-on-draw card, a hand-played card with no trigger amount, and every action that doesn't
/// read the context all pass / receive <c>null</c>. The trigger layer (<c>card-triggers.md</c>) is
/// the only producer of a populated context; until it lands this type has no callers that fill it.
/// </summary>
public sealed class CardActionContext
{
    /// <summary>The amount the firing trigger handed the card (e.g. the £200 tax a "double tax" card triples).</summary>
    public long TriggerAmount { get; set; }
}