using Microsoft.EntityFrameworkCore;

namespace MiniLibrary
{
    class BookDb(DbContextOptions<BookDb> options) : DbContext(options)
    {
        public DbSet<Book> Books => Set<Book>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($@"Data Source={AppDomain.CurrentDomain.BaseDirectory}\books.sqlite");
        }
    }
}
