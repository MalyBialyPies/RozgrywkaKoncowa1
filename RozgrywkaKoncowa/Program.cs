using RozgrywkaKoncowa.Resources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure language from appsettings
var defaultLanguage = app.Configuration.GetValue<string>("AppSettings:DefaultLanguage") ?? "pl";
Strings.SetLanguage(defaultLanguage);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/StrategyEval/Index");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=StrategyEval}/{action=Index}/{id?}");

app.Run();
