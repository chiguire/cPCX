using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities
{
    public class Event
    {  
        [MaxLength(36)]
        public string Id { get; set; }
        [MaxLength(60)]
        public string Name { get; set; } = "";
        [MaxLength(3)]
        public string PublicId { get; set; } = "";

        public int LastPostcardId { get; set; } = 1;
        [MaxLength(150)]
        public string Venue { get; set; } = "";
        public bool Visible { get; set; } = true;
        public bool Open { get; set; } = false;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
