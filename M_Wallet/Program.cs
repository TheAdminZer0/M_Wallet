using MudBlazor.Services;
using M_Wallet.Client.Pages;
using M_Wallet.Components;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using System.Text.Json.Serialization;

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

app.UseAntiforgery();

// Map API controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(M_Wallet.Client._Imports).Assembly);

app.Run();
