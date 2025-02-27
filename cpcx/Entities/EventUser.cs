using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class EventUser
    {
        [ForeignKey("Event")] [MaxLength(36)] public string EventId { get; init; } = null!;
        public Event Event { get; init; } = null!;
        [ForeignKey("User")] [MaxLength(36)] public string UserId { get; init; } = null!;
        public CpcxUser User { get; init; } = null!;
        [MaxLength(150)] public string Address { get; set; } = null!;
        public bool ActiveInEvent { get; set; } = true;
        public int PostcardsSent { get; set; } = 0;
        public int PostcardsReceived { get; set; } = 0;
    }
}
