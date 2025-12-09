using System.Net.Http.Json;
using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

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
            // Fetch in parallel
            var transactionsTask = _http.GetFromJsonAsync<List<Transaction>>("api/transactions");
            var paymentsTask = _http.GetFromJsonAsync<List<Payment>>("api/payments");

            await Task.WhenAll(transactionsTask, paymentsTask);

            _transactions = await transactionsTask;
            _payments = await paymentsTask;
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing transaction service: {ex.Message}");
            throw;
        }
    }

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

    // Helper to refresh data after an update (add/delete)
    public async Task RefreshDataAsync()
    {
        _isInitialized = false;
        _initializationTask = null;
        await InitializeAsync();
    }
}
