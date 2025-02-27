using AutoMapper;
using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.Extensions.Options;

namespace cpcx.Services
{
    public class PostcardService(
        ApplicationDbContext context,
        IMapper mapper,
        IEventService eventService,
        IOptionsSnapshot<PostcardConfig> postcardConfig,
        ILogger<PostcardService> logger)
    {
        private readonly IMapper _mapper = mapper;
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
                Id = Guid.NewGuid().ToString(),
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

        private async Task<EventUser?> GetAvailableAddress(CpcxUser u, string eventId)
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

            var address = context.EventUsers.FirstOrDefault(
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
                    eu_.PostcardsSent - eu_.PostcardsReceived < postcardConfig.Value.MaxDifferenceBetweenSentAndReceived
                    );
            
            return address;
        }

        private bool UserCanSendPostcard(EventUser eu)
        {
            if (eu.ActiveInEvent == false)
            {
                return false;
            }

            // TODO - Allow custom number of travelling postcards per event
            var travellingPostcardsNum = context.Postcards.Count(
                p =>
                    // Postcards from THIS event
                    p.Event.Id == eu.EventId &&
                    // Postcards from THIS user
                    p.Sender.Id == eu.UserId &&
                    // Postcard hasn't been registered yet
                    p.ReceivedOn == null
            );

            return travellingPostcardsNum < _postcardConfig.MaxTravellingPostcards;
        }
    }
}
