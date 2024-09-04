var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    // Allow to be hosted in an iframe
    context.Response.Headers.Append("Content-Security-Policy", "frame-src *;");
    //context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors *;");
    await next();
});

app.UseFileServer();
//app.MapGet("/", () => "Hello World!");

app.Run();