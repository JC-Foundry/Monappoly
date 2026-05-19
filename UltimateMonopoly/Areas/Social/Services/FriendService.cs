using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using UltimateMonopoly.Data;
using UltimateMonopoly.Models.DataModels.Social;
using UltimateMonopoly.Models.ViewModels.Social;

namespace UltimateMonopoly.Areas.Social.Services;

public class FriendService
{
    private readonly IRepositoryManager _repos;
    private readonly AppDbContext _context;
    private readonly IUserInfo _userInfo;
    private readonly PresenceService _presence;
    private readonly LinkGenerator _linkGenerator;
    private readonly ILogger<FriendService> _logger;

    public FriendService(IRepositoryManager repos,
        AppDbContext context,
        IUserInfo userInfo,
        PresenceService presence,
        LinkGenerator linkGenerator,
        ILogger<FriendService> logger)
    {
        _repos = repos;
        _context = context;
        _userInfo = userInfo;
        _presence = presence;
        _linkGenerator = linkGenerator;
        _logger = logger;
    }

    private string? GetImgUrl(string? imgName)
    {
        string? imgUrl = null;
        if (!string.IsNullOrEmpty(imgName))
        {
            imgUrl = _linkGenerator.GetPathByPage(
                page: "/Profile/Index",
                handler: "AvatarImage",
                values: new { area = "Identity", name = imgName });
        }
        return imgUrl;
    }
    

