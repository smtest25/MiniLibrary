using MiniLibrary;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MiniLibraryClient
{
    internal class Program
    {
        private static UriBuilder _serv = new();
        private static HttpClient _client = new();
        static async Task Main(string[] args)
        {
            using HttpClient client = new();
            _client = client;
            _serv = new UriBuilder("https", "localhost", 9999);

            Console.WriteLine($"Connecting to {_serv.Uri} ...");

            _serv.Path = "health";

            try
            {
                await client.GetStringAsync(_serv.Uri);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Could not connect. Is the API running?");
                return;
            }

            Console.WriteLine("Connected.");

            await CommandLoop();
        }

        static async Task CommandLoop()
        {
            var line = string.Empty;
            Console.WriteLine(@$"
Welcome to MiniLibrary!
For a list of commands type ""help"".");
            while (true)
            {
                Console.Write("> ");
                line = Console.ReadLine();

                var cmdParams = line!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (cmdParams.Length == 0)
                    continue;

                switch (cmdParams[0])
                {
                    case "exit":
                        return;
                    case "help":
                        HelpCommand(cmdParams);
                        break;
                    case "login":
                        await LoginCommand(cmdParams);
                        break;
                    case "init":
                        await InitCommand();
                        break;
                    case "list":
                        await ListCommand();
                        break;
                    case "add":
                        await AddCommand(cmdParams);
                        break;
                    case "find":
                        await FindCommand(cmdParams);
                        break;
                    case "borrow":
                        await BorrowCommand(cmdParams);
                        break;
                    case "return":
                        await ReturnCommand(cmdParams);
                        break;
                    default:
                        Console.WriteLine(@"Unknown command. For a list of commands type ""help"".");
                        break;
                }
            }
        }

        private static async Task LoginCommand(string[] cmdParams)
        {
            if (cmdParams.Length != 3)
            {
                Console.WriteLine(@"Invalid number of parameters. For help type ""help login"".");
                return;
            }

            var url = new UriBuilder(_serv.Uri);
            url.Path = $"login";

            var res = await _client.PostAsJsonAsync(url.Uri, new { user = cmdParams[1], pass = cmdParams[2] });

            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Invalid credentials!");
                _client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            var loginRes = await res.Content.ReadFromJsonAsync<LoginResponse>();

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginRes!.Token);

            Console.WriteLine("Login successful!");
        }

        private static void WriteBooks(List<Book>? books)
        {
            Console.WriteLine($"""

                Found {books?.Count} book(s)

                """);

            foreach (var book in books!)
            {
                Console.WriteLine($"""
GUID: {book.Guid}
Name: {book.Name}
Author: {book.Author}
Published: {book.Year}
ISBN: {book.ISBN}
Available: {book.Amount} units

""");
            }
        }

        private static async Task AddCommand(string[] cmdParams)
        {
            var book = new Book { };

            if (cmdParams.Length == 1)
            {
                Console.WriteLine("All fields are required. Enter blank value to quit.");

                Console.Write("Name: ");
                book.Name = Console.ReadLine()!.Trim();
                if (string.IsNullOrEmpty(book.Name))
                    return;

                Console.Write("Author: ");
                book.Author = Console.ReadLine()!.Trim();
                if (string.IsNullOrEmpty(book.Author))
                    return;

                while (true)
                {
                    Console.Write("Year published: ");
                    var yearString = Console.ReadLine()!.Trim();
                    if (string.IsNullOrEmpty(yearString))
                        return;
                    if (int.TryParse(yearString, out var year))
                    {
                        book.Year = year;
                        break;
                    }
                    Console.WriteLine("Invalid entry, try again.");
                }

                Console.Write("ISBN: ");
                book.ISBN = Console.ReadLine()!.Trim();
                if (string.IsNullOrEmpty(book.ISBN))
                    return;


                while (true)
                {
                    Console.Write("Units available: ");
                    var unitsString = Console.ReadLine()!.Trim();
                    if (string.IsNullOrEmpty(unitsString))
                        return;
                    if (int.TryParse(unitsString, out var units))
                    {
                        book.Amount = units;
                        break;
                    }
                    Console.WriteLine("Invalid entry, try again.");
                }

                Console.WriteLine("Adding book. Proceed? Y/N");
                if (Console.ReadKey(true).Key != ConsoleKey.Y)
                    return;


            }

            if (cmdParams.Length > 1)
            {
                var addParams = string.Join(' ', cmdParams.Skip(1));
                var argMatches = Regex.Matches(addParams, @"[^\s""']+|""[^""]+""");

                if (argMatches.Count != 5)
                {
                    Console.WriteLine(@"Invalid number of parameters! For help type ""help add"".");
                    return;
                }

                book.Name = argMatches[0].Value.Trim('"');
                book.Author = argMatches[1].Value.Trim('"');
                book.ISBN = argMatches[3].Value.Trim('"');

                try
                {
                    book.Year = int.Parse(argMatches[2].Value.Trim('"'));
                    book.Amount = int.Parse(argMatches[4].Value.Trim('"'));
                }
                catch
                {
                    Console.WriteLine(@"Invalid parameter value! For help type ""help add"".");
                    return;
                }
            }


            var url = new UriBuilder(_serv.Uri);
            url.Path = "add";

            var res = await _client.PostAsJsonAsync(url.Uri, book);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            Console.WriteLine("Book added!");
        }

        private static async Task FindCommand(string[] cmdParams)
        {
            if (cmdParams.Length == 1)
            {
                Console.WriteLine("No filters specified.");
                return;
            }

            var url = new UriBuilder(_serv.Uri);
            url.Path = $"find/{string.Join(";", cmdParams.Skip(1))}";

            var res = await _client.GetAsync(url.Uri);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (res.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.WriteLine(@"Invalid parameters! For help type ""help find"".");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            var books = await res.Content.ReadFromJsonAsync<List<Book>>();
            WriteBooks(books);
        }

        private static async Task ListCommand()
        {
            var url = new UriBuilder(_serv.Uri);
            url.Path = "list";

            var res = await _client.GetAsync(url.Uri);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            var books = await res.Content.ReadFromJsonAsync<List<Book>>();
            WriteBooks(books);
        }

        private static async Task InitCommand()
        {

            Console.WriteLine("WARNING: All existing data will be overwritten. Proceed? Y/N");
            if (Console.ReadKey(true).Key != ConsoleKey.Y)
                return;

            var url = new UriBuilder(_serv.Uri);
            url.Path = "init";

            var res = await _client.PostAsync(url.Uri, null);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            Console.WriteLine("Init OK!");
        }

        private static async Task BorrowCommand(string[] cmdParams)
        {
            if (cmdParams.Length == 1)
            {
                Console.WriteLine("No GUID specified.");
                return;
            }

            if (!Guid.TryParse(cmdParams[1], out var guid))
            {
                Console.WriteLine("Invalid GUID.");
                return;
            }

            var url = new UriBuilder(_serv.Uri);
            url.Path = $"borrow/{guid}";

            var res = await _client.PutAsync(url.Uri, null);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("No book with this GUID exists.");
                return;
            }
            if (res.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine("The book has no units available.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            Console.WriteLine($"Book borrowed successfully! Units remaining: {await res.Content.ReadAsStringAsync()}");
        }

        private static async Task ReturnCommand(string[] cmdParams)
        {
            if (cmdParams.Length == 1)
            {
                Console.WriteLine("No filters specified.");
                return;
            }

            if (!Guid.TryParse(cmdParams[1], out var guid))
            {
                Console.WriteLine("Invalid GUID.");
                return;
            }

            var url = new UriBuilder(_serv.Uri);
            url.Path = $"return/{guid}";

            var res = await _client.PutAsync(url.Uri, null);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access denied. Try logging in.");
                return;
            }
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("No book with this GUID exists.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine("Unknown error!");
                return;
            }

            Console.WriteLine($"Book returned successfully! Units remaining: {await res.Content.ReadAsStringAsync()}");

        }

        static void HelpCommand(string[] cmdParams)
        {
            if (cmdParams.Length == 1)
            {
                Console.WriteLine("""
Available commands:

help
login
init
list
add
find
borrow
return
exit

For more info on a command, type "help <command>".
""");
            }
            else
            {
                switch (cmdParams[1])
                {
                    case "exit":
                        Console.WriteLine(@"
exit
    - exit
");
                        break;

                    case "help":
                        Console.WriteLine(@"
help
    - list available commands
help <command>
    - display more info about a command
");
                        break;

                    case "login":
                        Console.WriteLine(@"
login <user> <pass>
    - logs into the system with the specified credentials
");
                        break;

                    case "init":
                        Console.WriteLine(@"
init
    - initializes db with testing books
");
                        break;

                    case "list":
                        Console.WriteLine(@"
list
    - return a list of all books
");
                        break;

                    case "add":
                        Console.WriteLine(@"
add
    - add new book interactively
add <name> <author> <year> <ISBN> <units-available>
    - add new book with given attributes
    - values containing spaces must be enclosed in ""double quotes""
");
                        break;

                    case "find":
                        Console.WriteLine(@"
find <query1> <query2> ...
    - look up books matching ALL given queries
    - a <query> must look as follows: <parameter>:<value>
    - <parameter> is one of: name auth year isbn
    - <value> is a string enclosed in ""double quotes"" (for name, auth, isbn) or a range (for year)
    - a range consists of an optional starting year and/or ending year, separated by a dash (-)
    - examples of valid ranges: 2000, 1999-2002, 2020-, -1950
");
                        break;

                    case "borrow":
                        Console.WriteLine(@"
borrow <GUID>
    - borrow the book with given GUID (decrements units available by 1)
");
                        break;

                    case "return":
                        Console.WriteLine(@"
return <GUID>
    - return the book with given GUID (increments units available by 1)
");
                        break;

                    default:
                        Console.WriteLine(@"Unknown command. For a list of commands type ""help"".");
                        break;
                }
            }
        }
    }
}
