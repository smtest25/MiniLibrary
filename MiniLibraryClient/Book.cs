using System.Text.Json.Serialization;

namespace MiniLibraryClient
{
    public class Book
    {
        [JsonIgnore]
        public int ID { get; set; }
        public Guid Guid { get; set; }
        public string? Name { get; set; }
        public string? Author { get; set; }
        public int Year { get; set; }
        public string? ISBN { get; set; }
        public int Amount { get; set; }
    }
}
