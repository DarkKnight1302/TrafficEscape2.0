using NewHorizonLib;
using TrafficEscape2._0.ApiClients;
using TrafficEscape2._0.Repositories;
using TrafficEscape2._0.Services;
using TrafficEscape2._0.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITrafficComputeService, TrafficComputeService>();
builder.Services.AddSingleton<IRouteSlotRepository, RouteSlotRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IGoogleTrafficApiClient, GoogleTrafficApiClient>();
builder.Services.AddSingleton<IUserService, UserService>();

Registration.InitializeServices(builder.Services, "TrafficEscape", 500);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
