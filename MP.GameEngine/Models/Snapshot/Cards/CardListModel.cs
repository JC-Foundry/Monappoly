namespace MP.GameEngine.Models.Snapshot.Cards;

public class CardListModel
{
    /// <summary>
    /// Chance card list
    /// </summary>
    public List<CardModel> ChanceCards { get; set; } = [];

    /// <summary>
    /// Community chest card list
    /// </summary>
    public List<CardModel> CommunityChestCards { get; set; } = [];

    /// <summary>
    /// Percentage chance list
    /// </summary>
    public List<CardModel> PercentChanceCards { get; set; } = [];

    /// <summary>
    /// Percentage community chest list
    /// </summary>
    public List<CardModel> PercentCommunityChestCards { get; set; } = [];

    /// <summary>
    /// Third cards list
    /// </summary>
    public List<CardModel> ThirdCards { get; set; } = [];

    /// <summary>
    /// Double cards list
    /// </summary>
    public List<CardModel> DoubleCards { get; set; } = [];

    /// <summary>
    /// Triple cards list
    /// </summary>
    public List<CardModel> TripleCards { get; set; } = [];

    /// <summary>
    /// Tax cards list
    /// </summary>
    public List<CardModel> TaxCards { get; set; } = [];

    /// <summary>
    /// Go cards list
    /// </summary>
    public List<CardModel> GoCards { get; set; } = [];

    /// <summary>
    /// Just visiting cards list
    /// </summary>
    public List<CardModel> JustVisitingCards { get; set; } = [];

    /// <summary>
    /// Free parking cards list
    /// </summary>
    public List<CardModel> FreeParkingCards { get; set; } = [];

    /// <summary>
    /// Go to jail cards list
    /// </summary>
    public List<CardModel> GoToJailCards { get; set; } = [];

    public CardListModel()
    {
    }

    public CardListModel(CardListModel model)
    {
        ChanceCards = model.ChanceCards.Select(c => new CardModel(c)).ToList();
        CommunityChestCards = model.CommunityChestCards.Select(c => new CardModel(c)).ToList();
        
        PercentChanceCards = model.PercentChanceCards.Select(c => new CardModel(c)).ToList();
        PercentCommunityChestCards = model.PercentCommunityChestCards.Select(c => new CardModel(c)).ToList();
        ThirdCards = model.ThirdCards.Select(c => new CardModel(c)).ToList();
        
        DoubleCards = model.DoubleCards.Select(c => new CardModel(c)).ToList();
        TripleCards = model.TripleCards.Select(c => new CardModel(c)).ToList();
        
        TaxCards = model.TaxCards.Select(c => new CardModel(c)).ToList();
        
        GoCards = model.GoCards.Select(c => new CardModel(c)).ToList();
        JustVisitingCards = model.JustVisitingCards.Select(c => new CardModel(c)).ToList();
        FreeParkingCards = model.FreeParkingCards.Select(c => new CardModel(c)).ToList();
        GoToJailCards = model.GoToJailCards.Select(c => new CardModel(c)).ToList();
    }
}