namespace UltimateMonopoly.Models.ViewModels.Social;

public class FriendRequestLists
{
    public IReadOnlyList<FriendRequestViewModel> IncomingRequests { get; set; }
    public IReadOnlyList<FriendRequestViewModel> OutgoingRequests { get; set; }
}