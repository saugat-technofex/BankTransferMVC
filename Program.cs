using BankTransferMVC.Integrations.ClearJunction;
using BankTransferMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<ClearJunctionOptions>(
    builder.Configuration.GetSection(ClearJunctionOptions.SectionName));

builder.Services.AddSingleton<IClearJunctionSignatureService, ClearJunctionSignatureService>();
builder.Services.AddSingleton<ITransferStore, InMemoryTransferStore>();
builder.Services.AddSingleton<IClientCustomerIdGenerator, ClientCustomerIdGenerator>();
builder.Services.AddSingleton<ICjReferenceCatalog, CjReferenceCatalog>();
builder.Services.AddSingleton<ICjFieldSchema, CjFieldSchema>();

builder.Services.AddSingleton<ICjModeService, CjModeService>();
builder.Services.AddSingleton<ICjSimulator, CjSimulator>();
builder.Services.AddSingleton<ICjCallLog, CjCallLog>();

builder.Services.AddHttpClient<IClearJunctionClient, ClearJunctionClient>();
builder.Services.AddHttpClient("cj-webhook-self", c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<CjLifecycleWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
