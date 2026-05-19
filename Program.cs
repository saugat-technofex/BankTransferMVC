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

builder.Services.AddHttpClient<IClearJunctionClient, ClearJunctionClient>();

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
