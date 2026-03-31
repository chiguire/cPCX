using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class Postcard
    {
        public Guid Id { get; set; }

        public Event Event { get; init; } = null!;
        public required DateTimeOffset SentOn {  get; set; }
        public DateTimeOffset? ReceivedOn { get; set; }
        public CpcxUser Sender { get; init; } = null!;
        public CpcxUser Receiver { get; init; } = null!;
        
        [MaxLength(10)]
        public required string PostcardId { get; set; }

        public string FullPostCardId => $"{Event.PublicId}-{PostcardId}";

        public bool IsExpired(DateTimeOffset postcardExpiredTime) => SentOn <= postcardExpiredTime;
    }
}
