using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.Postcard;

public class Travelling(UserManager<CpcxUser> userManager,
                        IPostcardService postcardService,
                        IOptionsSnapshot<PostcardConfig> postcardConfig,
                        MainEventService mainEventService) : MessagePageModel
{
    private readonly PostcardConfig _postcardConfig = postcardConfig.Value;

    public List<cpcx.Entities.Postcard> Postcards { get; set; } = null!;
    
    public int TravellingPostcardsCount { get; set; }
    
    public int MaxTravellingPostcardsCount { get; set; }
    
    public async Task<ActionResult> OnGet()
    {
        var us = await userManager.GetUserAsync(User);
        var evId = await mainEventService.GetMainEventId();
        var postcards = await postcardService.GetTravellingPostcards(us!.Id, evId);

        Postcards = postcards;
        TravellingPostcardsCount = Postcards.Count;
        MaxTravellingPostcardsCount = _postcardConfig.MaxTravellingPostcards;

        return Page();
    }
}