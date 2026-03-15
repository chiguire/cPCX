using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public interface IUserService
{
    Task<string> GetUserAddress(Guid userId, Guid eventId);
    Task SetUserAddress(Guid userId, Guid eventId, string address);
    Task SetUserActiveInEvent(Guid userId, Guid eventId, bool value);
    Task SetUserPronoun(CpcxUser user, Pronoun pronoun);
    Task SetUserProfileDescription(CpcxUser user, string profileDescription);
    Task SetUserAvatar(CpcxUser user, string avatarPath);
    Task<EventUser> GetEventUser(Guid eventId, Guid userId);
    Task BlockUser(Guid blockerId, Guid blockedId);
    Task UnblockUser(Guid blockerId, Guid blockedId);
    Task<List<CpcxUser>> GetBlockedUsers(Guid userId);
    Task<bool> HasBlocked(Guid blockerId, Guid blockedId);
}

public class UserService(ApplicationDbContext context, ILogger<UserService> logger) : IUserService
{
    public async Task<string> GetUserAddress(Guid userId, Guid eventId)
    {
        var eventUser = await context.EventUsers.FindAsync(eventId, userId);

        if (eventUser == null)
        {
            logger.LogError("User {UserId} has not joined Event {EventId}", userId, eventId);
            throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
        }
        
        return eventUser.Address;
    }

    public async Task SetUserAddress(Guid userId, Guid eventId, string address)
    {
        var eu = await GetEventUser(eventId, userId);
        
        eu.Address = address;
        
        context.EventUsers.Update(eu);
        await context.SaveChangesAsync();
    }
    
    public async Task SetUserActiveInEvent(Guid userId, Guid eventId, bool value)
    {
        var eu = await GetEventUser(eventId, userId);
        
        eu.ActiveInEvent = value;
        
        context.EventUsers.Update(eu);
        await context.SaveChangesAsync();
    }

    public async Task SetUserPronoun(CpcxUser user, Pronoun pronoun)
    {
        user.Pronouns = pronoun;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task SetUserProfileDescription(CpcxUser user, string profileDescription)
    {
        user.ProfileDescription = profileDescription;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task SetUserAvatar(CpcxUser user, string avatarPath)
    {
        user.AvatarPath = avatarPath;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<EventUser> GetEventUser(Guid eventId, Guid userId)
    {
        var eu = await context.EventUsers.FindAsync(eventId, userId);

        if (eu == null)
        {
            logger.LogError("User {UserId} is not part of event {Event}", userId, eventId);
            throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
        }

        return eu;
    }

    public async Task BlockUser(Guid blockerId, Guid blockedId)
    {
        var blocker = await context.Users.Include(u => u.BlockedUsers).FirstOrDefaultAsync(u => u.Id == blockerId);
        var blocked = await context.Users.FindAsync(blockedId);
        if (blocker == null || blocked == null) return;

        blocker.BlockedUsers ??= [];
        if (!blocker.BlockedUsers.Any(u => u.Id == blockedId))
        {
            blocker.BlockedUsers.Add(blocked);
            await context.SaveChangesAsync();
        }
    }

    public async Task UnblockUser(Guid blockerId, Guid blockedId)
    {
        var blocker = await context.Users.Include(u => u.BlockedUsers).FirstOrDefaultAsync(u => u.Id == blockerId);
        if (blocker?.BlockedUsers == null) return;

        var blocked = blocker.BlockedUsers.FirstOrDefault(u => u.Id == blockedId);
        if (blocked != null)
        {
            blocker.BlockedUsers.Remove(blocked);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<CpcxUser>> GetBlockedUsers(Guid userId)
    {
        var user = await context.Users.Include(u => u.BlockedUsers).FirstOrDefaultAsync(u => u.Id == userId);
        return user?.BlockedUsers ?? [];
    }

    public async Task<bool> HasBlocked(Guid blockerId, Guid blockedId)
    {
        return await context.Users
            .Where(u => u.Id == blockerId)
            .AnyAsync(u => u.BlockedUsers!.Any(b => b.Id == blockedId));
    }
}