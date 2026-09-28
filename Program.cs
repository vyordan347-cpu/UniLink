using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using UniLink.Data;

var builder = WebApplication.CreateBuilder(args);

var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(puerto))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'DefaultConnection'. Configúrala con user-secrets en desarrollo o con una variable de entorno en producción.");

builder.Services.AddDbContext<UniLinkDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/health", async (UniLinkDbContext db) =>
{
    var baseDeDatosConectada = false;
    try
    {
        baseDeDatosConectada = await db.Database.CanConnectAsync();
    }
    catch
    {
        // El endpoint informa la falta de conexión sin exponer detalles de configuración.
    }

    return Results.Ok(new
    {
        estado = "ok",
        baseDeDatos = baseDeDatosConectada ? "conectada" : "sin conexión"
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<UniLinkDbContext>();
        db.Database.Migrate();
    }
    catch (Exception exception)
    {
        app.Logger.LogError(
            "No se pudieron aplicar las migraciones pendientes. Tipo de error: {TipoError}",
            exception.GetType().Name);
    }
}

app.Run();
