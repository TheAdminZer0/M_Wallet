using System.Net.Http.Json;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

/// <summary>
/// Service for caching product data with lazy initialization.
/// Reduces API calls by caching products and providing refresh capability.
/// </summary>
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

    /// <summary>
    /// Initializes the service and loads data if not already loaded.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        if (_initializationTask == null || _initializationTask.IsFaulted)
        {
            _initializationTask = LoadDataInternalAsync();
        }

        await _initializationTask;
    }

    /// <summary>
    /// Loads products from the API.
    /// </summary>
    private async Task LoadDataInternalAsync()
    {
        _products = await _http.GetFromJsonAsync<List<Product>>("api/products");
        _isInitialized = true;
    }

    /// <summary>
    /// Gets cached products, optionally forcing a refresh from the API.
    /// </summary>
    /// <param name="forceRefresh">If true, reloads data from API before returning.</param>
    /// <returns>List of all active products.</returns>
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

    /// <summary>
    /// Forces a full data refresh from the API.
    /// Call this after creating, updating, or deleting products.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        _isInitialized = false;
        _initializationTask = null;
        await InitializeAsync();
    }
}
