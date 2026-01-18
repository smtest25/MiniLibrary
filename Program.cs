using MiniLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoginRequest = MiniLibrary.LoginRequest;

var jwtKeyHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("minilibrary_key"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters

    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "minilibrary",
        ValidAudience = "minilibrary",
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyHash)

    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<BookDb>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BookDb>().Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapGet("/health", [AllowAnonymous] () => 1);

app.MapPost("/login", [AllowAnonymous] (LoginRequest req) =>
{
    if (req.User != "user" || req.Pass != "pass")
        return Results.Unauthorized();

    var creds = new SigningCredentials(new SymmetricSecurityKey(jwtKeyHash), SecurityAlgorithms.HmacSha256Signature);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, req.User),
    };

    var jwt = new JwtSecurityToken(
        issuer: "minilibrary",
        audience: "minilibrary",
        claims: claims,
        expires: DateTime.Now.AddMinutes(10),
        signingCredentials: creds);

    return Results.Ok(
        new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            Duration = 60 * 10
        });
});

app.MapPost("/init", [Authorize] async (BookDb db) =>
{
    db.Books.RemoveRange(db.Books);

    db.Books.Add(new Book
    {
        Guid = Guid.NewGuid(),
        Name = "The Winds of Winter",
        Year = 2050,
        Author = "Martin, George Raymond Richard",
        ISBN = "978-0553801477",
        Amount = 10
    });

    db.Books.Add(new Book
    {
        Guid = Guid.NewGuid(),
        Name = "A Dream of Spring",
        Year = 2051,
        Author = "Sanderson, Brandon",
        ISBN = "978-0553801478",
        Amount = 10
    });

    db.Books.Add(new Book
    {
        Guid = Guid.NewGuid(),
        Name = "A Dream of Summer",
        Year = 2052,
        Author = "Sanderson, Brandon",
        ISBN = "978-0553801479",
        Amount = 10
    });

    await db.SaveChangesAsync();
    return Results.Created();
});

app.MapPost("add", [Authorize] async (Book book, BookDb db) =>
{
    book.Guid = Guid.NewGuid();
    db.Books.Add(book);
    await db.SaveChangesAsync();

    return Results.Created();
});

app.MapGet("find/{filters}", [Authorize] async (string filters, BookDb db) =>
{
    var foundBooks = db.Books.Where(b => true);

    foreach (var filter in filters.Split(';'))
    {
        if (filter.IndexOf(':') < 0)
            return Results.BadRequest();
        var param = filter.Split(':').First();
        if (string.IsNullOrEmpty(param))
            return Results.BadRequest();
        var value = string.Join("", filter.Split(':').Skip(1)).Trim('"');
        if (string.IsNullOrEmpty(value))
            return Results.BadRequest();

        try
        {
            switch (param)
            {
                case "name":
                    foundBooks = foundBooks.Where(b => b.Name!.ToLower().Contains(value.ToLower()));
                    break;
                case "auth":
                    foundBooks = foundBooks.Where(b => b.Author!.ToLower().Contains(value.ToLower()));
                    break;
                case "isbn":
                    foundBooks = foundBooks.Where(b => b.ISBN!.ToLower().Contains(value.ToLower()));
                    break;
                case "year":
                    try
                    {
                        var rangeSplit = value.Split('-');

                        if (rangeSplit.Length == 1)
                        {
                            foundBooks = foundBooks.Where(b => b.Year == int.Parse(rangeSplit[0]));
                        }

                        if (rangeSplit.Length == 2)
                        {
                            var yearFrom = rangeSplit[0];
                            if (string.IsNullOrEmpty(yearFrom))
                                yearFrom = int.MinValue.ToString();
                            var yearTo = rangeSplit[1];
                            if (string.IsNullOrEmpty(yearTo))
                                yearTo = int.MaxValue.ToString();

                            foundBooks = foundBooks.Where(b => b.Year >= int.Parse(yearFrom) && b.Year <= int.Parse(yearTo));
                        }
                    }
                    catch
                    {
                        return Results.BadRequest();
                    }
                    break;
                default:
                    return Results.BadRequest();
            }
        }
        catch
        {
            return Results.InternalServerError();
        }
    }
    return Results.Ok(await foundBooks.ToListAsync());
});

app.MapGet("list", [Authorize] async (BookDb db) =>
{
    return await db.Books.ToListAsync();
});

app.MapPut("borrow/{guid}", [Authorize] async (Guid guid, BookDb db) =>
{
    var book = await db.Books.FirstOrDefaultAsync(b => b.Guid == guid);

    if (book is null)
        return Results.NotFound();

    if (book.Amount == 0)
        return Results.NoContent();

    book.Amount--;

    await db.SaveChangesAsync();

    return Results.Ok(book.Amount);
});

app.MapPut("return/{guid}", [Authorize] async (Guid guid, BookDb db) =>
{
    var book = await db.Books.FirstOrDefaultAsync(b => b.Guid == guid);

    if (book is null)
        return Results.NotFound();

    book.Amount++;

    await db.SaveChangesAsync();

    return Results.Ok(book.Amount);
});

app.Run();

