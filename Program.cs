using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:8080");
// Add services to the container.
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.DBGenericManager>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.FileSoporteHelper>();
builder.Services.AddHttpClient<ServPersonalCtr.Managers.L00.GeminiHelper>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.MngFileSoporte>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.MngGpt>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngLicencias>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngLicencias>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.MngGpt>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin))
                return false;

            if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                return true;

            if (builder.Environment.IsDevelopment() &&
                string.Equals(origin, "http://localhost:4200", StringComparison.OrdinalIgnoreCase))
                return true;

            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return uri.Host.StartsWith("10.8.0.");

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("PermitirAngular");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