    public async Task<List<FriendViewModel>> GetFriendsList()
    {
        var currentUserId = _userInfo.UserId;

        var friends = await _repos.GetRepository<Friend>()
            .AsQueryable()
            .Where(f => !f.IsDeleted && 
                        f.DateRemovedUtc == null
                        && (f.CreatedById == currentUserId
                            || f.FriendUserId == currentUserId))
            .ToListAsync();

        if (friends.Count == 0)
            return [];

        var friendUserIds = friends
            .Select(f => f.CreatedById == currentUserId ? f.FriendUserId : f.CreatedById!)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(u => friendUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var lastActive = await _presence.GetLastActiveUtcAsync(friendUserIds);

        var result = new List<FriendViewModel>(friends.Count);
        foreach (var friend in friends)
        {
            var friendUserId = friend.CreatedById == currentUserId
                ? friend.FriendUserId
                : friend.CreatedById!;

            if (!users.TryGetValue(friendUserId, out var user))
                continue;

            var lastSeen = lastActive.TryGetValue(friendUserId, out var ts) ? ts : null;
            var isOnline = _presence.IsOnline(friendUserId);

            var imgUrl = GetImgUrl(user.AvatarImageName);
            result.Add(new FriendViewModel(
                currentUserId,
                friend,
                user,
                imgUrl,
                lastSeen,
                isOnline));
        }

        return result.OrderBy(f => f.DisplayName).ToList();
    }


    #region Friend Requests


    public async Task<FriendRequestLists> GetFriendRequests()
    {
        var allRequests = await _context.FriendRequests.FilterDeleted(DeletedQueryType.OnlyActive)
            .Where(r => r.IsAccepted == null && (r.CreatedById == _userInfo.UserId || r.ToUserId == _userInfo.UserId))
            .ToListAsync();

        var userIds = allRequests.Select(r => r.CreatedById == _userInfo.UserId ? r.ToUserId : r.CreatedById)
            .Distinct().ToList();
        var users = await _context.Users.Where(u => u.IsEnabled && userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);
        
        var incomingRequests = new List<FriendRequestViewModel>();
        var outgoingRequests = new List<FriendRequestViewModel>();
        foreach (var request in allRequests)
        {
            var createdId = request.CreatedById ?? throw new InvalidOperationException("User ID not set");
            
            var outgoing = request.CreatedById == _userInfo.UserId;
            if(!users.TryGetValue(outgoing ? request.ToUserId : createdId, out var user))
                continue;
            
            var imgUrl = GetImgUrl(user.AvatarImageName);
            var requestViewModel = new FriendRequestViewModel(_userInfo.UserId, request, user, imgUrl);
            if(outgoing)
                outgoingRequests.Add(requestViewModel);
            else
                incomingRequests.Add(requestViewModel);
        }
        
        return new FriendRequestLists
        {
            IncomingRequests = incomingRequests,
            OutgoingRequests = outgoingRequests
        };
    }
    
    
    public record FriendRequestResult(bool Success, string? ErrorMessage);

    public async Task<FriendRequestResult> TrySendFriendRequest(string friendUsername)
    {
        //Get The user:
        var user = await _context.Users.Where(u => u.IsEnabled)
            .FirstOrDefaultAsync(u => u.UserName != null && u.UserName.ToLower() == friendUsername.ToLower());
        if (user == null) return new FriendRequestResult(false, "No user exists with this username.");
        
        //Check if self:
        if(string.Equals(user.Id, _userInfo.UserId, StringComparison.OrdinalIgnoreCase))
            return new FriendRequestResult(false, "Cannot send friend request to yourself.");
        
        //Check if pending:
        var pendingRequests = await _context.FriendRequests.FilterDeleted(DeletedQueryType.OnlyActive)
            .Where(f => f.IsAccepted == null 
                        && ((f.CreatedById == user.Id && f.ToUserId == _userInfo.UserId) 
                            || (f.ToUserId == user.Id && f.CreatedById == _userInfo.UserId)))
            .ToListAsync();
        if (pendingRequests.Count > 0)
        {
            var anyOutgoing = pendingRequests.Any(r => r.CreatedById == _userInfo.UserId);
            var anyIncoming = pendingRequests.Any(r => r.ToUserId == _userInfo.UserId);
            if(anyIncoming)
                return new FriendRequestResult(false, "Incoming friend request already pending.");
            
            return anyOutgoing 
                ? new FriendRequestResult(false, "Outgoing friend request already pending.") 
                : new FriendRequestResult(false, "Friend request already pending.");
        }
        
        //Check if already friends:
        var friends = await _context.Friends.AnyAsync(f => !f.IsDeleted
                                                           && ((f.CreatedById == _userInfo.UserId && f.FriendUserId == user.Id)
                                                               || (f.FriendUserId == _userInfo.UserId && f.CreatedById == user.Id)));
        if (friends) return new FriendRequestResult(false, "Already friends.");
        
        //Check if blocked:
        var isBlocked = await _context.BlockedUsers.AnyAsync(b => !b.IsDeleted 
                                                                  && ((b.BlockedUserId == user.Id && b.CreatedById == _userInfo.UserId) 
                                                                      || (b.CreatedById == user.Id && b.BlockedUserId == _userInfo.UserId)));
        if (isBlocked) 
            //Message is ambiguous so that it is not revealed that someone blocked you
            return new FriendRequestResult(false, "No user exists with this username.");
        
        var request = new FriendRequest(user.Id);
        await _repos.GetRepository<FriendRequest>()
            .AddAsync(request);
        return new FriendRequestResult(true, null);
    }

    public async Task<bool> TryAcceptFriendRequest(string requestId)
    {
        var request = await _context.FriendRequests.FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsAccepted == null && r.ToUserId == _userInfo.UserId);
        if (request == null) return false;
        
        request.Accept();
        var friend = new Friend();
        friend.Add(request.CreatedById ?? throw new InvalidOperationException("User ID not set"));

        return await ProcessFriendRequest(request, friend);        
    }

    public async Task<bool> TryDeclineFriendRequest(string requestId)
    {
        var request = await _context.FriendRequests.FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.IsAccepted == null && r.ToUserId == _userInfo.UserId);
        if (request == null) return false;
        
        request.Decline();
        return await ProcessFriendRequest(request, null);
    }

    private async Task<bool> ProcessFriendRequest(FriendRequest request, Friend? friend)
    {
        await _repos.BeginTransactionAsync();
        try
        {
            if(friend != null)
                await _repos.GetRepository<Friend>()
                    .AddAsync(friend, saveNow: false);
            
            await _repos.GetRepository<FriendRequest>()
                .UpdateAsync(request, saveNow: false);

            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to {Process} friend request: {RequestId}", 
                friend != null ? "accept" : "decline", request.Id);
            return false;
        }
    }

    #endregion
    
}