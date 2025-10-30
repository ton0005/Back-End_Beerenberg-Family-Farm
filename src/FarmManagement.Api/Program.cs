using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
// JWT token
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
// Application layer
using FarmManagement.Application.Services;
using FarmManagement.Application.Repositories;
using FarmManagement.Application.Security;
using FarmManagement.Application.Configuration;
// Core layer
using FarmManagement.Core.Entities.Identity;
// Infrastructure layer
using FarmManagement.Infrastructure.Security;
using FarmManagement.Infrastructure.Data;
using FarmManagement.Infrastructure.Data.Repositories;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text.Json.Serialization;
using FarmManagement.Api.Filters;
using FarmManagement.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// JWT options from appsettings
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var key = jwtSection["Key"];

// Services
builder.Services.AddControllers(options =>
{
    // Register global result filter to wrap responses
    options.Filters.Add(typeof(ApiResponseWrapperFilter));
}).AddJsonOptions(opts =>
{
    // Avoid circular reference 500s when serializing EF navigation properties
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// for Swagger (documentation)
builder.Services.AddEndpointsApiExplorer();
/*builder.Services.AddSwaggerGen((c) =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});
*/

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FarmManagementSystem API",
        Version = "v1"
    });

    // Ensure DateOnly/TimeOnly are described correctly
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    
    // Add JWT Bearer authentication to Swagger (Authorize button)
    var bearerDefinition = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\nExample: \"Bearer eyJhbGciOi...\""
    };

    c.AddSecurityDefinition("Bearer", bearerDefinition);

    // Reference the defined scheme by id in the security requirement so Swagger UI will prompt for it
    var bearerRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    };

    c.AddSecurityRequirement(bearerRequirement);
});
// include business logic scopes
builder.Services.AddScoped<IAuthUserRepository, AuthUserRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
// Allow services to access HttpContext (for correlation id)
builder.Services.AddHttpContextAccessor();
// Staff services
builder.Services.AddScoped<FarmManagement.Application.Repositories.IStaffRepository, FarmManagement.Infrastructure.Data.Repositories.StaffRepository>();
builder.Services.AddScoped<FarmManagement.Application.Services.StaffService, FarmManagement.Application.Services.StaffService>();
// Staff role repository
builder.Services.AddScoped<FarmManagement.Application.Repositories.IStaffRoleRepository, FarmManagement.Infrastructure.Data.Repositories.StaffRoleRepository>();
// Department repository (resolve name -> id)
builder.Services.AddScoped<FarmManagement.Application.Repositories.IDepartmentRepository, FarmManagement.Infrastructure.Data.Repositories.DepartmentRepository>();

// Shift repositories and service
builder.Services.AddScoped<FarmManagement.Application.Repositories.IShiftTypeRepository, FarmManagement.Infrastructure.Data.Repositories.ShiftTypeRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IShiftRepository, FarmManagement.Infrastructure.Data.Repositories.ShiftRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IShiftAssignmentRepository, FarmManagement.Infrastructure.Data.Repositories.ShiftAssignmentRepository>();
builder.Services.AddScoped<FarmManagement.Application.Services.IShiftService, FarmManagement.Application.Services.ShiftService>();

// Time entry repository & service
builder.Services.AddScoped<FarmManagement.Application.Repositories.ITimeEntryRepository, FarmManagement.Infrastructure.Data.Repositories.TimeEntryRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IExceptionRepository, FarmManagement.Infrastructure.Data.Repositories.ExceptionRepository>();
builder.Services.AddScoped<FarmManagement.Application.Services.ITimeEntryService, FarmManagement.Application.Services.TimeEntryService>();
// Audit repository
builder.Services.AddScoped<FarmManagement.Application.Repositories.IAuditRepository, FarmManagement.Infrastructure.Data.Repositories.AuditRepository>();
// EntryType repository (lookup by id)
builder.Services.AddScoped<FarmManagement.Application.Repositories.IEntryTypeRepository, FarmManagement.Infrastructure.Data.Repositories.EntryTypeRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IExceptionTypeRepository, FarmManagement.Infrastructure.Data.Repositories.ExceptionTypeRepository>();
// ShiftAssignment repository (for updating CompletedAt)
builder.Services.AddScoped<FarmManagement.Application.Repositories.IShiftAssignmentRepository, FarmManagement.Infrastructure.Data.Repositories.ShiftAssignmentRepository>();

// Payroll repositories and service
builder.Services.AddScoped<FarmManagement.Application.Repositories.IPayCalendarRepository, FarmManagement.Infrastructure.Data.Repositories.PayCalendarRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IPayrollRunRepository, FarmManagement.Infrastructure.Data.Repositories.PayrollRunRepository>();
builder.Services.AddScoped<FarmManagement.Application.Repositories.IPayRateRepository, FarmManagement.Infrastructure.Data.Repositories.PayRateRepository>();
builder.Services.AddScoped<FarmManagement.Application.Services.IPayrollService, FarmManagement.Application.Services.PayrollService>();

