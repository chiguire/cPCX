using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public interface IUserService
{
    Task<int> GetTravellingPostcards(CpcxUser user, Event @event);
    Task<string> GetUserAddress(CpcxUser user, Event @event);
}

public class UserService(ApplicationDbContext context, IOptionsSnapshot<PostcardConfig> postcardConfig, ILogger<UserService> logger) : IUserService
{
    private readonly PostcardConfig _postcardConfig = postcardConfig.Value;
    
    public async Task<int> GetTravellingPostcards(CpcxUser user, Event @event)
    {
        var eventUser = await context.EventUsers.FindAsync(@event.Id, user.Id);

        if (eventUser == null)
        {
            logger.LogError("User {UserId} has not joined Event {EventId}", user.Id, @event.Id);
            throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
        }
        
        var currentDateTime = DateTime.UtcNow;
        // Postcards sent before this time are considered expired
        var postcardExpiredTime = currentDateTime.AddHours(-_postcardConfig.PostcardExpirationTimeInHours);

        var travellingPostcardCount = await context.Postcards.CountAsync(p =>
            // Postcards from this user
            p.Sender.Id == user.Id &&
            // Postcards from this event
            p.Event.Id == @event.Id &&
            // Postcard hasn't already expired
            p.SentOn >= postcardExpiredTime &&
            // Postcard hasn't been registered yet
            (p.ReceivedOn == null || p.ReceivedOn == DateTime.UnixEpoch)
        );
        
        return travellingPostcardCount;
    }

    public async Task<string> GetUserAddress(CpcxUser user, Event @event)
    {
        var eventUser = await context.EventUsers.FindAsync(@event.Id, user.Id);

        if (eventUser == null)
        {
            logger.LogError("User {UserId} has not joined Event {EventId}", user.Id, @event.Id);
            throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
        }
        
        return eventUser.Address;
    }
}