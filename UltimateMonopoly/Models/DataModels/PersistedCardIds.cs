using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace UltimateMonopoly.Models.DataModels;

[Index(nameof(CardText), IsUnique = true)]
public class PersistedCardIds : AuditModel
{
    [Key] 
    [MaxLength(38)] 
    public string CardId { get; private set; } = Guid.NewGuid().ToString();
        
    [Required]
    public string CardText { get; private set; }
    
    //Includes action IDs 
    [Required] 
    public string GroupIdJson { get; private set; } = "[]";

    [Required] 
    public string ConditionIdJson { get; private set; } = "[]";

    public PersistedCardIds()
    {
    }
    
    public PersistedCardIds(string text, IEnumerable<CardGroupIdInput> groupIdsInput, ushort conditionIdsCount = 1)
    {
        CardText = text;

        var groupIdsList = groupIdsInput.ToList();
        if(groupIdsList.Count == 0)
            throw new ArgumentException("Must have at least one group");

        var groupIds = new List<CardGroupIdJson>();
        foreach (var input in groupIdsList)
        {
            if(input.NumberOfActions == 0)
                throw new ArgumentException("Number of actions must be greater than 0");
            
            var groupId = Guid.NewGuid().ToString();
            var actionIds = new string[input.NumberOfActions];
            for (var i = 0; i < input.NumberOfActions; i++)
            {
                actionIds[i] = Guid.NewGuid().ToString();
            }
            
            groupIds.Add(new CardGroupIdJson(groupId, actionIds));
        }
        
        GroupIdJson = JsonSerializer.Serialize(groupIds);

        if (conditionIdsCount == 0)
            return;
        
        var conditionIds = new string[conditionIdsCount];
        for (var i = 0; i < conditionIdsCount; i++)
        {
            conditionIds[i] = Guid.NewGuid().ToString();
        }
        
        ConditionIdJson = JsonSerializer.Serialize(conditionIds);
    }
}

public record CardGroupIdInput(ushort NumberOfActions = 1);
public record CardGroupIdJson(string GroupId, string[] ActionIds);