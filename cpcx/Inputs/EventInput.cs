namespace cpcx.Inputs
{
    public class EventInput
    {
        public required string Name { get; set; }
        public required string Venue { get; set; }
        public bool Visible { get; set; }
        public bool Open { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
