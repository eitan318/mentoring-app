using System.Text;
using MentoringApp.Api.Endpoints;
using MentoringApp.Api.Helpers;
using MentoringApp.Data.DI;
using MentoringApp.Service.DI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Data + service layers
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddTransient<DummyDataSeeder>();

// Configure JSON to emit $type discriminators for polymorphic UserModel
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
});

// JWT settings
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrSupervisor", policy => policy.RequireRole("Admin", "Supervisor"));
});

// CORS
var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? string.Empty;
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Swagger with Bearer token support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MentoringApp API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebClient");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPairEndpoints();
app.MapMatchingEndpoints();
app.MapIssueEndpoints();
app.MapReviewEndpoints();
app.MapReferenceEndpoints();
app.MapSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapDevEndpoints();

app.Run();
