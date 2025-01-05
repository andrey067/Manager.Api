using Api;
using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddHostLogging();
builder.Services.AddWebHostInfrastructure(builder.Configuration);
builder.Services.RegisterEndpointsFromAssemblyContaining<IApiMarker>();

var app = builder.Build();

//     await dbContext.Database.MigrateAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapEndpoints();

app.Run();