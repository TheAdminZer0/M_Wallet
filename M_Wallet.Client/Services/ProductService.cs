using System.Net.Http.Json;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

public class ProductService
{
    private readonly HttpClient _http;
    private List<Product>? _products;
    private bool _isInitialized = false;
    private Task? _initializationTask;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        if (_initializationTask == null || _initializationTask.IsFaulted)
        {
            _initializationTask = LoadDataInternalAsync();
        }

        await _initializationTask;
    }

    private async Task LoadDataInternalAsync()
    {
        try
        {
            _products = await _http.GetFromJsonAsync<List<Product>>("api/products");
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing product service: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Product>> GetProductsAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _isInitialized = false;
            _initializationTask = null;
        }

        await InitializeAsync();
        return _products ?? new List<Product>();
    }

    // Helper to refresh data after an update (add/delete/edit)
    public async Task RefreshDataAsync()
    {
        _isInitialized = false;
        _initializationTask = null;
        await InitializeAsync();
    }
}
