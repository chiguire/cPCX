namespace cpcx.Config
{
    public class PostcardConfig
    {
        public const string Postcard = "Postcard";

        public int MaxDifferenceBetweenSentAndReceived { get; set; } = 2;
        public int MaxTravellingPostcards { get; set; } = 4;
    }
}
