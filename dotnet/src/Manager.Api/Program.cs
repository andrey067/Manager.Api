using Manager.Api.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
