namespace UltimateMonopoly.Models.ImportModels;

public class CardJsonImport
{
    public string CardText { get; set; }
    
    //TODO - Decide on whether card imports should be dependant on its actions, or if all cards should be flat JSON import; with nullable fields
}