using Internship.Api.Security;
using Internship.API.Security;
using Internship.Application.Interfaces;
using Internship.Application.Services;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Internship.Infrastructure.Data;
using Internship.Infrastructure.Repositories;
using Internship.Infrastructure.Services;
using Internship.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SendGrid;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Internship API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});


// Database Configuration with validation
var connectionString = builder.Configuration.GetConnectionString("DBConnection")
    ?? throw new InvalidOperationException("Database connection string 'DBConnection' is not configured.");

builder.Services.AddDbContext<RefundDisputeContext>(options =>
    options.UseSqlServer(connectionString));

// Identity Configuration with .NET 9 improvements
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<RefundDisputeContext>()
.AddDefaultTokenProviders();

// JWT Configuration with options pattern
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Service Registration - Existing Authentication services
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// Register your authorization handler
builder.Services.AddScoped<IAuthorizationHandler, AdminOrAssignedUserHandler>();

// Register repositories
builder.Services.AddScoped<ITransactionLogRepository, TransactionLogRepository>();
builder.Services.AddScoped<IRefundRequestLogRepository, RefundRequestLogRepository>();
builder.Services.AddScoped<IDisputeLogRepository, DisputeLogRepository>();

// Register audit service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Service Registration - Payment System repositories and services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IRefundRequestService, RefundRequestService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["SendGrid:ApiKey"];

    if (string.IsNullOrEmpty(apiKey))
        throw new InvalidOperationException("SendGrid:ApiKey is missing in configuration");

    if (!apiKey.StartsWith("SG."))
        throw new InvalidOperationException("SendGrid API key must start with 'SG.'");

    return new SendGridClient(apiKey);
});

// Authorization Policy Configuration
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrAssignedUser", policy =>
    {
        policy.AddRequirements(new AdminOrAssignedUserRequirement());
    });
});

// JWT Authentication with validation
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");

ArgumentException.ThrowIfNullOrEmpty(jwtSettings.Secret, nameof(jwtSettings.Secret));

builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// Uploaded attachments contain user data and must not be served anonymously.

// SPA static files configuration

app.UseRouting();          // 1. Enable routing FIRST
app.UseAuthentication();   // 3. Auth AFTER routing
app.UseAuthorization();    // 4. Authorization AFTER auth

// Map API controllers
app.MapControllers();

// Database initialization with improved error handling
try
{
    await InitializeDatabaseAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while initializing the database");

    if (app.Environment.IsDevelopment())
    {
        throw; // Re-throw in development for debugging
    }

    // In production, you might want to handle this differently
    logger.LogCritical("Application startup failed due to database initialization error");
    Environment.Exit(1);
}

app.Run();

async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = serviceProvider.GetRequiredService<RefundDisputeContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Creating roles...");
        await CreateRoleIfNotExistsAsync(roleManager, "Admin");
        await CreateRoleIfNotExistsAsync(roleManager, "User");

        if (app.Environment.IsDevelopment())
        {
            var adminEmail = app.Configuration["SeedAdmin:Email"];
            var adminPassword = app.Configuration["SeedAdmin:Password"];
            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                await CreateDefaultAdminAsync(userManager, adminEmail, adminPassword);
            }
        }

        // Comment out test data seeding until entities are fully implemented
        // logger.LogInformation("Seeding test data...");
        // await SeedTestDataAsync(context, userManager);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization");
        throw;
    }
}

async Task CreateRoleIfNotExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
{
    if (!await roleManager.RoleExistsAsync(roleName))
    {
        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
        }
    }
}

async Task CreateDefaultAdminAsync(UserManager<ApplicationUser> userManager, string adminEmail, string adminPassword)
{
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin != null)
    {
        return; // Admin already exists
    }

    var admin = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        FirstName = "Admin",
        LastName = "User",
        RefreshToken = Guid.NewGuid().ToString(),
        LastLoginAt = DateTime.UtcNow,
        EmailConfirmed = true
    };

    var createResult = await userManager.CreateAsync(admin, adminPassword);
    if (!createResult.Succeeded)
    {
        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to create admin user: {errors}");
    }

    var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
    if (!roleResult.Succeeded)
    {
        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to add admin role: {errors}");
    }
}

async Task SeedTestDataAsync(RefundDisputeContext context, UserManager<ApplicationUser> userManager)
{
    // Check if data already exists
    if (await context.Transactions.AnyAsync())
    {
        return; // Data already seeded
    }

    // Create test user if not exists
    const string testUserEmail = "testuser@internship.com";
    var testUser = await userManager.FindByEmailAsync(testUserEmail);

    if (testUser == null)
    {
        testUser = new ApplicationUser
        {
            UserName = testUserEmail,
            Email = testUserEmail,
            FirstName = "Test",
            LastName = "User",
            RefreshToken = Guid.NewGuid().ToString(),
            LastLoginAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(testUser, "TestUser123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(testUser, "User");
        }
    }

    // Create sample transactions for development/testing
    if (testUser != null)
    {
        var transactions = new[]
        {
            new Transaction
            {
                TransactionId = "TXN-001",
                UserId = testUser.Id,
                Amount = 100.00m,
                Currency = "USD",
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                PaymentMethod = PaymentMethod.CreditCard,
                Description = "Sample payment transaction",
                MerchantReference = "REF-001",
                MaskedCardNumber = "**** **** **** 1234",
                CardHolderName = "Test User",
                CardType = "Visa",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Transaction
            {
                TransactionId = "TXN-002",
                UserId = testUser.Id,
                Amount = 250.50m,
                Currency = "USD",
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                PaymentMethod = PaymentMethod.DebitCard,
                Description = "Another sample transaction",
                MerchantReference = "REF-002",
                MaskedCardNumber = "**** **** **** 5678",
                CardHolderName = "Test User",
                CardType = "MasterCard",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
    }
}
