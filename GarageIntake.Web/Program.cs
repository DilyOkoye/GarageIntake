using GarageIntake.Web.Services;
using Microsoft.AspNetCore.DataProtection;
var builder = WebApplication.CreateBuilder(args);
// Local, stateless prototype: open forms need refreshing after a server restart.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddRazorPages();
var mode = builder.Configuration["Analysis:Mode"] ?? "Demo";
if (!new[] { "Demo", "OpenAI" }.Contains(mode, StringComparer.OrdinalIgnoreCase))
    throw new InvalidOperationException("Analysis:Mode must be Demo or OpenAI.");
builder.Services.AddSingleton<DemoIntakeAnalyzer>();
builder.Services.AddHttpClient<OpenAiIntakeAnalyzer>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IIntakeAnalyzer>(services =>
    mode.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
        ? services.GetRequiredService<OpenAiIntakeAnalyzer>()
        : services.GetRequiredService<DemoIntakeAnalyzer>());
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'; form-action 'self'; base-uri 'self'";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
