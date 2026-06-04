using System.Text.Json.Serialization;

namespace MP.GameEngine.Models.Snapshot;

public class LoanModel
{
    public uint Amount { get; set; }
    public DateTime DateTaken { get; set; }
    
    public uint PaidBack { get; set; }
    
    [JsonIgnore]
    public bool IsOutstanding => PaidBack < Amount;

    public LoanModel()
    {
    }
    
    public LoanModel(uint amount)
    {
        Amount = amount;
        DateTaken = DateTime.UtcNow;
    }

    public LoanModel(LoanModel model)
    {
        Amount = model.Amount;
        PaidBack = model.PaidBack;
        DateTaken = model.DateTaken;
    }
}