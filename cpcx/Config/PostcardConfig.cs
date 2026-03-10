namespace cpcx.Config
{
    public class PostcardConfig
    {
        public const string Postcard = "Postcard";

        public int MaxTravellingPostcards { get; init; } = 4;
        public int PostcardExpirationTimeInHours { get; init; } = 3;
    }
}
