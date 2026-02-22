using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using AvailabilityService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AvailabilityOptions>(builder.Configuration.GetSection("Availability"));
builder.Services.AddSingleton<InMemoryAvailabilityStore>();
builder.Services.AddSingleton<IAvailabilitySlotService, AvailabilitySlotService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

AvailabilitySeedData.Seed(app.Services);

app.Run();

