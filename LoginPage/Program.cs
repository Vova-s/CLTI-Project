using Blazored.LocalStorage;
using LoginPage.Client;
using LoginPage.Server.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LoginPage.Client.Shared;

#region Configure Builder and Services
var builder = WebApplication.CreateBuilder(args);

#region Configure HTTP Client Services
// Add an HTTP client with a specific base address for the API server
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
                                 ?? builder.Configuration.GetValue<string>("BaseAddress")
                                 ?? "https://localhost:7227/");
});

// Configure HttpClient for the Blazor WebAssembly client project
builder.Services.AddScoped(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return clientFactory.CreateClient("ServerAPI");
});
#endregion

#region Configure Local Storage and Authentication State
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<StateService>();

#endregion

#region Configure Controllers and Database Context
builder.Services.AddControllers();

// Add AppDbContext using SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["JWT:SecretKey"] ?? "defaultSecretKey12345678901234567890"))
        };
    });
#endregion

#region Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("*")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed(_ => true); // Caution: This allows all domains; adjust for production.
    });
});
#endregion

#region Configure Blazor Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
#endregion

#endregion

#region Build Application and Configure Middleware
Console.WriteLine("Connection string: " + builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

#region Configure CORS and Debugging Middleware
// Use CORS policy before routing and authorization
app.UseCors("CorsPolicy");

// Debugging middleware for CORS in development mode
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    app.Use(async (context, next) =>
    {
        // Log request details for diagnostics
        Console.WriteLine("------- Request Received -------");
        Console.WriteLine($"Method: {context.Request.Method}");
        Console.WriteLine($"Path: {context.Request.Path}");
        Console.WriteLine("Headers:");
        foreach (var header in context.Request.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        // Log cross-domain request information if present
        if (context.Request.Headers.ContainsKey("Origin"))
        {
            Console.WriteLine($"Cross-domain request from: {context.Request.Headers["Origin"]}");
        }

        await next();

        // Log response details for diagnostics
        Console.WriteLine("------- Response Sent -------");
        Console.WriteLine($"Status: {context.Response.StatusCode}");
        Console.WriteLine("Response Headers:");
        foreach (var header in context.Response.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
#endregion

#region Configure Standard Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
#endregion

#region Configure Blazor Endpoints and Controllers
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.MapControllers();
app.MapFallbackToFile("index.html");
#endregion

#endregion

app.Run();
