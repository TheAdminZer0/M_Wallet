using System.Net.Http.Json;
using System.Text.Json;
using M_Wallet.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services
{
    public class UserPreferencesService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authStateProvider;
        private UserPreferences _preferences = new();
        private int? _currentUserId;

        public event Action? OnChange;

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

        private async Task LoadPreferences()
        {
            if (_currentUserId == null) return;

            try
            {
                // Fetch all employees to find the current one (Optimization: Add GET /api/employees/{id} later)
                var employees = await _http.GetFromJsonAsync<List<Employee>>("api/employees");
                var employee = employees?.FirstOrDefault(e => e.Id == _currentUserId);
                
                if (employee?.Preferences != null)
                {
                    _preferences = JsonSerializer.Deserialize<UserPreferences>(employee.Preferences) ?? new UserPreferences();
                }
                else
                {
                    _preferences = new UserPreferences();
                }
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preferences: {ex.Message}");
                _preferences = new UserPreferences();
                NotifyStateChanged();
            }
        }

        public async Task SavePreferences()
        {
            if (_currentUserId == null) return;

            try
            {
                var json = JsonSerializer.Serialize(_preferences);
                var response = await _http.PutAsJsonAsync($"api/employees/{_currentUserId}/preferences", json);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to save preferences");
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
