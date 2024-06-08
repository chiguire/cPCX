using Microsoft.AspNetCore.Identity;

namespace cpcx.Entities
{
    public class Event
    {  
        public string Id { get; set; }
        public string Name { get; set; }
        public string Venue { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<User> Users { get; set; } = [];

        public List<Postcard> Postcards { get; set; } = [];
    }
}
