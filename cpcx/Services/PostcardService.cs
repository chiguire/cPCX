using AutoMapper;
using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services
{
    public interface IPostcardService
    {
        Task<Postcard> SendPostcard(CpcxUser u, Event e);
        Task<Postcard?> RegisterPostcard(CpcxUser u, string publicEventId, string postcardId);
        Task<Postcard> GetPostcard(string postcardId);
    }

    public class PostcardService(
        ApplicationDbContext context,
        IEventService eventService,
        IOptionsSnapshot<PostcardConfig> postcardConfig,
        ILogger<PostcardService> logger) : IPostcardService
    {
        private readonly PostcardConfig _postcardConfig = postcardConfig.Value;

        public async Task<Postcard> SendPostcard(CpcxUser u, Event e)
        {
            var chosenAddress = await GetAvailableAddress(u, e.Id);

            if (chosenAddress == null)
            {
                logger.LogWarning("No addresses available for postcard");
                throw new CPCXException(CPCXErrorCode.NoAddressesFoundInEvent);
            }

            var np = new Postcard
            {
                Id = Guid.NewGuid(),
                Event = e,
                Sender = u,
                Receiver = chosenAddress.User,
                SentOn = DateTime.UtcNow,
                PostcardId = await eventService.GetNextEventPostcardId(e.Id),
            };

            context.Postcards.Add(np);
            await context.SaveChangesAsync();

            return np;
        }

        private async Task<EventUser?> GetAvailableAddress(CpcxUser u, Guid eventId)
        {
            var e = await eventService.GetEvent(eventId);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", eventId);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            var eu = await context.EventUsers.FindAsync(eventId, u.Id);
            if (eu == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, eventId);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            var address = await context.EventUsers.FirstOrDefaultAsync(
                eu_ =>
                    // Postcards from THIS event
                    eu_.EventId == eventId &&
                    // Recipient is part of THIS event
                    eu_.ActiveInEvent &&
                    // Sender can't send a postcard to themselves
                    eu_.UserId != u.Id &&
                    // Recipient has this user blocked
                    u.BlockedUsers.Find(bu => bu.Id == eu_.UserId) == null &&
                    // Recipient can still receive postcards if they have sent a couple more postcards than they have received
                    eu_.PostcardsSent - eu_.PostcardsReceived < _postcardConfig.MaxDifferenceBetweenSentAndReceived
                    );
            
            return address;
        }

        public async Task<Postcard?> RegisterPostcard(CpcxUser u, string publicEventId, string postcardId)
        {
            var postcardToRegister = await context.Postcards.FirstOrDefaultAsync(p =>
                // Postcard from this event
                p.Event.PublicId == publicEventId &&
                // Postcard meant for this user
                p.Receiver.Id == u.Id &&
                // Postcard has the correct postcard ID
                p.PostcardId == postcardId
            );

            if (postcardToRegister == null)
            {
                logger.LogError("Postcard {publicEventId}-{postcardId} not found, or not meant for user {userId}", publicEventId, postcardId, u.Id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }
            
            postcardToRegister.ReceivedOn = DateTime.UtcNow;

            context.Postcards.Update(postcardToRegister);
            await context.SaveChangesAsync();

            return postcardToRegister;
        }

        public async Task<Postcard> GetPostcard(string postcardId)
        {
            var postcardIdParts = postcardId.Trim().Split(['-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Postcard ID format: XXX-YYYYY - where XXX is the event publicId (e.g. E26), and YYYYY is at least 1 digit
            if (postcardIdParts.Length != 2 ||                     // No hyphen in postcard id 
                postcardIdParts[0].Length != 3 ||                  // event publicId is a 3-character string
                int.TryParse(postcardIdParts[1], out _) == false)  // postcard number in event is an integer
            {
                logger.LogInformation("Postcard ID {postcardId} has incorrect format", postcardId);
                throw new CPCXException(CPCXErrorCode.PostcardIdInvalidFormat);
            }

            var p = await context.Postcards.FirstOrDefaultAsync(p =>
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
    }
}
