var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello from backend");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();