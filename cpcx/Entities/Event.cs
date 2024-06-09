using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities
{
    public class Event
    {  
        public string Id { get; set; }
        public string Name { get; set; } = "";
        public string Venue { get; set; } = "";
        public bool Visible { get; set; } = true;
        public bool Open { get; set; } = false;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<User> Users { get; set; } = [];

        public List<Postcard> Postcards { get; set; } = [];
    }
}
