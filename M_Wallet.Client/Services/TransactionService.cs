using System.Net.Http.Json;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

/// <summary>
/// Service for caching and providing transaction/payment data with lazy initialization.
/// Reduces API calls by caching data and providing refresh capability.
/// </summary>
public class TransactionService
{
    private readonly HttpClient _http;
    private List<Transaction>? _transactions;
    private List<Payment>? _payments;
    private bool _isInitialized = false;
    private Task? _initializationTask;

    public TransactionService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Initializes the service and loads data if not already loaded.
    /// Uses a single initialization task to prevent duplicate API calls.
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
    /// Fetches transactions and payments from the API in parallel.
    /// </summary>
    private async Task LoadDataInternalAsync()
    {
        // Fetch in parallel
        var transactionsTask = _http.GetFromJsonAsync<List<Transaction>>("api/transactions");
        var paymentsTask = _http.GetFromJsonAsync<List<Payment>>("api/payments");

        await Task.WhenAll(transactionsTask, paymentsTask);

        _transactions = await transactionsTask;
        _payments = await paymentsTask;
        
        _isInitialized = true;
    }

    /// <summary>
    /// Gets cached transactions, optionally forcing a refresh from the API.
    /// </summary>
    /// <param name="forceRefresh">If true, reloads data from API before returning.</param>
    /// <returns>List of all transactions with items and payment allocations.</returns>
    public async Task<List<Transaction>> GetTransactionsAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _isInitialized = false;
            _initializationTask = null;
        }

        await InitializeAsync();
        return _transactions ?? new List<Transaction>();
    }

    /// <summary>
    /// Gets cached payments, optionally forcing a refresh from the API.
    /// </summary>
    /// <param name="forceRefresh">If true, reloads data from API before returning.</param>
    /// <returns>List of all payments with allocations.</returns>
    public async Task<List<Payment>> GetPaymentsAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _isInitialized = false;
            _initializationTask = null;
        }

        await InitializeAsync();
        return _payments ?? new List<Payment>();
    }

    /// <summary>
    /// Forces a full refresh of all cached data from the API.
    /// Call this after creating, updating, or deleting transactions/payments.
    /// </summary>
    public async Task RefreshDataAsync()
    {
        _isInitialized = false;
        _initializationTask = null;
        await InitializeAsync();
    }
}
