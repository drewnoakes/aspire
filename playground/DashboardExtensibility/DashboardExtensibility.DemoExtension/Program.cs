var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseFileServer();
//app.MapGet("/", () => "Hello World!");

app.Run();
