namespace MP.GameEngine.Models.Snapshot;

/// <summary>
/// The pending payout modification for the triple currently being resolved (cards-dev-changes.md §2.13,
/// card-triggers.md). Triple-bonus cards — the drawn payout modifiers (doubled / ×die / redirect / no-bonus)
/// and the held "cancel a player's triple bonus" card — <b>record</b> their effect here instead of applying
/// it, so <see cref="Services.SubSystems.PlayerService.ResolveTripleBonus"/> can credit the bonus <b>exactly
/// once</b> (accumulator +£500 once, payout once) after the <c>OnTripleBonus</c> window. <see cref="Cancelled"/>
/// (a factor-0 suppress/cancel) dominates any scaling <see cref="Factor"/>.
///
/// <para>Transient: set as a triple resolves and cleared the moment it is applied, so it is never carried
/// across a turn boundary or into a snapshot.</para>
/// </summary>
public class TripleBonusModifier
{
    /// <summary>Payout multiplier — 1 = full, 2 = doubled, a die value for ×die. Ignored when <see cref="Cancelled"/>.</summary>
    public ushort Factor { get; set; } = 1;

    /// <summary>Who receives the payout — null = the roller; a player id when redirected (e.g. to the lowest roller).</summary>
    public string? RecipientId { get; set; }

    /// <summary>A cancel/suppress (factor 0) was recorded — dominates any scaling factor, so the payout is nil.</summary>
    public bool Cancelled { get; set; }

    public TripleBonusModifier()
    {
    }

    public TripleBonusModifier(TripleBonusModifier model)
    {
        Factor = model.Factor;
        RecipientId = model.RecipientId;
        Cancelled = model.Cancelled;
    }
}
