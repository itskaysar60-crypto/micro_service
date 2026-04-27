using InventoryService.Application;
using InventoryService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Register all layers ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Controllers + Swagger ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

// Run on port 5002
app.Run();
