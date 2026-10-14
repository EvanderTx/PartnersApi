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

// Enums travel as "CUSTOMER", "SUPPLIER" or "BOTH"; numeric values are rejected.
var enumConverter = new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false);

builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(enumConverter));

// The OpenAPI document reads these options, so the schema shows the same enum values as the API.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(enumConverter));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Partners API v1"));
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Exposes the Program class to the integration tests (WebApplicationFactory<Program>).
public partial class Program;
