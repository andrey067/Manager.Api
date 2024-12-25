using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Database;


// Esta implementação cria uma instância de ManagerContext para ser usada em tempo de design.
// Ela configura o DbContextOptionsBuilder para usar o SQL Server com uma convenção de nomenclatura em snake_case.
public class DesignTimeDbContextFactory: IDesignTimeDbContextFactory<ManagerContext>
{
    public ManagerContext CreateDbContext(string[] args)
    {
        //Postgres
        // var optionsBuilder = new DbContextOptionsBuilder<ManagerContext>()
        //     .UseNpgsql(npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__MyMigrationsHistory", "manager"))
        //     .UseSnakeCaseNamingConvention();
        
        //SqlServer
        var optionsBuilder = new DbContextOptionsBuilder<ManagerContext>()
            .UseSqlServer(options => options.MigrationsHistoryTable("__MyMigrationsHistory", "manager"))
            .UseSnakeCaseNamingConvention();

        return new ManagerContext(optionsBuilder.Options);
    }
}