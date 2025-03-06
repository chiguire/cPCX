using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public class MainEventService(IOptions<CpcxConfig> cpcxConfig, IServiceScopeFactory scopeFactory, ILogger<MainEventService> logger)
{
    private readonly CpcxConfig _cpcxConfig = cpcxConfig.Value;
    
    private Guid _mainEventId = Guid.Empty;
    private string _mainEventPublicId = string.Empty;

    public async Task<Guid> GetMainEventId()
    {
        if (_mainEventId != Guid.Empty) return _mainEventId;

        await InitMainEvent();

        return _mainEventId;
    }
    
    public async Task<string> GetMainEventPublicId()
    {
        if (_mainEventPublicId != String.Empty) return _mainEventPublicId;

        await InitMainEvent();

        return _mainEventPublicId;
    }

    private async Task InitMainEvent()
    {
        Event? mainEvent = null;

        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (context == null)
            {
                logger.LogError("Main Event Error: ApplicationDbContext");
                throw new CPCXException(CPCXErrorCode.MainEventNotSet);
            }
            
            mainEvent = await context.Events.FirstOrDefaultAsync(e =>
                e.PublicId == _cpcxConfig.ActiveEventId
            );
        }

        if (mainEvent == null)
        {
            logger.LogError("Main Event Not Set or Not Found");
            throw new CPCXException(CPCXErrorCode.MainEventNotSet);
        }

        _mainEventId = mainEvent.Id;
        _mainEventPublicId = mainEvent.PublicId;
    }
}