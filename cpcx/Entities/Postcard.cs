using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace cpcx.Entities
{
    public class Postcard
    {
        public string Id { get; set; }
        public string EventId { get; set; }
        public Event Event { get; set; }
        public string PublicId { get; set; }
        public DateTime SentOn {  get; set; }
        public DateTime? ReceivedOn { get; set; }
        
        public string SenderId { get; set; }
        public User Sender { get; set; } = null!;
        public string ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        
        public bool IsTravelling()
        {
            return ReceivedOn is null;
        }
    }
}
