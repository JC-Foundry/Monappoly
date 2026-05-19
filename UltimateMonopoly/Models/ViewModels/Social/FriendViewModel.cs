using JC.Core.Extensions;
using UltimateMonopoly.Data;
using UltimateMonopoly.Models.DataModels.Social;

namespace UltimateMonopoly.Models.ViewModels.Social;

public class FriendViewModel : FriendBaseViewModel
{
    public string FriendModelId { get; }
    public string FriendId { get; }

    public string FriendAddedDate { get; }

    public string LastSeen { get; }
    public bool IsOnline { get; }

    public uint NumberOfWins { get; }
    public uint NumberOfLosses { get; }
    public uint NumberOfDraws { get; }
    public uint NumberOfGamesPlayed => NumberOfWins + NumberOfLosses + NumberOfDraws;

    public FriendViewModel(string currentUserId, Friend friend, AppUser user, string? imgUrl, DateTime? lastSeenUtc, bool isOnline)
        : base(user, imgUrl)
    {
        FriendModelId = friend.Id;

        if (string.Equals(currentUserId, friend.CreatedById))
        {
            if (string.Equals(currentUserId, friend.FriendUserId))
                throw new InvalidOperationException("Cannot determine friend ID — both sides are the current user");

            FriendId = friend.FriendUserId;
        }
        else if (string.Equals(currentUserId, friend.FriendUserId))
        {
            FriendId = friend.CreatedById
                ?? throw new InvalidOperationException("User ID not set");
        }
        else
        {
            throw new InvalidOperationException("Current user is not part of this friendship");
        }

        FriendAddedDate = friend.DateAddedUtc.ToLocalTime().ToString("D");
        

        LastSeen = isOnline
            ? "Online"
            : lastSeenUtc?.ToRelativeTime() ?? "Never seen";
        IsOnline = isOnline;
        
        NumberOfWins = user.NumberOfWins;
        NumberOfLosses = user.NumberOfLosses;
        NumberOfDraws = user.NumberOfDraws;
    }
}

public class FriendBaseViewModel
{
    public string Username { get; }
    public string DisplayName { get; }
    public string Initial { get; }

    public string? AvatarColour { get; }
    public string? AvatarImageUrl { get; }

    public FriendBaseViewModel(AppUser user, string? imgUrl)
    {
        Username = user.UserName ?? "Unknown";
        DisplayName = user.DisplayName ?? Username;
        Initial = DisplayName.Length > 0 ? $"{DisplayName[0]}".ToUpperInvariant() : "U";

        AvatarColour = user.AvatarColour;
        AvatarImageUrl = imgUrl;
    }
}