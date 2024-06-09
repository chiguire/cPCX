using cpcx.Data;
using cpcx.Exceptions;
using cpcx.Inputs;

namespace cpcx.Services
{
    public class EventService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventService> _logger;

        public EventService(ApplicationDbContext context, ILogger<EventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void CreateEvent(EventInput eventInput)
        {
            // Check if name is unique
            if (_context.Events.Any(e => string.Equals(e.Name, eventInput.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new CPCXException(CPCXErrorCode.EventNameAlreadyUsed);
            }


        }
    }
}
