namespace AMZN.Shared.Exceptions
{
    public class ApiException : Exception
    {
        public string Code { get; }     // наш ErrorCodes
        public int StatusCode { get; }

        public ApiException(string code, string message, int statusCode)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }
    }
}
