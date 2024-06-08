using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities
{
    public class User : IdentityUser
    {  
        public string Alias { get; set; }
        public string Pronouns { get; set; }

        public List<Event> Events { get; set; } = [];
        public List<Postcard> PostcardsSent { get; set; } = [];
        public List<Postcard> PostcardsReceived { get; set; } = [];
    }
}
