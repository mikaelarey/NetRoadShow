using API_2.BackgroundServices;
using API_2.Hubs;
using API_2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddSingleton<ElevatorSystemService>();
builder.Services.AddSingleton<ElevatorSystemStatusUpdateService>();

builder.Services.AddSingleton<ElevatorRequestBackgroundService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ElevatorRequestBackgroundService>());

builder.Services.AddSingleton<ElevatorSimulationBackgroundService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ElevatorSimulationBackgroundService>());

builder.Services.AddSingleton<ClientUpdateBackgroundService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ClientUpdateBackgroundService>());


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularClient");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map your SignalR Hub
app.MapHub<ElevatorHub>("/elevatorhub"); // Clients will connect to "/elevatorhub"

app.Run();
