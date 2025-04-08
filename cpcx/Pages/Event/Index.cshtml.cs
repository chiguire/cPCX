using System.ComponentModel.DataAnnotations;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace cpcx.Pages.Event;

[Authorize]
public class Index(IEventService eventService,
                   UserManager<CpcxUser> userManager,
                   IUserService userService) : MessagePageModel
{
    public IEnumerable<cpcx.Entities.Event> EventsAvailableToJoin { get; set; } = null!;
    public IEnumerable<EventUser> EventsJoinedByUser { get; set; } = null!;

    [BindProperty] public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        public string EventId { get; set; } = "";
        
        [Display(Name = "Active in event", Description = "Uncheck this box to set yourself away from the event, you will not get postcards")]
        public bool UserActiveInEvent { get; set; }

        [Display(Name = "Address in event")]
        [Required(ErrorMessage = "Address is required. Set yourself to inactive until you have an address")]
        public string AddressInEvent { get; set; } = "";

    }
    
    public async Task<IActionResult> OnGet([FromQuery]string eventPublicId)
    {
        var us = (await userManager.GetUserAsync(User))!;

        var availableEvents = await eventService.GetAvailableEvents();
        var eventsUserIsIn = await eventService.GetEventsJoinedByUser(us.Id);

        var eventsToJoin = availableEvents.Where(ev => !eventsUserIsIn.Select(ev2 => ev2.EventId).Contains(ev.Id)).ToList();
        
        EventsAvailableToJoin = eventsToJoin;
        EventsJoinedByUser = eventsUserIsIn;
        
        

        return Page();
    }

    public async Task<IActionResult> OnPostEvent()
    {
        if (ModelState.IsValid)
        {
            var us = (await userManager.GetUserAsync(User))!;
            var eventId = new Guid(Input.EventId);
            var eu = await userService.GetEventUser(eventId, us.Id);
            
            if (eu.Address != Input.AddressInEvent)
            {
                await userService.SetUserAddress(us.Id, eventId, Input.AddressInEvent);
            }

            if (eu.ActiveInEvent != Input.UserActiveInEvent)
            {
                await userService.SetUserActiveInEvent(us.Id, eventId, Input.UserActiveInEvent);
            }
            
            SetStatusMessage("Changes updated", StatusMessageType.Success);
            return RedirectToPage("Index");
        }

        SetStatusMessage("Please see issues in form", StatusMessageType.Info);
        return RedirectToPage();
    }
}