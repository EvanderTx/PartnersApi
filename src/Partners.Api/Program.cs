using System.Text.Json;
using System.Text.Json.Serialization;
using Partners.Api.ErrorHandling;
using Partners.Application;
using Partners.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PartnersDb")
    ?? throw new InvalidOperationException("Connection string 'PartnersDb' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Exposes the Program class to the integration tests (WebApplicationFactory<Program>).
public partial class Program;
