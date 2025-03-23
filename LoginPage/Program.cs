using Blazored.LocalStorage;
using LoginPage.Client;
using LoginPage.Server.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DI-сервіси
// Додаємо HTTP-клієнт зі специфічною базовою адресою для сервера API
builder.Services.AddHttpClient("ServerAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.Configuration.GetValue<string>("BaseAddress") ?? "https://localhost:7227/");
});

// Налаштовуємо HttpClient для клієнтського проекту Blazor WebAssembly
builder.Services.AddScoped(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return clientFactory.CreateClient("ServerAPI");
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddControllers();

// Додаємо AppDbContext з SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT автентифікація
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

// Налаштування CORS для дозволу запитів з клієнтського додатку
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("*")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed(_ => true); // Будьте обережні з цим в продакшені, це дозволяє всі домени
    });
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Middleware
// Важливо розмістити UseCors перед UseRouting, UseAuthorization, UseEndpoints
app.UseCors("CorsPolicy");

// Для відлагодження CORS в режимі розробки
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    app.Use(async (context, next) =>
    {
        // Запис всіх заголовків для діагностики
        Console.WriteLine("------- Запит отримано -------");
        Console.WriteLine($"Метод: {context.Request.Method}");
        Console.WriteLine($"Шлях: {context.Request.Path}");
        Console.WriteLine("Заголовки:");
        foreach (var header in context.Request.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        // Для кроссдоменних запитів реєструємо спеціальні заголовки
        if (context.Request.Headers.ContainsKey("Origin"))
        {
            Console.WriteLine($"Кросс-доменний запит з: {context.Request.Headers["Origin"]}");
        }

        await next();

        // Відстеження відповіді
        Console.WriteLine("------- Відповідь відправлено -------");
        Console.WriteLine($"Статус: {context.Response.StatusCode}");
        Console.WriteLine("Заголовки відповіді:");
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();