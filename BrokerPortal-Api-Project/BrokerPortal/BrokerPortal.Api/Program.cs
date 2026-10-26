using BrokerPortal.Application.DuckCreek;
using BrokerPortal.Application.Users;
using BrokerPortal.Api;
using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using BrokerPortal.Infrastructure.DuckCreek;
using BrokerPortal.Infrastructure.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and Swagger.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-BrokerPortal.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 480));
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(id, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BrokerPortalDbContext>();
            var account = await dbContext.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new { user.Status, user.Role, user.AgencyId })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (account is null ||
                account.Status != "Active" ||
                account.Role != principal!.FindFirstValue(ClaimTypes.Role) ||
                account.AgencyId?.ToString() != principal!.FindFirstValue(ClaimsPrincipalExtensions.AgencyIdClaim))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-BrokerPortal.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.Configure<InvitationEmailOptions>(
    builder.Configuration.GetSection("InvitationEmail"));
builder.Services.Configure<ActivationPasswordOptions>(
    builder.Configuration.GetSection("ActivationPassword"));
builder.Services.Configure<WorkspaceOptions>(
    builder.Configuration.GetSection("Workspaces"));
builder.Services.Configure<DuckCreekOptions>(
    builder.Configuration.GetSection("DuckCreek"));

// Configure SQL Server.
var connectionString =
    builder.Configuration.GetConnectionString("BrokerPortalDb")
    ?? throw new InvalidOperationException(
        "BrokerPortalDb connection string is missing.");

builder.Services.AddDbContext<BrokerPortalDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register application services.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPasswordHasher<DuckCreekUser>, PasswordHasher<DuckCreekUser>>();

// Duck Creek User Admin: "Local" stores users in the portal database until a Duck Creek
// environment is available; "Http" calls the real API. Switch with DuckCreek:Mode.
if (string.Equals(builder.Configuration["DuckCreek:Mode"], DuckCreekOptions.ModeHttp,
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IDuckCreekUserAdminClient, HttpDuckCreekUserAdminClient>();
}
else
{
    builder.Services.AddScoped<IDuckCreekUserAdminClient, LocalDuckCreekUserAdminClient>();
}
builder.Services.AddScoped<DevelopmentOnlyAntiforgeryFilter>();
builder.Services.AddScoped<IPasswordHasher<PortalUser>, PasswordHasher<PortalUser>>();
builder.Services.AddSingleton<IInvitationEmailService, InvitationEmailService>();

// Allow requests from the Angular development server.
builder.Services.AddCors(options =>
{
    options.AddPolicy("BrokerPortalAngular", policy =>
        policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

await DevelopmentAdminBootstrapper.EnsureAdminAsync(
    app.Services, builder.Configuration, app.Environment);

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("GlobalExceptionHandler");
    logger.LogError(exception, "An unhandled API error occurred.");

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        message = "An unexpected server error occurred."
    });
}));

// Enable Swagger UI in Development mode.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Apply the Angular CORS policy.
app.UseCors("BrokerPortalAngular");
app.UseAuthentication();
app.UseAuthorization();

// Map controller endpoints once.
app.MapControllers();

// Root endpoint to confirm the API is running.
app.MapGet("/", () => Results.Ok(new
{
    application = "BrokerPortal API",
    status = "Running",
    documentation = "/swagger"
}));

app.Run();
