using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    // Holds the current user's claims. By default, the user is anonymous.
    private ClaimsPrincipal _user = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Gets the current authentication state. 
    /// If a token exists in local storage, the user is considered authenticated.
    /// Otherwise, returns an anonymous user.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Retrieve the token from local storage.
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (!string.IsNullOrEmpty(token))
            {
                // Create a new ClaimsIdentity with an authentication type (e.g., "jwt")
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "User")
                }, "jwt");

                _user = new ClaimsPrincipal(identity);
            }
        }
        catch (InvalidOperationException)
        {
            // If JavaScript interop is not available (e.g., during prerendering),
            // return an anonymous user.
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new AuthenticationState(_user);
    }

    /// <summary>
    /// Refreshes the authentication state.
    /// This is useful after the JS environment has loaded or when a token change occurs.
    /// </summary>
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

        // Notify subscribers that the authentication state has changed.
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    /// <summary>
    /// Logs in the user by saving the token to local storage and updating the authentication state.
    /// </summary>
    /// <param name="token">The authentication token.</param>
    public async Task Login(string token)
    {
        // Зберігаємо токен
        await _localStorage.SetItemAsync("authToken", token);

        // Створюємо identity з деякими базовими claims
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, "User"),
        new Claim(ClaimTypes.NameIdentifier, "user-id"),
        new Claim("access_token", token)
    };

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        // Оновлюємо приватне поле _user
        _user = user;

        // Повідомляємо про зміну стану аутентифікації
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    /// <summary>
    /// Logs out the user by removing the token from local storage and resetting the authentication state.
    /// </summary>
    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }
}
