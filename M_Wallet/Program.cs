using MudBlazor.Services;
using M_Wallet.Client.Pages;
using M_Wallet.Components;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using System.Text.Json.Serialization;
using M_Wallet.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add API Controllers with JSON options to handle circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Add HttpClient for server-side rendering (IMPORTANT!)
builder.Services.AddScoped<HttpClient>(sp =>
{
    var client = new HttpClient
    {
        BaseAddress = new Uri("https://localhost:7265/") // Your server URL
    };
    return client;
});

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserPreferencesService>();

// Add Authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
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

// Map API controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(M_Wallet.Client._Imports).Assembly);

app.Run();
