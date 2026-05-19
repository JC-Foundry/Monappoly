using System.Text.Json;
using JC.Core.Models;
using JC.Web.Security.Services;
using Microsoft.EntityFrameworkCore;
using UltimateMonopoly.Data;
using UltimateMonopoly.Services;

namespace UltimateMonopoly.Areas.Identity.Services;

public class ProfileService
{
    public const string CookieName = "user-profile";
    public const string ProtectorPurpose = "UserProfileProtector";
    public const string ImgFileType = ".png";

    //TODO: to be extended with pieces that are in the game
    //Note: your avatar image is a game piece
    private readonly string[] _avatarImageNames = ["car", "dog", "cat", "van", "horse", "wheel_barrow"];

    private readonly FilePathProvider _filePathProvider;
    private readonly AppDbContext _context;
    private readonly IUserInfo _userInfo;
    private readonly ICookieService _cookies;

    public ProfileService(
        FilePathProvider filePathProvider,
        AppDbContext context,
        IUserInfo userInfo,
        [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService cookies)
    {
        _filePathProvider = filePathProvider;
        _context = context;
        _userInfo = userInfo;
        _cookies = cookies;
    }

    public List<string> GetAvatarImagePaths()
        => (from imgName in _avatarImageNames
            let path = _filePathProvider.GetFilePath(FilePathProvider.FileCategory.ProfileImg)
            select Path.Combine(path, imgName + ImgFileType)
            into path where File.Exists(path) select path).ToList();

    public IReadOnlyList<string> GetAvailableAvatarImageNames()
    {
        var dir = _filePathProvider.GetFilePath(FilePathProvider.FileCategory.ProfileImg);
        return _avatarImageNames
            .Where(name => File.Exists(Path.Combine(dir, name + ImgFileType)))
            .ToList();
    }

    public string? GetAvatarImagePath(string name)
    {
        if (!_avatarImageNames.Contains(name)) return null;
        var path = Path.Combine(
            _filePathProvider.GetFilePath(FilePathProvider.FileCategory.ProfileImg),
            name + ImgFileType);
        return File.Exists(path) ? path : null;
    }

    public async Task<UserProfile> GetAsync()
    {
        var userId = _userInfo.UserId ?? throw new InvalidOperationException("No authenticated user");

        var cached = ReadCookie();
        if (cached?.UserId == userId)
            return new UserProfile(cached.AvatarColour, cached.AvatarImageName);

        if (cached is not null)
            _cookies.TryDeleteCookie(CookieName);

        var profile = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserProfile(u.AvatarColour, u.AvatarImageName))
            .FirstAsync();

        WriteCookie(userId, profile);
        return profile;
    }

    public async Task<bool> TryUpdateAsync(UserProfile updated)
    {
        var userId = _userInfo.UserId ?? throw new InvalidOperationException("No authenticated user");

        if (updated.AvatarImageName is not null && !_avatarImageNames.Contains(updated.AvatarImageName))
            return false;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;
        
        var updatedColour = user.SetAvatarColour(updated.AvatarColour);
        user.AvatarImageName = updated.AvatarImageName;
        await _context.SaveChangesAsync();

        var newProfile = updated with { AvatarColour = updatedColour };
        WriteCookie(userId, newProfile);
        return true;
    }

    private CookiePayload? ReadCookie()
    {
        var raw = _cookies.GetCookie(CookieName);
        if (raw is null) return null;
        try { return JsonSerializer.Deserialize<CookiePayload>(raw); }
        catch { return null; }
    }

    private void WriteCookie(string userId, UserProfile profile)
        => _cookies.TryCreateCookie(CookieName, JsonSerializer.Serialize(
            new CookiePayload(userId, profile.AvatarColour, profile.AvatarImageName)));

    private record CookiePayload(string UserId, string? AvatarColour, string? AvatarImageName);
}