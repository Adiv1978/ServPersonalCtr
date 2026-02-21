var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ServPersonalCtr.Managers.L00.DBGenericManager>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngSeguridad>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngPersonal>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L10.mngLicencias>();
builder.Services.AddScoped<ServPersonalCtr.Managers.L20.mngLicencias>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
