using cpcx.Config;
using cpcx.Data;
using cpcx.Dto;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services
{
    public interface IPostcardService
    {
        Task<Postcard> SendPostcard(CpcxUser u, Event e);
        Task<Postcard> RegisterPostcard(CpcxUser u, string publicEventId, string postcardId);
        Task<Postcard> GetPostcard(string postcardId);
        
        Task<List<Postcard>> GetTravellingPostcards(Guid userId, Guid eventId, bool includeExpired);
        Task<(List<Postcard> Items, int TotalCount)> GetSentPostcards(Guid userId, Guid eventId, int page, int pageSize);
        Task<(List<Postcard> Items, int TotalCount)> GetReceivedPostcards(Guid userId, Guid eventId, int page, int pageSize);
    }

    public class PostcardService(
        ApplicationDbContext context,
        IEventService eventService,
        IOptionsSnapshot<PostcardConfig> postcardConfig,
        ILogger<PostcardService> logger,
        TimeProvider timeProvider) : IPostcardService
    {
        private readonly PostcardConfig _postcardConfig = postcardConfig.Value;

        public async Task<Postcard> SendPostcard(CpcxUser u, Event e)
        {
            var eu = await context.EventUsers.FindAsync(e.Id, u.Id);

            if (eu == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, e.Id);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            var postcardExpiredTime = timeProvider.GetUtcNow().AddHours(-_postcardConfig.PostcardExpirationTimeInHours);
            var travellingCount = await context.Postcards.CountAsync(p =>
                p.Sender.Id == u.Id &&
                p.Event.Id == e.Id &&
                p.SentOn > postcardExpiredTime &&
                (p.ReceivedOn == null || p.ReceivedOn == DateTimeOffset.UnixEpoch)
            );

            if (travellingCount >= _postcardConfig.MaxTravellingPostcards)
            {
                logger.LogInformation("User {UserId} has reached the travelling postcard limit ({Limit})", u.Id, _postcardConfig.MaxTravellingPostcards);
                throw new CPCXException(CPCXErrorCode.TravelingPostcardLimitReached);
            }

            var allocation = await eventService.AllocatePostcardAsync(e.Id, u.Id);

            if (allocation == null)
            {
                logger.LogWarning("No addresses available for postcard");
                throw new CPCXException(CPCXErrorCode.NoAddressesFoundInEvent);
            }

            var receiver = await context.Users.FindAsync(allocation.ReceiverUserId);

            logger.LogInformation("User {Sender} will send a postcard to {Receiver} at {Address}, PCID {EPID}-{PCID}", u.UserName, receiver?.UserName, allocation.ReceiverAddress, e.PublicId, allocation.PostcardId);

            var np = new Postcard
            {
                Id = Guid.NewGuid(),
                Event = e,
                Sender = u,
                Receiver = receiver!,
                SentOn = timeProvider.GetUtcNow(),
                PostcardId = allocation.PostcardId,
            };

            context.Postcards.Add(np);
            await context.SaveChangesAsync();

            return np;
        }

        public async Task<Postcard> RegisterPostcard(CpcxUser u, string publicEventId, string postcardId)
        {
            var ev = await context.Events.FirstOrDefaultAsync(e => e.PublicId == publicEventId);
            
            if (ev == null)
            {
                logger.LogError("Event with public ID {EventPublicId} not found", publicEventId);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }
            
            var receiverEu = await context.EventUsers.FindAsync(ev.Id, u.Id);

            if (receiverEu == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, ev.Id);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            var postcardToRegister = await context.Postcards
                .Include(p => p.Event)
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .FirstOrDefaultAsync(p =>
                // Postcard from this event
                p.Event.PublicId == publicEventId &&
                // Postcard meant for this user
                p.Receiver.Id == u.Id &&
                // Postcard has the correct postcard ID
                p.PostcardId == postcardId
            );

            if (postcardToRegister == null)
            {
                logger.LogError("Postcard {publicEventId}-{postcardId} not found, or not meant for user {userId}", publicEventId, postcardId, u.UserName);
                throw new CPCXException(CPCXErrorCode.PostcardNotFound);
            }
            
            var senderEu = await context.EventUsers.FindAsync(ev.Id, postcardToRegister.Sender.Id);

            if (senderEu == null)
            {
                logger.LogError("Postcard {publicEventId}-{postcardId} was found and meant for user {userId} but Sender is not part of the event", publicEventId, postcardId, u.UserName);
                throw new CPCXException(CPCXErrorCode.Unknown);
            }
            
            postcardToRegister.ReceivedOn = timeProvider.GetUtcNow();
            senderEu.PostcardsSent += 1;
            senderEu.PriorityScore++;
            receiverEu.PostcardsReceived += 1;

            context.EventUsers.Update(senderEu);
            context.EventUsers.Update(receiverEu);
            context.Postcards.Update(postcardToRegister);
            await context.SaveChangesAsync();

            return postcardToRegister;
        }

        public async Task<Postcard> GetPostcard(string postcardId)
        {
            var postcardIdParts = postcardId.Trim().Split("-", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Postcard ID format: XXX-YYYYY - where XXX is the event publicId (e.g. E26), and YYYYY is at least 1 digit
            if (postcardIdParts.Length != 2 ||                     // No hyphen in postcard id 
                postcardIdParts[0].Length != 3 ||                  // event publicId is a 3-character string
                int.TryParse(postcardIdParts[1], out _) == false)  // postcard number in event is an integer
            {
                logger.LogInformation("Postcard ID {postcardId} has incorrect format", postcardId);
                throw new CPCXException(CPCXErrorCode.PostcardIdInvalidFormat);
            }

            var p = await context.Postcards
                .Include(p => p.Event)
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .FirstOrDefaultAsync(p =>
                p.Event.PublicId == postcardIdParts[0] && 
                p.PostcardId == postcardIdParts[1]
                );
            
            if (p == null)
            {
                logger.LogInformation("Postcard ID {postcardId} not found", postcardId);
                throw new CPCXException(CPCXErrorCode.PostcardNotFound);
            }

            return p;
        }
        
        public async Task<List<Postcard>> GetTravellingPostcards(Guid userId, Guid eventId, bool includeExpired)
        {
            var eventUser = await context.EventUsers.FindAsync(eventId, userId);

            if (eventUser == null)
            {
                logger.LogError("User {UserId} has not joined Event {EventId}", userId, eventId);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }
        
            var currentDateTime = timeProvider.GetUtcNow();
            // Postcards sent before this time are considered expired
            var postcardExpiredTime = currentDateTime.AddHours(-_postcardConfig.PostcardExpirationTimeInHours);

            var travellingPostcards = await context.Postcards
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .Include(p => p.Event)
                .Where(p =>
                    // Postcards from this user
                    p.Sender.Id == userId &&
                    // Postcards from this event
                    p.Event.Id == eventId &&
                    // If Expired Postcards aren't included, check expiry
                    // Duplicate p.IsExpired here because of EF limitations
                    (includeExpired || p.SentOn > postcardExpiredTime) &&
                    // Postcard hasn't been registered yet
                    (p.ReceivedOn == null || p.ReceivedOn == DateTimeOffset.UnixEpoch)
                ).ToListAsync();
        
            return travellingPostcards;
        }

        public async Task<(List<Postcard> Items, int TotalCount)> GetSentPostcards(Guid userId, Guid eventId, int page, int pageSize)
        {
            var query = context.Postcards
                .Include(p => p.Event)
                .Include(p => p.Receiver)
                .Where(p => p.Sender.Id == userId && p.Event.Id == eventId &&
                            p.ReceivedOn != null && p.ReceivedOn != DateTimeOffset.UnixEpoch)
                .OrderByDescending(p => p.SentOn);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<(List<Postcard> Items, int TotalCount)> GetReceivedPostcards(Guid userId, Guid eventId, int page, int pageSize)
        {
            var query = context.Postcards
                .Include(p => p.Event)
                .Include(p => p.Sender)
                .Where(p => p.Receiver.Id == userId && p.Event.Id == eventId &&
                            p.ReceivedOn != null && p.ReceivedOn != DateTimeOffset.UnixEpoch)
                .OrderByDescending(p => p.SentOn);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
