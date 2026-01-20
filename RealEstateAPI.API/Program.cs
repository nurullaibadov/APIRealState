using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using RealEstateAPI.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    );

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(); 
        options.EnableDetailedErrors(); 
    }
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // JSON serialization ayarları
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase; // camelCase kullan
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Her domain'den istek kabul et
              .AllowAnyMethod()   // GET, POST, PUT, DELETE hepsi
              .AllowAnyHeader();  // Her header kabul et
    });

    // Production için daha güvenli CORS policy:
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://www.yourfrontend.com", "https://yourfrontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Cookie'lere izin ver
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Real Estate API",
        Version = "v1",
        Description = "Real Estate Management API with Onion Architecture",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // JWT Authentication için Swagger'a ekle
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");

app.MapControllers();

app.UseAuthorization();


app.UseStaticFiles();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Pending migration'ları uygula
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Applying pending migrations...");
            context.Database.Migrate();
            Console.WriteLine("Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("No pending migrations.");
        }

        // Database var mı kontrol et, yoksa oluştur
        context.Database.EnsureCreated();

        Console.WriteLine("Database connection successful!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");

        // Hata durumunda uygulamayı durdur
        throw;
    }
}


app.Run();
