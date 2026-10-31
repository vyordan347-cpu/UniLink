using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using UniLink.Data;
using UniLink.Models;
using UniLink.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(puerto))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'DefaultConnection'. Configúrala con user-secrets en desarrollo o con una variable de entorno en producción.");

builder.Services.AddDbContext<UniLinkDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<Usuario, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<UniLinkDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
});

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var dataProtection = builder.Services.AddDataProtection();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
    redisConfiguration.AbortOnConnectFail = false;
    IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConfiguration);

    builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.InstanceName = "UniLink:";
        options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection);
    });
    dataProtection.PersistKeysToStackExchangeRedis(redisConnection, "UniLink:DataProtection-Keys");
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSession();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    app.Logger.LogWarning(
        "No se configuró Redis. Se usarán sesiones y caché en memoria; las claves de Data Protection no se compartirán mediante Redis.");
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/health", async (UniLinkDbContext db, IDistributedCache cache) =>
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

    var redisEstado = "no configurado";
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        redisEstado = "sin conexión";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var clavePrueba = $"health:{Guid.NewGuid():N}";
            var valorPrueba = Guid.NewGuid().ToString("N");

            await cache.SetStringAsync(
                clavePrueba,
                valorPrueba,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                timeout.Token).WaitAsync(timeout.Token);

            var valorLeido = await cache.GetStringAsync(clavePrueba, timeout.Token)
                .WaitAsync(timeout.Token);
            if (valorLeido == valorPrueba)
            {
                redisEstado = "conectado";
            }
        }
        catch
        {
            // Una falla de Redis no debe impedir que el endpoint de salud responda.
        }
    }

    return Results.Ok(new
    {
        estado = "ok",
        baseDeDatos = baseDeDatosConectada ? "conectada" : "sin conexión",
        redis = redisEstado
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
