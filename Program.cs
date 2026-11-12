using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.ML;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
using UniLink.Data;
using UniLink.Hubs;
using UniLink.ML;
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
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api/asistente"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.WriteAsJsonAsync(new
            {
                respuesta = "Tu sesión ya no está activa. Inicia sesión nuevamente.",
                error = true
            });
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api/asistente"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new
            {
                respuesta = "No tienes permiso para usar esta función.",
                error = true
            });
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("asistente-chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "usuario-desconocido",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                respuesta = "Has enviado varios mensajes en poco tiempo. Espera un momento y vuelve a intentarlo.",
                herramientasUsadas = Array.Empty<string>()
            },
            cancellationToken);
    };
});
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, UsuarioIdProvider>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<BusquedaService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddSingleton<ColaCorreos>();
builder.Services.AddHttpClient<EnvioCorreoResend>();
builder.Services.AddHostedService<ConsumidorCorreos>();
builder.Services.AddHostedService<RecordatoriosAlquilerBackgroundService>();
builder.Services.AddSingleton<EntrenadorSentimiento>();
builder.Services.AddSingleton<ModeloRecomendacion>();
builder.Services.AddScoped<EntrenadorRecomendacion>();
builder.Services.AddScoped<RecomendacionService>();
builder.Services.AddScoped<AsistenteService>();
builder.Services.AddHostedService<EntrenamientoRecomendacionBackgroundService>();
var rutaModeloSentimiento = Path.Combine(builder.Environment.ContentRootPath, "ML", "Modelos", "sentimiento.zip");
builder.Services
    .AddPredictionEnginePool<DatosSentimiento, PrediccionSentimiento>()
    .FromFile(rutaModeloSentimiento, watchForChanges: false);

var app = builder.Build();

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    app.Logger.LogWarning(
        "No se configuró Redis. Se usarán sesiones y caché en memoria; las claves de Data Protection no se compartirán mediante Redis.");
}

var colaCorreos = app.Services.GetRequiredService<ColaCorreos>();
if (!colaCorreos.Habilitada)
{
    app.Logger.LogWarning(
        "El envío de correos está deshabilitado. Configura CloudAMQP:Url y Resend:ApiKey para habilitarlo.");
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler("/Home/Error");
app.UseHttpsRedirection();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseSession();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/health", async (UniLinkDbContext db, IDistributedCache cache, ColaCorreos colaCorreos) =>
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

    var colaEstado = "sin conexión";
    if (colaCorreos.CloudAMQPConfigurado)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            if (await colaCorreos.VerificarConexionAsync(timeout.Token).WaitAsync(timeout.Token))
            {
                colaEstado = "conectada";
            }
        }
        catch (OperationCanceledException)
        {
            // Una comprobación lenta de RabbitMQ no debe bloquear el endpoint de salud.
        }
    }

    return Results.Ok(new
    {
        estado = "ok",
        baseDeDatos = baseDeDatosConectada ? "conectada" : "sin conexión",
        redis = redisEstado,
        cola = colaEstado
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

app.MapOpenApi();
app.MapScalarApiReference();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<UniLinkDbContext>();
        await db.Database.MigrateAsync();
        var sembrador = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await sembrador.SeedAsync();
        var busquedaService = scope.ServiceProvider.GetRequiredService<BusquedaService>();
        await busquedaService.InicializarIndicesVaciosAsync(CancellationToken.None);
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "No se pudieron aplicar las migraciones o cargar los datos de demostración.");
    }

    var entrenador = scope.ServiceProvider.GetRequiredService<EntrenadorSentimiento>();
    if (!File.Exists(rutaModeloSentimiento))
    {
        entrenador.Entrenar(app.Environment.ContentRootPath);
    }
}

app.Run();
