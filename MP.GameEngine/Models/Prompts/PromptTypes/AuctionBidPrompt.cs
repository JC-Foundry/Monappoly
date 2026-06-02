using MP.GameEngine.Models.Prompts.PromptTypes.Responses;

namespace MP.GameEngine.Models.Prompts.PromptTypes;

/// <summary>
/// Asks <see cref="Prompt.PlayerId"/> to bid on the property being auctioned,
/// or to pass. The engine runs the auction loop and opens one of these
/// prompts per bidder per round, in clockwise order
/// (<c>game-rules.md</c> Default rule 6 — every player may bid, including
/// the player who declined the purchase and any players currently in jail).
/// </summary>
/// <remarks>
/// Per <c>game-rules.md</c> Default rule 7, bidding must be paid from money
/// the player genuinely has — they cannot mortgage, sell buildings, or
/// trade to fund a bid. The validator enforces this by capping
/// <see cref="AuctionBidResponse.BidAmount"/> at <see cref="PlayerBalance"/>.
/// </remarks>
public sealed class AuctionBidPrompt : Prompt<AuctionBidResponse>
{
    /// <summary>
    /// The property being auctioned, by
    /// <see cref="Snapshot.PropertyModel.BoardIndex"/>.
    /// </summary>
    public ushort BoardIndex { get; init; }

    /// <summary>
    /// The highest bid so far. At the start of the auction this is the minimum
    /// bid — the property's reserve price (50% of its purchase cost, rounded to
    /// the game's grid; <see cref="Helpers.MoneyHelper.MinAuctionBid"/>), not
    /// <c>0</c>. A raise must strictly exceed it and is built from
    /// <see cref="AllowedIncrements"/>.
    /// </summary>
    public uint CurrentHighBid { get; init; }

    /// <summary>
    /// The player who currently holds the high bid, if any. <c>null</c>
    /// before the first raise lands (the auction sits at the minimum bid).
    /// Useful for the frontend ("Player X is winning at £350") but not
    /// consulted by validation.
    /// </summary>
    public string? CurrentHighBidderId { get; init; }

    /// <summary>The bidder's available cash. Bids above this are rejected by the validator.</summary>
    public uint PlayerBalance { get; init; }

    /// <summary>
    /// The raise amounts the bidder may add to <see cref="CurrentHighBid"/>,
    /// derived from the game's rounding rule via
    /// <see cref="Helpers.MoneyHelper.AuctionIncrements"/> — e.g. <c>[50, 100]</c>
    /// under "round to 50", <c>[1, 5, 10, 20, 50, 100]</c> with no rounding. The
    /// client renders one button per increment (each gated against
    /// <see cref="PlayerBalance"/>) and submits <c>CurrentHighBid + increment</c>
    /// as the <see cref="AuctionBidResponse.BidAmount"/>. Computed server-side so
    /// the rounding→increment mapping stays a single source of truth.
    /// </summary>
    public IReadOnlyList<ushort> AllowedIncrements { get; init; } = [];

    public override PromptTarget Target => PromptTarget.SinglePlayer(PlayerId);
}