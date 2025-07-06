using NewHorizonLib;
using Quartz;
using TrafficEscape2._0.ApiClients;
using TrafficEscape2._0.Cron;
using TrafficEscape2._0.Handlers;
using TrafficEscape2._0.Repositories;
using TrafficEscape2._0.Services;
using TrafficEscape2._0.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AddAuthHeaderParameter>();
});
builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigin",
                      builder =>
                      {
                          builder.WithOrigins("https://localhost:7174", "https://ambitious-sand-0822ae200.2.azurestaticapps.net", "https://trafficescape.in")
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});
builder.Services.AddSingleton<ITrafficComputeService, TrafficComputeService>();
builder.Services.AddSingleton<IRouteSlotRepository, RouteSlotRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IGoogleTrafficApiClient, GoogleTrafficApiClient>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddSingleton<ILoginHandler, LoginHandler>();
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
builder.Services.AddSingleton<ITrafficDataHandler, TrafficDataHandler>();

Registration.InitializeServices(builder.Services, "TrafficEscape", 500);
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("TrafficAnalysisJob");

    q.AddJob<TrafficAnalysisJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
                        .ForJob(jobKey)
                        .WithIdentity("TrafficAnalysisJob-trigger")
                        .WithCronSchedule("0 */10 * * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
var app = builder.Build();

app.UseCors("AllowSpecificOrigin");
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
