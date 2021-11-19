using Newtonsoft.Json;

namespace Base.API.Models.Responses
{
    public class UserResponse
    {
        public UserResponse(User user)
        {
            Success = true;
            User = user;
            AuthToken = user.AuthToken;
        }
        public UserResponse(bool succes, string message)
        {
            Success = succes;
            if (succes) SuccessMessage = message;
            else ErrorMessage = message;
        }
        public bool Success { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        
        [JsonProperty("auth_token")]
        public string AuthToken { get; set; }
        
        [JsonProperty("info")] 
        public User User { get; set; }
    }
}