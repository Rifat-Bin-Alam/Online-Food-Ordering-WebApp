
using Microsoft.EntityFrameworkCore;
using OnlineFoodOrdering.Data;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(90);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".OnlineFoodOrdering.Session";
});

// Configure Stripe API key from configuration
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];

if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        DbInitializer.Initialize(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Main/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

// Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

app.Run();
