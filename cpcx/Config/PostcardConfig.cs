namespace cpcx.Config
{
    public class PostcardConfig
    {
        public const string Postcard = "Postcard";

        public int MaxDifferenceBetweenSentAndReceived { get; init; } = 2;
        public int MaxTravellingPostcards { get; init; } = 4;
        public int PostcardExpirationTimeInHours { get; init; } = 3;
    }
}
