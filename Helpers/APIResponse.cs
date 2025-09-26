using System.Net;

namespace ECommerce.Helpers
{
    public class APIResponse
    {
        public APIResponse()
        {
            ErrorMessages = new List<string>();
        }
        public HttpStatusCode StatusCode { get; set; }

        public bool IsSuccess { get; set; } = true;

        //For General Information or Success Message
        public string? Message { get; set; }

        //Detailed Error List
        public List<string> ErrorMessages { get; set; }

        public object? Result { get; set; }
    }
}