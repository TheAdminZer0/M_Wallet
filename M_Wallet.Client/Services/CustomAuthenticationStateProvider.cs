using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using M_Wallet.Shared;
using M_Wallet.Shared.Models;
using System.Net.Http.Json;

namespace M_Wallet.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly string _userKey = "authUser";

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? userJson = null;
        try
        {
            userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", _userKey);
        }
        catch
        {
            // Ignore errors (e.g. during server-side prerendering)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        if (string.IsNullOrEmpty(userJson))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var person = JsonSerializer.Deserialize<Person>(userJson);
            if (person == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, person.Name),
                new Claim(ClaimTypes.Role, person.Role),
                new Claim("Id", person.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/people/login", new LoginRequest { Username = username, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var person = await response.Content.ReadFromJsonAsync<Person>();
                if (person != null)
                {
                    var userJson = JsonSerializer.Serialize(person);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _userKey, userJson);
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _userKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
    
    public async Task<Person?> GetCurrentUserAsync()
    {
        string? userJson = null;
        try
        {
            userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", _userKey);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrEmpty(userJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Person>(userJson);
        }
        catch
        {
            return null;
        }
    }
}
