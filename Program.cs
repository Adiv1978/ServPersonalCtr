var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:8080");
// Add services to the container.
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.DBGenericManager>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngLicencias>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngLicencias>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirAngular", policy =>
    {
        // Aquí pones la URL exacta de tu frontend (sin la barra / al final)
        policy.WithOrigins("http://192.168.5.1")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();
app.UseCors("PermitirAngular");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