// Payroll auto-generation background service
builder.Services.AddHostedService<FarmManagement.Application.Services.PayrollAutoGenerationService>();

// Bind payroll configuration
builder.Services.Configure<FarmManagement.Application.Configuration.PayrollOptions>(builder.Configuration.GetSection("Payroll"));

// Provide a DB-backed provider that reads payroll options from the PayrollOptions table and falls back to configuration
builder.Services.AddScoped<FarmManagement.Application.Services.IPayrollOptionsProvider, FarmManagement.Infrastructure.Services.PayrollOptionsProvider>();

// Register HttpClient for EmailJS
builder.Services.AddHttpClient<IEmailService, EmailJSService>();

builder.Services.AddScoped<AuthService>();
// for Auth and hashing
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<AuthService>();
// include JWT service (ensure IStaffRoleRepository and UserManager are injected)
builder.Services.AddScoped<IJwtTokenService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var userManager = sp.GetService<UserManager<ApplicationUser>>();
    var staffRoleRepo = sp.GetService<FarmManagement.Application.Repositories.IStaffRoleRepository>();
    return new FarmManagement.Infrastructure.Security.JwtTokenService(config, userManager, staffRoleRepo);
});

// Identity (uses the same ApplicationDbContext so Identity tables live alongside app tables)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // Add this to fix password reset token provider

// Register custom claims factory to include staff roles in Identity claims
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaims>();
// frontend CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
            // add local vite url if testing locally
            "http://localhost:5173",    // local test
            "http://34.129.216.29/react-app/" // production
            )
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// Email and Password Reset services
//builder.Services.AddScoped<IEmailService, EmailJSService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Identity helper service (creates Identity users from application services)
// Construct explicitly via factory so required Identity services (UserManager/RoleManager)
// are resolved and passed to the implementation's constructor.
builder.Services.AddScoped<FarmManagement.Application.Services.IIdentityService>(sp =>
{
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var staffRepo = sp.GetRequiredService<FarmManagement.Application.Repositories.IStaffRepository>();
    return new FarmManagement.Infrastructure.Security.IdentityService(userManager, roleManager, staffRepo);
});

// SQL database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
            ClockSkew = TimeSpan.Zero,
            // Use the standard Role claim type for [Authorize(Roles="...")]
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();


// Swagger shown only in dev
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger(option =>
    {
        // This changes the path where the JSON is served
        option.RouteTemplate = "api/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        // Match the custom route template
        c.SwaggerEndpoint("/api/v1/swagger.json", "FarmManagementSystem API v1");
        // Serve Swagger UI at "/swagger"
        c.RoutePrefix = "dotnet-app"; // set to "" if you want it at root "/"
        
        c.DocumentTitle = "FarmManagementSystem API Documentation";
    });
//}
// Exception middleware should be one of the first middlewares so it can catch downstream errors
app.UseApiExceptionMiddleware();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
// Map controller routes so attribute-routed controllers are reachable
app.MapControllers();

    // Run identity seeder (create roles and admin user) - best effort; failures won't stop app
    try
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        IdentitySeedRun();

        async void IdentitySeedRun()
        {
            try
            {
                await FarmManagement.Infrastructure.Data.Seed.IdentitySeeder.SeedAsync(app.Services, builder.Configuration);
                logger.LogInformation("Identity seeding completed (roles/admin)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Identity seeding failed");
            }
                // Run staff data seeder (sample staff rows) - best effort
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.StaffSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("Staff data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Staff data seeding failed");
                }
                // Seed roles and staff-role mappings
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.RoleSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("Role data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Role data seeding failed");
                }
                // Seed default shift types (Morning/Afternoon/FullDay/Custom)
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.ShiftTypeSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("ShiftType data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ShiftType data seeding failed");
                }
                // Seed EntryTypes (CLOCK_IN, CLOCK_OUT, BREAK_START, BREAK_END)
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.EntryTypeSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("EntryType data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "EntryType data seeding failed");
                }

                // Seed ExceptionTypes (MISSING_CLOCK_IN, MISSING_CLOCK_OUT, ADJUST_REQUEST, INCORRECT_STATION, OTHER)
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.ExceptionTypeSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("ExceptionType data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ExceptionType data seeding failed");
                }

                // Seed sample TimeStations
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.TimeStationSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("TimeStation data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "TimeStation data seeding failed");
                }
                // Seed pay rates from JSON seed (so rates aren't hard-coded)
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.PayRateSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("PayRate data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PayRate data seeding failed");
                }
                // Seed payroll options from JSON seed (pay frequency, thresholds, paid break minutes)
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.PayrollOptionsSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("PayrollOptions data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PayrollOptions data seeding failed");
                }

                // Seed a test staff and time entries for payroll testing
                try
                {
                    await FarmManagement.Infrastructure.Data.Seed.TestPayrollDataSeeder.SeedAsync(app.Services, builder.Configuration);
                    logger.LogInformation("Test payroll data seeding completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Test payroll data seeding failed");
                }
        }
    }
    catch
    {
        // swallow - logging above handles it
    }

    app.Run();