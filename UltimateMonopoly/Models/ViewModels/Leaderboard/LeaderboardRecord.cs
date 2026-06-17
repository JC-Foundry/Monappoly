using UltimateMonopoly.Models.ViewModels.Social;

namespace UltimateMonopoly.Models.ViewModels.Leaderboard;

public class LeaderboardRecord
{
    public UserProfileViewModel UserProfile { get; }
    public bool AreFriends { get; }

    public LeaderboardRecord(UserProfileViewModel userProfile, bool areFriends)
    {
        UserProfile = userProfile;
        AreFriends = areFriends;
    }
}