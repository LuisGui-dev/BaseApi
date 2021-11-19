using Newtonsoft.Json;

namespace Base.API.Models.Responses
{
    public class CustomerResponse
    {
        private static readonly int CUSTOMER_PER_PAGE = 20;
        
        public CustomerResponse(Customer customer)
        {
            Success = true;
            Customer = customer;
        }

        public CustomerResponse(bool succes, string message)
        {
            Success = succes;
            if (succes) SuccessMessage = message;
            else ErrorMessage = message;
        }

        public bool Success { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        
        [JsonProperty("info")]
        public Customer Customer { get; set; }
    }
}