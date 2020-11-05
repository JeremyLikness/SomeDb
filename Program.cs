using System;
using SomeDb;
using Microsoft.EntityFrameworkCore;
using System.Linq;

// SQLite connection string
var ConnectionStr = $"Data Source={nameof(ADbContext)}.db";

Console.WriteLine($"{new Unicorn()}{Environment.NewLine}Preparing database...");

SetupDatabase();

Console.WriteLine($"{Environment.NewLine}Ready. Database. One. Hit ENTER to continue.");
Console.ReadLine();

ShowCounts();

ShowTypes();

/// <summary>
/// Shows counts of the tables that were created.
/// </summary>
void ShowCounts()
{
    var options = new DbContextOptionsBuilder<ADbContext>()
        .UseSqlite(ConnectionStr);
    using var context = new ADbContext(options.Options);
    Console.WriteLine($"{nameof(context.SomeThings)} = {context.SomeThings.Count()}");    
}

/// <summary>
/// Main query.
/// </summary>
void ShowTypes()
{
    var options = new DbContextOptionsBuilder<ADbContext>()
        .UseSqlite(ConnectionStr);
        //.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    
    using var context = new ADbContext(options.Options);

    var baseQuery = context.SomeThings;
    
    var query = baseQuery
        .Where(thing => thing.IsActive &&
            thing.Created > DateTime.Now.AddDays(-7))
        .Take(5).Select(
        t => new
        {
            t.Name,
            t.Created
        });

    //Console.WriteLine(query.ToQueryString());
        
    foreach (var result in query)
    {
        Console.WriteLine($"{result.Name} was created on {result.Created}");
        Console.WriteLine("---");
    }
}

/// <summary>
/// First time, this will create the database based on <see cref="Expression"/> derived types.
/// </summary>
void SetupDatabase()
{
    var options = new DbContextOptionsBuilder<ADbContext>()
        .UseSqlite(ConnectionStr);

    using var context = new ADbContext(options.Options);

    // already there
    if (!context.Database.EnsureCreated())
    {
        return;
    }

    var random = new Random();

    for (var idx = 0; idx < 10000; idx++)
    {
        var something = new Something
        {
            IsActive = random.NextDouble() < 0.7,
            Name = $"Thing {idx}",
            Created = DateTime.Now.AddHours(-1 * 24 * 7 * 4 * random.NextDouble())
        };
        context.SomeThings.Add(something);
    }

    context.SaveChanges();
}

namespace SomeDb
{
    /// <summary>
    /// I always wanted to demo something.
    /// </summary>
    public class Something
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
    }
    
    /// <summary>
    /// I wanted to show a data context.
    /// </summary>
    public class ADbContext : DbContext
    {
        public ADbContext() { }
        public ADbContext(
            DbContextOptions<ADbContext> options) 
            : base(options) { }

        public DbSet<Something> SomeThings { get; set; }
    }

    /// <summary>
    /// The EF magic unicorn.
    /// </summary>
    public class Unicorn
    {
        const int CHAR = 0x100;
        const int SEQ = 0x10000;
        const int CHARMASK = CHAR - 1;

        public override string ToString() =>
            new string(new[]
            {
                0x02015, 0x15F2F, 0x0005C, 0x05F02, 0x00001, 0x0200F, 0x02D03, 
                0x03D02, 0x0002F, 0x02004, 0x05C02, 0x00001, 0x02009, 0x05F03, 
                0x02002, 0x05F03, 0x02003, 0x17C2E, 0x02004, 0x15C7C, 0x15C01, 
                0x02008, 0x17C20, 0x05F02, 0x07C02, 0x00020, 0x05F02, 0x0007C, 
                0x02002, 0x0007C, 0x02002, 0x00029, 0x02003, 0x05C03, 0x00001, 
                0x02008, 0x17C20, 0x15F7C, 0x1207C, 0x1205F, 0x0007C, 0x02003, 
                0x15C5F, 0x12F20, 0x0007C, 0x02002, 0x02F02, 0x0007C, 0x05C02, 
                0x00001, 0x02008, 0x0007C, 0x05F03, 0x07C02, 0x15F7C, 0x02007, 
                0x0002F, 0x02003, 0x05C03, 0x0002F, 0x05C02
            }.SelectMany(
                    i => i < CHAR ?
                        new[] { (char)i } :
                        i < SEQ ?
                            new string((char)(i >> 8), i & CHARMASK).ToCharArray() :
                            new[] { (char)((i - SEQ) >> 8), (char)((i - SEQ) & CHARMASK) }
                            ).ToArray())
            .Replace(new string(new[] { (char)0x01 }), Environment.NewLine);        
    }
}