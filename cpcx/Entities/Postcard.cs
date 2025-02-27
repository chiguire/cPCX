using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class Postcard
    {
        [MaxLength(36)] public required string Id { get; init; }

        public Event Event { get; init; } = null!;
        public required DateTime SentOn {  get; set; }
        public DateTime? ReceivedOn { get; set; }
        public CpcxUser Sender { get; init; } = null!;
        public CpcxUser Receiver { get; init; } = null!;
        
        [MaxLength(10)]
        public required string PostcardId { get; set; }
    }
}
