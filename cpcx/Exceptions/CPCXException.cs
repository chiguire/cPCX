namespace cpcx.Exceptions
{
    public class CPCXException : Exception
    {
        public CPCXException(string message) : base(message) { }

        public CPCXException(CPCXErrorCode errorCode) : base() 
        {
            ErrorCode = errorCode;
        }

        public CPCXErrorCode ErrorCode { get; set; } = CPCXErrorCode.Unknown;

        public static string ErrorCodeMessage(CPCXErrorCode errorCode)
        {
            switch (errorCode)
            {
                case CPCXErrorCode.None:
                    return "None";
                case CPCXErrorCode.Unknown:
                    return "Unknown Error";
                case CPCXErrorCode.EventNotFound:
                    return "Event Not Found";
                case CPCXErrorCode.EventNameAlreadyUsed:
                    return "Event Name Already Used";
                case CPCXErrorCode.EventStartAfterEnd:
                    return "Event Start After End";
                case CPCXErrorCode.EventUserAlreadyJoined:
                    return "Event User Already Joined";
                case CPCXErrorCode.EventUserNotJoined:
                    return "Event User Not Joined";
                case CPCXErrorCode.EventUserAddressEmpty:
                    return "Event User Address Empty";
                case CPCXErrorCode.PostcardIdInvalidFormat:
                    return "Postcard Id Invalid Format";
                case CPCXErrorCode.PostcardNotFound:
                    return "Postcard Not Found";
                case CPCXErrorCode.TravelingPostcardLimitReached:
                    return "Traveling Postcard Limit Reached";
                case CPCXErrorCode.NoAddressesFoundInEvent:
                    return "No Addresses Found";
                case CPCXErrorCode.MainEventNotSet:
                    return "Main Event Not Set";
                default:
                    throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null);
            }
        }
    }

    public enum CPCXErrorCode
    {
        None = 0,
        Unknown,
        EventNotFound,
        EventNameAlreadyUsed,
        EventStartAfterEnd,
        EventUserAlreadyJoined,
        EventUserNotJoined,
        EventUserAddressEmpty,
        PostcardIdInvalidFormat,
        PostcardNotFound,
        TravelingPostcardLimitReached,
        NoAddressesFoundInEvent,
        MainEventNotSet,
    }
}
