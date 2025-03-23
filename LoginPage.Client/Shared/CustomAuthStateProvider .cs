using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private ClaimsPrincipal _user = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (!string.IsNullOrEmpty(token))
            {
                var identity = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, "User")
            }, "jwt");

                _user = new ClaimsPrincipal(identity);
            }
        }
        catch (InvalidOperationException)
        {
            // Якщо JS interop недоступний (пререндиринг), повертаємо анонімного користувача.
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_user);
    }

    // Додатковий метод для примусового оновлення стану після завантаження клієнтського JS середовища:
    public async Task RefreshAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrEmpty(token))
        {
            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, "User")
        }, "jwt");

            _user = new ClaimsPrincipal(identity);
        }
        else
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    public async Task Login(string token)
    {
        await _localStorage.SetItemAsync("authToken", token);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "User")
        }, "jwt");

        _user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }
}
