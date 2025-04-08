using AutoMapper;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Inputs;
using Microsoft.EntityFrameworkCore;

namespace cpcx.Services
{
    public interface IEventService
    {
        Task AddUser(Guid id, CpcxUser u, string address);
        Task UpdateUserAddress(Guid id, CpcxUser u, string address);
        Task CreateEvent(EventInput eventInput);
        Task<Event?> GetEvent(Guid id);
        Task RemoveUser(Guid id, CpcxUser u);
        Task SetEventDates(Guid id, DateTime start, DateTime end);
        Task SetEventName(Guid id, string value);
        Task SetEventOpen(Guid id, bool value);
        Task SetEventVenue(Guid id, string value);
        Task SetEventVisible(Guid id, bool value);
        Task<string> GetNextEventPostcardId(Guid id);
        Task<List<Event>> GetAvailableEvents();
        Task<List<EventUser>> GetEventsJoinedByUser(Guid userId);

    }

    public class EventService(ApplicationDbContext context, IMapper mapper, ILogger<EventService> logger)
        : IEventService
    {
        public async Task CreateEvent(EventInput eventInput)
        {
            // Check if name is unique
            if (context.Events.Any(e => string.Equals(e.Name, eventInput.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new CPCXException(CPCXErrorCode.EventNameAlreadyUsed);
            }

            Event e = mapper.Map<Event>(eventInput);
            e.Id = Guid.NewGuid();

            context.Events.Add(e);

            await context.SaveChangesAsync();
        }

        public async Task<Event?> GetEvent(Guid id)
        {
            return await context.FindAsync<Event>(id);
        }

        public async Task SetEventVisible(Guid id, bool value)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            e.Visible = value;
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task SetEventOpen(Guid id, bool value)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            e.Visible = value;
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task SetEventName(Guid id, string value)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }
            if (context.Events.Any(e => e.Id != id && e.Name == value))
            {
                logger.LogError("Event {Event} name {Name} already used in another event", id, value);
                throw new CPCXException(CPCXErrorCode.EventNameAlreadyUsed);
            }

            e.Name = value;
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task SetEventVenue(Guid id, string value)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            e.Venue = value;
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task SetEventDates(Guid id, DateTime start, DateTime end)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            if (start > end)
            {
                logger.LogError("Event {Event} start date {Start} is after end date {End}", id, start, end);
                throw new CPCXException(CPCXErrorCode.EventStartAfterEnd);
            }

            e.Start = start;
            e.End = end;
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task AddUser(Guid id, CpcxUser u, string address)
        {
            var eu = await context.FindAsync<EventUser>(id, u.Id);
            
            if (eu is not null)
            {
                logger.LogError("User {UserId} has already joined event {Event}", u.Id, id);
                throw new CPCXException(CPCXErrorCode.EventUserAlreadyJoined);
            }
            
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            var v = new EventUser
            {
                Event = e,
                User = u,
                Address = address,
            };

            context.EventUsers.Add(v);
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task UpdateUserAddress(Guid id, CpcxUser u, string address)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            var eu = await context.FindAsync<EventUser>(id, u.Id);
            if (eu == null)
            {
                logger.LogError("User {UserId} is not part of event {Event}", u.Id, id);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            eu.Address = address;

            context.Update(e);
            await context.SaveChangesAsync();
        }



        public async Task RemoveUser(Guid id, CpcxUser u)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }
            var eu = await context.FindAsync<EventUser>(id, u.Id);
            if (eu == null)
            {
                logger.LogError("User {UserId} has not joined event {Event}", u.Id, id);
                throw new CPCXException(CPCXErrorCode.EventUserNotJoined);
            }

            context.EventUsers.Remove(eu);
            context.Update(e);
            await context.SaveChangesAsync();
        }

        public async Task<string> GetNextEventPostcardId(Guid id)
        {
            var e = await GetEvent(id);
            if (e == null)
            {
                logger.LogError("Event {Event} not found", id);
                throw new CPCXException(CPCXErrorCode.EventNotFound);
            }

            var nextPostcardId = e.LastPostcardId;

            e.LastPostcardId += 1;

            context.Events.Update(e);
            await context.SaveChangesAsync();

            return nextPostcardId.ToString();
        }

        public async Task<List<Event>> GetAvailableEvents()
        {
            var evs = await context.Events
                .Where(
                ev => ev.Visible && ev.Open
            ).ToListAsync();

            return evs;
        }
        
        public async Task<List<EventUser>> GetEventsJoinedByUser(Guid userId)
        {
            var evs = await context.EventUsers
                .Include(ev => ev.Event)
                .Include(ev => ev.User)
                .Where(
                    eu => eu.User.Id == userId
                ).ToListAsync();

            return evs;
        }
    }
}
