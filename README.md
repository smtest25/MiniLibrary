# MiniLibrary
Super simple .NET library book API and console-based client that consumes it.
## Notes
- To run, either set both `MiniLibrary` and `MiniLibraryClient` projects as startup in Visual Studio, or `dotnet run` these projects from their directories.
- Data is persisted in an SQLite database file named `books.sqlite`, which by default is located next to the `MiniLibrary` DLL, and is created if it does not exist.
- The API runs by default at `https://localhost:9999`, which is where the client expects it to be. Configuration may be adjusted if necessary.
- API documentation is available at `/scalar`.
- Most API endpoints require JWT bearer authentication. The `/login` endpoint provides a token when supplied with the correct credentials (default hardcoded values are `user` and `pass` as username and password respectively). To authenticate from the client app, use the `login` command.
- No generative AI or LLM of any kind was used in the making of this software.
