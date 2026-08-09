using Microsoft.EntityFrameworkCore;
using SoodalLife.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<SoodalLifeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SoodalLife")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
