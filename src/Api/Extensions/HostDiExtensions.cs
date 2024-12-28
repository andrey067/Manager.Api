using System.Text.Json.Serialization;
using Api.Database;
using Api.Interfaces;
using Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Api.Extensions;

public static class HostDiExtensions
{
    public static IServiceCollection AddWebHostInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<SeedService>();

        services
            .AddEfCore(configuration);

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API v1", Version = "v1" });
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "My API v2", Version = "v2" });

                c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
            });

        services.AddScoped<ITokenService, TokenService>();

        // Adiciona o MediatR ao contêiner de serviços, registrando os serviços a partir do assembly que contém EfCoreDbContext
        services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<ManagerContext>());

        // Configura opções de serialização JSON para adicionar um conversor de enumeração para string
        services.Configure<JsonOptions>(opt =>
        {
            opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddValidatorsFromAssemblyContaining<IApiMarker>();

        return services;
    }

    public static void AddHostLogging(this WebApplicationBuilder builder)
    {
        // Configura o Serilog como o provedor de logging, lendo as configurações do arquivo de configuração do aplicativo
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
    }

    private static IServiceCollection AddEfCore(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");
        var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");

        //Postgres
        // services.AddDbContext<ManagerContext>(x => x
        //     .EnableSensitiveDataLogging()
        //     .UseNpgsql(postgresConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__MyMigrationsHistory", "manager"))
        //     .UseSnakeCaseNamingConvention()
        // );

        //SQlServer
        //services.AddDbContext<ManagerContext>(x => x
        //    .EnableSensitiveDataLogging()
        //    .UseSqlServer(sqlServerConnectionString,
        //        sqlServerOptions =>
        //            sqlServerOptions.MigrationsHistoryTable("__MyMigrationsHistory", "manager-vertical"))
        //    .UseSnakeCaseNamingConvention()
        //);


        //InMemory
        services.AddDbContext<ManagerContext>(x => x
            .UseInMemoryDatabase("manager-vertical")
        );

        return services;
    }
}