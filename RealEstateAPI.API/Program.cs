using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealEstateAPI.Application.Mappings;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using RealEstateAPI.Infrastructure.Helpers;
using RealEstateAPI.Infrastructure.Repositories;
using RealEstateAPI.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// JWT SETTINGS – TEK YERDEN OKU (ÇOK ÖNEMLİ)
// ============================================================

var jwtSection = builder.Configuration.GetSection("JwtSettings");

if (!jwtSection.Exists())
    throw new Exception("JwtSettings section not found in configuration.");

var jwtSecret = jwtSection["SecretKey"];
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtExpiration = jwtSection["ExpirationInMinutes"];

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new Exception("JwtSettings:SecretKey is missing.");

if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new Exception("JwtSettings:Issuer is missing.");

if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new Exception("JwtSettings:Audience is missing.");

if (string.IsNullOrWhiteSpace(jwtExpiration))
    throw new Exception("JwtSettings:ExpirationInMinutes is missing.");

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");

    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.MigrationsAssembly("RealEstateAPI.Infrastructure");
            mySqlOptions.CommandTimeout(60);
            mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ============================================================
// DEPENDENCY INJECTION
// ============================================================

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton<JwtHelper>(_ =>
    new JwtHelper(
        secretKey: jwtSecret!,
        issuer: jwtIssuer!,
        audience: jwtAudience!,
        expirationInMinutes: int.Parse(jwtExpiration!)
    )
);

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IFileService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var uploadSettings = config.GetSection("FileUploadSettings");

    return new FileService(
        uploadPath: uploadSettings["PropertyImagesPath"]!,
        maxFileSizeInMB: long.Parse(uploadSettings["MaxFileSizeInMB"]!),
        allowedExtensions: uploadSettings.GetSection("AllowedExtensions").Get<string[]>()!
    );
});

builder.Services.AddScoped<IEmailService>(provider =>
{
    var smtp = provider.GetRequiredService<IConfiguration>()
                       .GetSection("SmtpSettings");

    return new EmailService(
        host: smtp["Host"]!,
        port: int.Parse(smtp["Port"]!),
        username: smtp["Username"]!,
        password: smtp["Password"]!,
        fromEmail: smtp["FromEmail"]!,
        fromName: smtp["FromName"]!,
        enableSsl: bool.Parse(smtp["EnableSsl"]!)
    );
});

// ============================================================
// JWT AUTHENTICATION (DÜZELTİLDİ)
// ============================================================

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret!)
        ),

        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName); // 🔥 ÇÖZÜM

    options.SwaggerDoc("v1", new()
    {
        Title = "Real Estate API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// PIPELINE
// ============================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // 👉 /swagger
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================================
// DATABASE MIGRATION
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
