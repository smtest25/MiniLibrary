using System.Text.Json.Serialization;

namespace MiniLibrary
{
    public class LoginRequest
    {
        public string? User { get; set; }
        public string? Pass { get; set; }
    }
}
