using System.Text.Json.Serialization;

namespace MiniLibrary
{
    public class LoginResponse
    {
        public string? Token { get; set; }

        public int Duration { get; set; }
    }
}
