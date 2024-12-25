using Api;
using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.AddHostLogging();
builder.Services.AddWebHostInfrastructure(builder.Configuration);
builder.Services.RegisterEndpointsFromAssemblyContaining<IApiMarker>();


var app = builder.Build();
app.UseHttpsRedirection();

// using (var scope = app.Services.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<EfCoreDbContext>();
//     await dbContext.Database.MigrateAsync();
//
//     var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
//     await seedService.SeedDataAsync();
// }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapEndpoints();

app.Run();

