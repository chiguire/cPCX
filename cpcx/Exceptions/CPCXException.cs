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
    }

    public enum CPCXErrorCode
    {
        None = 0,
        Unknown,
        EventNameAlreadyUsed,
    }
}
