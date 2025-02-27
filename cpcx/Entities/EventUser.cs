using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class EventUser
    {
        [ForeignKey("Event")]
        public string EventId { get; set; }
        public Event Event { get; init; } = null!;
        [ForeignKey("User")]
        public string UserId { get; set; }
        public CpcxUser User { get; init; } = null!;

        [MaxLength(150)]
        public string Address { get; set; } = "";
        public bool ActiveInEvent { get; set; } = true;
        
        public int PostcardsSent { get; set; } = 0;
        public int PostcardsReceived { get; set; } = 0;
    }
}
