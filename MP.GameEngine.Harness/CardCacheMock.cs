using System.Text.Json;
using JC.Core.Extensions;
using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Helpers.Cards;
using MP.GameEngine.Models.Imports;
using MP.GameEngine.Models.Snapshot.Cards;

namespace MP.GameEngine.Harness;

public class CardCacheMock : ICardCacheService
{
    private const string FilePath = @"..\config\cards";
    private readonly string[] _cardPaths = [Chance, ComChest, PercentChance, PercentComChest, Third, Double, Triple, Tax, Go, JustVisiting, FreeParking, GoToJail];
    private const string Chance = "chance";
    private const string ComChest = "comChest";
    private const string PercentChance = "percentChance";
    private const string PercentComChest = "percentComChest";
    private const string Third = "third";
    private const string Double = "double";
    private const string Triple = "triple";
    private const string Tax = "tax";
    private const string Go = "go";
    private const string JustVisiting = "justVisiting";
    private const string FreeParking = "freeParking";
    private const string GoToJail = "goToJail";

    private async Task<List<CardModel>> LoadCards()
    {
        var cards = new List<CardModel>();

        foreach (var cp in _cardPaths)
        {
            var path = Path.Combine(FilePath, $"{cp}.json");
            var text = await File.ReadAllTextAsync(path);

            var import = JsonSerializer.Deserialize<List<CardJsonImport>>(text);
            if(import == null || import.Count == 0)
                continue;

            var index = 0;
            foreach (var model in import.Select(i => new CardModel
                     {
                         CardId = $"{cp}_{index}",
                         UniqueText = $"{i.RawText} {CardDisplayHelper.UniqueTagOpen}{index++}{CardDisplayHelper.UniqueTagClose}",
                         CardText = i.RawText,
                         CardType = EnumExtensions.TryParse<CardType>(i.CardType),
                         Groups = i.Groups.AsReadOnly(),
                         Conditions = i.Conditions.AsReadOnly(),
                         ConditionType = EnumExtensions.TryParse<CardConditionType>(i.ConditionType),
                         SuppressDefault = i.SuppressDefault
                     }))
            {
                var groupIndex = 0;
                foreach (var group in model.Groups)
                {
                    group.GroupId = groupIndex.ToString();
                    groupIndex++;

                    var actionIndex = 0;
                    foreach (var action in group.Actions)
                    {
                        action.ActionId = actionIndex.ToString();
                        actionIndex++;
                    }
                }

                var conditionIndex = 0;
                foreach (var condition in model.Conditions)
                {
                    condition.ConditionId = conditionIndex.ToString();
                    conditionIndex++;
                }

                cards.Add(model);
                index++;
            }
        }

        return cards;
    }


    public Task<List<CardModel>> GetCards()
        => LoadCards();

    public async Task<CardModel?> GetCard(string cardId)
        => (await LoadCards()).FirstOrDefault(c => c.CardId == cardId);
}