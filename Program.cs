using System;
using System.Collections.Generic;
using ExpressionDb;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;

// SQLite connection string
var ConnectionStr = $"Data Source={nameof(MyExpressionContext)}.db";

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
    var options = new DbContextOptionsBuilder<MyExpressionContext>()
        .UseSqlite(ConnectionStr);
    using var context = new MyExpressionContext(options.Options);
    Console.WriteLine($"{nameof(context.Types)} = {context.Types.Count()}");
    Console.WriteLine($"{nameof(context.Methods)} = {context.Methods.Count()}");
    Console.WriteLine($"{nameof(context.Properties)} = {context.Properties.Count()}");
    Console.WriteLine($"{nameof(context.Parameters)} = {context.Parameters.Count()}");
}

/// <summary>
/// Main query.
/// </summary>
void ShowTypes()
{
    var options = new DbContextOptionsBuilder<MyExpressionContext>()
        .UseSqlite(ConnectionStr);
    //    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    
    using var context = new MyExpressionContext(options.Options);

    var baseQuery = context.Types;
    
    var query = baseQuery.Take(3).Select(
        t => new
        {
            t.Name,
            Props = t.TypeProperties.OrderBy(p => p.Name).Select(p => new
            {
                p.Name,
                Type = p.PropertyType.Name
            }),
            Methods = t.TypeMethods.OrderBy(m => m.Name).Select(m => new
            {
                m.Name,
                ReturnType = m.ReturnType.Name,
                Parameters = m.MethodParameters.Select(mp => new
                {
                    mp.Name,
                    Type = mp.ParameterType.Name
                })
            })
        });

    //Console.WriteLine(query.ToQueryString());
    
    foreach (var result in query)
    {
        Console.WriteLine(result.Name);
        foreach (var prop in result.Props)
        {
            Console.WriteLine($"\t{prop.Type}: {prop.Name}");
        }
        Console.WriteLine("\t---");
        foreach (var method in result.Methods)
        {
            Console.Write($"\t{method.ReturnType} : {method.Name} (");
            bool first = true;
            foreach (var parm in method.Parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Console.Write(", ");
                }
                Console.Write($"{parm.Type}: {parm.Name}");
            }
            Console.WriteLine(")");
        }
        Console.WriteLine("---");
    }
}

/// <summary>
/// First time, this will create the database based on <see cref="Expression"/> derived types.
/// </summary>
void SetupDatabase()
{
    var options = new DbContextOptionsBuilder<MyExpressionContext>()
        .UseSqlite(ConnectionStr);

    using var context = new MyExpressionContext(options.Options);

    // already there
    if (!context.Database.EnsureCreated())
    {
        return;
    }

    var types = typeof(NewExpression).Assembly.GetTypes()
        .Where(t => typeof(Expression).IsAssignableFrom(t));

    var myTypes = new List<MyType>();

    // keeps track of types already added
    MyType GetOrSetType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var typeMatch = myTypes.FirstOrDefault(mt => mt.Name == name);
        if (typeMatch != null)
        {
            return typeMatch;
        }

        var newType = new MyType { Name = name }; ;
        myTypes.Add(newType);
        return newType;
    }

    foreach (var expr in types)
    {

        var myType = GetOrSetType(expr.Name);

        foreach (var prop in expr.GetProperties().Where(p => !string.IsNullOrWhiteSpace(p.PropertyType.Name)))
        {
            var myProp = new MyProperty
            {
                Name = prop.Name,
                ParentType = myType
            };
            var typeName = prop.PropertyType.Name;
            myProp.PropertyType = GetOrSetType(typeName);
            myType.TypeProperties.Add(myProp);
            context.Properties.Add(myProp);
        }

        foreach (var method in expr.GetMethods())
        {
            var myMethod = new MyMethod
            {
                Name = method.Name,
                ReturnType = GetOrSetType(method.ReturnType.Name),
                ParentType = myType
            };

            foreach (var parameter in method.GetParameters())
            {
                var myParameter = new MyParameter
                {
                    Name = parameter.Name,
                    ParameterType = GetOrSetType(parameter.ParameterType.Name),
                    ParentMethod = myMethod
                };
                myMethod.MethodParameters.Add(myParameter);
                context.Parameters.Add(myParameter);
            }

            myType.TypeMethods.Add(myMethod);
            context.Methods.Add(myMethod);
        }
        context.Types.Add(myType);
    }

    context.SaveChanges();
}

namespace ExpressionDb
{
    /// <summary>
    /// Represents a property on a type.
    /// </summary>
    public class MyProperty
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int PropertyTypeId { get; set; }
        public virtual MyType PropertyType { get; set; }

        public int ParentTypeId { get; set; }
        public virtual MyType ParentType { get; set; }
    }

    /// <summary>
    /// Represents a parameter on a method.
    /// </summary>
    public class MyParameter
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int ParameterTypeId { get; set; }
        public virtual MyType ParameterType { get; set; }

        public int ParentMethodId { get; set; }
        public virtual MyMethod ParentMethod { get; set; }
    }

    /// <summary>
    /// Represents a method on a type.
    /// </summary>
    public class MyMethod
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int ReturnTypeId { get; set; }
        public virtual MyType ReturnType { get; set; }

        public int ParentTypeId { get; set; }
        public virtual MyType ParentType { get; set; }

        public virtual ICollection<MyParameter> MethodParameters { get; set; }
            = new List<MyParameter>();
    }

    /// <summary>
    /// Represents a type.
    /// </summary>
    public class MyType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Has many methods.
        /// </summary>
        public virtual ICollection<MyMethod> TypeMethods { get; set; }
            = new List<MyMethod>();

        /// <summary>
        /// Has many properties.
        /// </summary>
        public virtual ICollection<MyProperty> TypeProperties { get; set; }
            = new List<MyProperty>();
    }

    /// <summary>
    /// The data context.
    /// </summary>
    public class MyExpressionContext : DbContext
    {
        public MyExpressionContext() { }
        public MyExpressionContext(
            DbContextOptions<MyExpressionContext> options) 
            : base(options) { }

        /// <summary>
        /// Interact with options here.
        /// </summary>
        /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/>.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.AddInterceptors(new LimitResultsCommandInterceptor());
            
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// Define the model.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyType>()
                .HasMany(mt => mt.TypeMethods)
                .WithOne(m => m.ParentType);

            modelBuilder.Entity<MyType>()
                .HasMany(mt => mt.TypeProperties)
                .WithOne(p => p.ParentType);

            modelBuilder.Entity<MyMethod>()
                .HasMany(m => m.MethodParameters)
                .WithOne(p => p.ParentMethod);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<MyType> Types { get; set; }
        public DbSet<MyMethod> Methods { get; set; }
        public DbSet<MyProperty> Properties { get; set; }
        public DbSet<MyParameter> Parameters { get; set; }
    }

    /// <summary>
    /// Intercepts SELECT and adds LIMIT 5.
    /// </summary>
    public class LimitResultsCommandInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ManipulateCommand(command);
            return result;
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ManipulateCommand(command);
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }
        private static void ManipulateCommand(DbCommand command)
        {
            if (command.CommandText.Contains("SELECT", StringComparison.Ordinal))
            {
                command.CommandText += " LIMIT 5";
            }
        }
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

