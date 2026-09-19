using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PicPay.Api.Endpoints;
using PicPay.Api.Infrastructure;
using PicPay.Application;
using PicPay.Application.Abstractions;
using PicPay.Infrastructure;
using PicPay.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue<bool>("Database:Seed"))
        await DevelopmentSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapTransferEndpoints();

app.Run();

public partial class Program;
