using System.Net.Http.Json;
using System.Text.Json;
using M_Wallet.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using M_Wallet.Shared;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;

namespace M_Wallet.Client.Services
{
    public class NavItem
    {
        public string Title { get; set; } = "";
        public string Href { get; set; } = "";
        public string Icon { get; set; } = "";
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
    }

    public class UserPreferencesService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authStateProvider;
        private UserPreferences _preferences = new();
        private int? _currentUserId;

        public event Action? OnChange;

        public List<NavItem> AvailableRoutes { get; } = new()
        {
            new() { Title = "POS", Href = "", Icon = Icons.Material.Filled.Home, Match = NavLinkMatch.All },
            new() { Title = "Transactions", Href = "transactions", Icon = Icons.Material.Filled.Receipt },
            new() { Title = "Statistics", Href = "stats", Icon = Icons.Material.Filled.BarChart },
            new() { Title = "Products", Href = "products", Icon = Icons.Material.Filled.Inventory },
            new() { Title = "Purchases", Href = "purchases", Icon = Icons.Material.Filled.ShoppingCart },
            new() { Title = "Debts & Balance", Href = "debts", Icon = Icons.Material.Filled.MoneyOff },
            new() { Title = "Users", Href = "users", Icon = Icons.Material.Filled.People },
            new() { Title = "History & Logs", Href = "logs", Icon = Icons.Material.Filled.History },
            new() { Title = "Settings", Href = "settings", Icon = Icons.Material.Filled.Settings },
        };

        public UserPreferencesService(HttpClient http, AuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        public UserPreferences Preferences => _preferences;

        public async Task InitializeAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst("Id");
                if (int.TryParse(idClaim?.Value, out int id))
                {
                    _currentUserId = id;
                    await LoadPreferences();
                }
            }
            else
            {
                _currentUserId = null;
                _preferences = new UserPreferences();
                NotifyStateChanged();
            }
        }

        public async Task<UserPreferences> LoadPreferences()
        {
            if (_currentUserId == null) return _preferences;

            try
            {
                // Add timestamp to prevent caching
                var person = await _http.GetFromJsonAsync<Person>($"api/people/{_currentUserId}?t={DateTime.Now.Ticks}");
                
                if (!string.IsNullOrEmpty(person?.Preferences))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _preferences = JsonSerializer.Deserialize<UserPreferences>(person.Preferences, options) ?? new UserPreferences();
                    Console.WriteLine($"Loaded preferences for user {_currentUserId}. Favorites: {_preferences.FavoriteRoutes.Count}");
                }
                else
                {
                    _preferences = new UserPreferences();
                    Console.WriteLine($"Loaded empty preferences for user {_currentUserId}");
                }
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preferences: {ex.Message}");
                // Do NOT reset preferences on error, keep existing state or default
                // _preferences = new UserPreferences(); 
                // NotifyStateChanged();
            }
            return _preferences;
        }

        public async Task SavePreferences(UserPreferences? newPreferences = null)
        {
            if (_currentUserId == null) return;

            if (newPreferences != null)
            {
                _preferences = newPreferences;
                NotifyStateChanged();
            }

            try
            {
                var response = await _http.PutAsJsonAsync($"api/people/{_currentUserId}/preferences", _preferences);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to save preferences: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving preferences: {ex.Message}");
            }
        }

        public async Task ToggleDarkMode()
        {
            _preferences.IsDarkMode = !_preferences.IsDarkMode;
            NotifyStateChanged();
            await SavePreferences();
        }

        public async Task ToggleFavoriteRoute(string route)
        {
            if (_preferences.FavoriteRoutes.Contains(route))
            {
                _preferences.FavoriteRoutes.Remove(route);
            }
            else
            {
                _preferences.FavoriteRoutes.Add(route);
            }
            NotifyStateChanged();
            await SavePreferences();
        }

        public bool IsRouteFavorite(string route)
        {
            return _preferences.FavoriteRoutes.Contains(route);
        }

        public async Task SetColumnVisibility(string tableName, string columnTitle, bool isVisible)
        {
            if (!_preferences.TableColumns.ContainsKey(tableName))
            {
                _preferences.TableColumns[tableName] = new Dictionary<string, bool>();
            }
            _preferences.TableColumns[tableName][columnTitle] = isVisible;
            
            await SavePreferences();
        }
        
        public bool IsColumnVisible(string tableName, string columnTitle, bool defaultState = true)
        {
            if (_preferences.TableColumns.TryGetValue(tableName, out var columns))
            {
                if (columns.TryGetValue(columnTitle, out var isVisible))
                {
                    return isVisible;
                }
            }
            return defaultState;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
