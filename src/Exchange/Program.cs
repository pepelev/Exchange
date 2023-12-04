var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(
    options =>
    {
        options.Limits.MaxRequestBodySize = 50L * 1024 * 1024 * 1024;
        options.Limits.MaxRequestBufferSize = 128 * 1024;
    }
);

builder.Services.AddControllers();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();