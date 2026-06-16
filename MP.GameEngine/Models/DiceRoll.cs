using System.ComponentModel.DataAnnotations;
using MP.GameEngine.Enums;

namespace MP.GameEngine.Models;

public class DiceRoll
{
    [Range(1, 6)]
    public ushort Die1 { get; }
    
    [Range(1, 6)]
    public ushort? Die2 { get; }
    [Range(1, 6)]
    public ushort? ThirdDie { get; }
    
    public DiceRollType RollType { get; }
    
    public bool IsTurnRoll { get; }

    public bool IsDoubleFive => Die1 == 5 && Die2 == 5 && RollType == DiceRollType.Double;
    
    public DiceRoll(ushort die1, ushort die2, ushort thirdDie, bool isTurnRoll = true)
    {
        Die1 = die1;
        Die2 = die2;
        ThirdDie = thirdDie;
        IsTurnRoll = isTurnRoll;

        RollType = isTurnRoll
            ? die1 == die2 && die2 == thirdDie
                ? DiceRollType.Triple
                : die1 == die2
                    ? DiceRollType.Double
                    : DiceRollType.Normal
            : DiceRollType.Normal;
    }

    public DiceRoll(DiceRoll roll, DiceRollType modifiedRollType)
    {
        Die1 = roll.Die1;
        Die2 = roll.Die2;

        ThirdDie = modifiedRollType == DiceRollType.Triple ? Die1 : roll.Die1;
        RollType = modifiedRollType;
        IsTurnRoll = true;
    }

    public DiceRoll(ushort die1, ushort? die2 = null)
    {
        Die1 = die1;
        Die2 = die2;
        
        ThirdDie = null;
        IsTurnRoll = false;
        RollType = DiceRollType.Normal;
    }
}