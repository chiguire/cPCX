using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities
{
    public class Event
    {
        public Guid Id { get; set; }
        [MaxLength(60)] public string ShortName { get; set; } = null!;
        [MaxLength(60)] public string Name { get; set; } = null!;
        [MaxLength(500)] public string URL { get; set; } = null!;
        [MaxLength(3)] public string PublicId { get; set; } = null!;
        public int LastPostcardId { get; set; } = 1;
        [MaxLength(150)] public string Venue { get; set; } = null!;
        public bool Visible { get; set; } = true;
        public bool Open { get; set; } = false;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
