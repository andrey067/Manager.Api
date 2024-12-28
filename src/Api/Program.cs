using Api;
using Api.Database;
using Api.Extensions;
using Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddHostLogging();
builder.Services.AddWebHostInfrastructure(builder.Configuration);
builder.Services.RegisterEndpointsFromAssemblyContaining<IApiMarker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    //var dbContext = scope.ServiceProvider.GetRequiredService<ManagerContext>();
    //var connection = dbContext.Database.GetConnectionString();
    //var migrations = await dbContext.Database.GetPendingMigrationsAsync();

    //if (!migrations.Any())
    //    await dbContext.Database.MigrateAsync();

    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seedService.SeedDataAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapEndpoints();

app.Run();