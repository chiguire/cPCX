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
                   MainEventService mainEventService,
                   UserManager<CpcxUser> userManager,
                   IUserService userService) : MessagePageModel
{
    [BindProperty] public InputModel Input { get; set; } = null!;
    
    public cpcx.Entities.Event CurrentEvent { get; set; } = null!;

    public class InputModel
    {
        public string EventId { get; set; } = "";
        
        [Display(Name = "Active in event", Description = "Uncheck this box to mark yourself away from the event (e.g. you're leaving early), you will not receive new postcards")]
        public bool UserActiveInEvent { get; set; }

        [Display(Name = "Address in event", Description = "Be as specific as necessary. Look out for experimental postcodes if that helps")]
        [Required(ErrorMessage = "Address is required. Set yourself to inactive until you have an address")]
        public string AddressInEvent { get; set; } = "";

    }
    
    public async Task<IActionResult> OnGet(string eventPublicId)
    {
        // While we have a single event
        // TO-DO: Once this page is for joining events, remove this
        if (string.IsNullOrEmpty(eventPublicId))
        {
            eventPublicId = await mainEventService.GetMainEventPublicId();
            return RedirectToPage("/Event/Index", new { eventPublicId = eventPublicId });
        }
        var us = (await userManager.GetUserAsync(User))!;
        var evId = await mainEventService.GetMainEventId();
        CurrentEvent = (await eventService.GetEvent(evId))!;
        var eu = await userService.GetEventUser(evId, us.Id);

        Input = new InputModel
        {
            AddressInEvent = eu.Address,
            EventId = eventPublicId,
            UserActiveInEvent = eu.ActiveInEvent,
        };
        
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