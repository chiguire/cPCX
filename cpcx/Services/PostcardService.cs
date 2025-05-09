﻿using AutoMapper;
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
        Task<Postcard> RegisterPostcard(CpcxUser u, string publicEventId, string postcardId);
        Task<Postcard> GetPostcard(string postcardId);
        
        Task<List<Postcard>> GetTravellingPostcards(Guid userId, Guid eventId, bool includeExpired);
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
            var eu = await context.EventUsers.FindAsync(e.Id, u.Id);

            if (eu == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, e.Id);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }
            
            var chosenAddress = await GetAvailableAddress(u, e.Id);

            if (chosenAddress == null)
            {
                logger.LogWarning("No addresses available for postcard");
                throw new CPCXException(CPCXErrorCode.NoAddressesFoundInEvent);
            }

            var postcardId = await eventService.GetNextEventPostcardId(e.Id);
            
            logger.LogInformation("User {Sender} will send a postcard to {Receiver} at {Address}, PCID {EPID}-{PCID}", u.UserName, chosenAddress.User.UserName, chosenAddress.Address, e.PublicId, postcardId);

            var np = new Postcard
            {
                Id = Guid.NewGuid(),
                Event = e,
                Sender = u,
                Receiver = chosenAddress.User,
                SentOn = DateTime.UtcNow,
                PostcardId = postcardId,
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

            if (await context.EventUsers.FindAsync(eventId, u.Id) == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, eventId);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            var blockedUserIds = u.BlockedUsers?.Select(u => u.Id).ToList() ?? [];

            var address = await context.EventUsers
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(
                eu =>
                    // Postcards from THIS event
                    eu.EventId == eventId &&
                    // Recipient is part of THIS event
                    eu.ActiveInEvent &&
                    // Sender can't send a postcard to themselves
                    eu.UserId != u.Id &&
                    // Recipient has this user blocked
                    !blockedUserIds.Contains(u.Id) &&
                    // Recipient can still receive postcards if they have sent a couple more postcards than they have received
                    eu.PostcardsSent - eu.PostcardsReceived < _postcardConfig.MaxDifferenceBetweenSentAndReceived
                    );
            
            return address;
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
            
            postcardToRegister.ReceivedOn = DateTime.UtcNow;
            senderEu.PostcardsSent += 1;
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
        
            var currentDateTime = DateTime.UtcNow;
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
                    (p.ReceivedOn == null || p.ReceivedOn == DateTime.UnixEpoch)
                ).ToListAsync();
        
            return travellingPostcards;
        }
    }
}
