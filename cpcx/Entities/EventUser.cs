using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class EventUser
    {
        [ForeignKey("Event")] public Guid EventId { get; init; }
        public Event Event { get; init; } = null!;
        [ForeignKey("User")] public Guid UserId { get; init; }
        public CpcxUser User { get; init; } = null!;
        [MaxLength(150)] public string Address { get; set; } = null!;
        public bool ActiveInEvent { get; set; } = true;
        public int PostcardsSent { get; set; } = 0;
        public int PostcardsReceived { get; set; } = 0;
    }
}
