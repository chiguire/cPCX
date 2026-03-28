using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace cpcx.Pages;

public class IndexModel : MessagePageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<CpcxUser> _userManager;
    private readonly IUserService _userService;
    private readonly IPostcardService _postcardService;
    private readonly MainEventService _mainEventService;
    private readonly int _maxTravellingPostcards;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<CpcxUser> userManager,
        IUserService userService,
        IPostcardService postcardService,
        MainEventService mainEventService,
        IOptions<PostcardConfig> postcardConfig)
    {
        _logger = logger;
        _userManager = userManager;
        _userService = userService;
        _postcardService = postcardService;
        _mainEventService = mainEventService;
        _maxTravellingPostcards = postcardConfig.Value.MaxTravellingPostcards;
    }

    public CpcxUser? CurrentUser { get; set; }
    public EventUser? UserEventStats { get; set; }
    public bool HasEmptyAddress => UserEventStats is not null && string.IsNullOrEmpty(UserEventStats.Address);
    public bool IsManuallyInactive => UserEventStats is not null && !string.IsNullOrEmpty(UserEventStats.Address) && !UserEventStats.ActiveInEvent;
    public List<Entities.Postcard> TravellingPostcards { get; set; } = [];
    public int MaxTravellingPostcards => _maxTravellingPostcards;

    public async Task OnGetAsync()
    {
        CurrentUser = await _userManager.GetUserAsync(User);
        if (CurrentUser is null) return;

        var eventId = await _mainEventService.GetMainEventId();
        UserEventStats = await _userService.GetEventUser(eventId, CurrentUser.Id);
        TravellingPostcards = await _postcardService.GetTravellingPostcards(CurrentUser.Id, eventId, false);
    }
}
