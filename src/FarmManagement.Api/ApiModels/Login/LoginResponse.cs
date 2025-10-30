namespace FarmManagement.Api.ApiModels
{
    public class LoginResponse
    {
        public bool IsLoggedIn { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public LoginResponse() { }

        public LoginResponse(bool isLoggedIn, string token, string message)
        {
            IsLoggedIn = isLoggedIn;
            Token = token;
            Message = message;
        }
    }
}
