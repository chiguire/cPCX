using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public interface IUserService
{
    Task<int> GetTravellingPostcards(Guid userId, Guid eventId);
    Task<string> GetUserAddress(Guid userId, Guid eventId);
    Task SetUserAddress(Guid userId, Guid eventId, string address);
    Task SetUserActiveInEvent(Guid userId, Guid eventId, bool value);
    Task SetUserPronoun(CpcxUser user, Pronoun pronoun);
    Task SetUserProfileDescription(CpcxUser user, string profileDescription);
    Task<EventUser> GetEventUser(Guid eventId, Guid userId);
}

public class UserService(ApplicationDbContext context, IOptionsSnapshot<PostcardConfig> postcardConfig, ILogger<UserService> logger) : IUserService
{
    private readonly PostcardConfig _postcardConfig = postcardConfig.Value;
    
    public async Task<int> GetTravellingPostcards(Guid userId, Guid eventId)
    {
        var eventUser = await context.EventUsers.FindAsync(eventId, userId);

        if (eventUser == null)
        {
            logger.LogError("User {UserId} has not joined Event {EventId}", userId, eventId);
            throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
        }
        
        var currentDateTime = DateTime.UtcNow;
        // Postcards sent before this time are considered expired
        var postcardExpiredTime = currentDateTime.AddHours(-_postcardConfig.PostcardExpirationTimeInHours);

        var travellingPostcardCount = await context.Postcards.CountAsync(p =>
            // Postcards from this user
            p.Sender.Id == userId &&
            // Postcards from this event
            p.Event.Id == eventId &&
            // Postcard hasn't already expired
            p.SentOn >= postcardExpiredTime &&
            // Postcard hasn't been registered yet
            (p.ReceivedOn == null || p.ReceivedOn == DateTime.UnixEpoch)
        );
        
        return travellingPostcardCount;
    }

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
}