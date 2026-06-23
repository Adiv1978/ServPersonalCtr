var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:8080");
// Add services to the container.
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.DBGenericManager>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.FileSoporteHelper>();
builder.Services.AddHttpClient<ServPersonalCtr.Managers.L00.GptHelper>();
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
builder.Services.AddCors(options =>
{
    /*options.AddPolicy("PermitirAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            // Intentar parsear el origen como una URI válida
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                // Validar si el host (la IP) comienza con "10.8."
                return uri.Host.StartsWith("10.8.");
            }
            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });*/
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // El origen de tu Angular
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
var app = builder.Build();
app.UseCors("AllowAngularDev");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
