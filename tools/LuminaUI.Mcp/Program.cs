using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using LuminaUI.Mcp;
using LuminaUI.Mcp.Indexing;
using LuminaUI.Mcp.Tools;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);
const string DefaultAdminPassword = "luminaui";

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

var dbPath = CatalogConfiguration.ResolveDatabasePath(builder.Configuration);
var dataDir = Path.GetDirectoryName(dbPath)!;
if (!string.IsNullOrEmpty(dataDir))
    Directory.CreateDirectory(dataDir);

var adminUser = AdminConfiguration.ResolveUsername(builder.Configuration) ?? "admin";
var adminPass = AdminConfiguration.ResolvePassword(builder.Configuration) ?? DefaultAdminPassword;
var requireConfiguredAdminPassword = builder.Configuration.GetValue("Admin:RequireConfiguredPassword", false);

if (adminPass == DefaultAdminPassword && requireConfiguredAdminPassword)
    throw new InvalidOperationException("LuminaUI MCP admin password must be configured. Set Admin__Password or MCP_ADMIN_PASSWORD before exposing the service.");

if (!builder.Environment.IsDevelopment() && adminPass == DefaultAdminPassword)
    Console.Error.WriteLine("WARNING: LuminaUI MCP is using the default admin password. Set Admin__Password or MCP_ADMIN_PASSWORD before exposing the service.");

var store = new CatalogStore(dbPath);
store.Initialize();

builder.Services.AddSingleton(store);
builder.Services.AddSingleton<ReindexService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(24);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = DocsMcpServerMetadata.Name,
            Version = DocsMcpServerMetadata.Version
        };
        options.ServerInstructions =
            "LuminaUI.Mcp is the documentation MCP for LuminaUI components, examples, design tokens, installation guidance, and MCP tool discovery. Use LuminaUI.Diagnostics.Mcp separately for live Avalonia application inspection.";
    })
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<LuminaMcpTools>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ─── MCP endpoint (public, read-only) ───
app.MapMcp("/mcp");

// ─── Health (public) ───
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/readyz", (CatalogStore s) =>
{
    var version = s.CurrentVersion;
    return version is null
        ? Results.Json(new { status = "not_ready", reason = "catalog_empty" }, statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(new { status = "ready", version.LibraryVersion, version.SourceCommit, version.GeneratedAt, version.ComponentCount });
});

// ─── Login page ───
app.MapGet("/login", () => Results.Extensions.FileHtml("login.html"));

app.MapPost("/login", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var user = form["username"].ToString();
    var pass = form["password"].ToString();

    if (user == adminUser && pass == adminPass)
    {
        var claims = new[] { new Claim("role", "admin") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Redirect("/admin");
    }

    return Results.Extensions.FileHtml("login.html", error: true);
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Redirect("/login");
});

// ─── Admin API (protected) ───
app.MapGet("/api/catalog", (CatalogStore s) =>
{
    var v = s.CurrentVersion;
    return v is null
        ? Results.Ok(new { status = "empty" })
        : Results.Ok(new { status = "ready", v.LibraryVersion, v.SourceCommit, v.GeneratedAt, v.ComponentCount });
}).RequireAuthorization();

app.MapPost("/api/reindex", (ReindexService svc, ReindexRequest req) =>
{
    if (!GitRefValidator.TryResolve(req, out var branchOrTag, out var error))
        return Results.BadRequest(new { error });

    if (!svc.TryStart())
        return Results.Conflict(new { error = "Reindex already in progress", progress = svc.Progress });

    _ = Task.Run(() => svc.RunAsync(branchOrTag));
    return Results.Accepted("/api/reindex/status", new { status = "started" });
}).RequireAuthorization();

app.MapGet("/api/reindex/status", (ReindexService svc) =>
{
    return Results.Ok(new { svc.IsRunning, svc.Progress, svc.LastResult });
}).RequireAuthorization();

// ─── Admin UI (protected) ───
app.MapGet("/", () => Results.Redirect("/admin"));
app.MapGet("/admin", () => Results.Extensions.FileHtml("admin.html")).RequireAuthorization();

if (store.CurrentVersion is null && app.Configuration.GetValue("Catalog:AutoIndexOnEmpty", false))
{
    var autoIndexRef = GitRefValidator.NormalizeOptional(app.Configuration.GetValue<string>("Catalog:DefaultRef"));
    if (autoIndexRef is null || GitRefValidator.IsAllowedRef(autoIndexRef))
    {
        var reindexService = app.Services.GetRequiredService<ReindexService>();
        if (reindexService.TryStart())
        {
            Console.Error.WriteLine($"  AutoIndex: starting catalog build from {autoIndexRef ?? "default branch"}");
            _ = Task.Run(() => reindexService.RunAsync(autoIndexRef));
        }
    }
    else
    {
        Console.Error.WriteLine($"WARNING: Catalog:DefaultRef '{autoIndexRef}' is not a valid git ref name. Auto index skipped.");
    }
}

Console.Error.WriteLine($"LuminaUI MCP Server");
Console.Error.WriteLine($"  Database: {dbPath}");
Console.Error.WriteLine($"  Remote:   {CatalogConfiguration.ResolveRemoteUrl(app.Configuration)}");
Console.Error.WriteLine($"  Status:   {(store.CurrentVersion is not null ? $"ready ({store.CurrentVersion.ComponentCount} components)" : "empty - login to build")}");
Console.Error.WriteLine($"  MCP:      http://localhost:{app.Configuration.GetValue("Catalog:Port", "3001")}/mcp");
Console.Error.WriteLine($"  Admin:    http://localhost:{app.Configuration.GetValue("Catalog:Port", "3001")}/admin");
Console.Error.WriteLine($"  Login:    {adminUser} / {(adminPass == DefaultAdminPassword ? DefaultAdminPassword : "(configured)")}");

var port = app.Configuration.GetValue("Catalog:Port", "3001");
app.Run($"http://0.0.0.0:{port}");

// ═══════════════════════════════════════════════════════════════
// HTML helper
// ═══════════════════════════════════════════════════════════════

public static class ResultsExtensions
{
    public static IResult FileHtml(this IResultExtensions _, string fileName, bool error = false)
    {
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", fileName);
        if (!File.Exists(htmlPath))
            return Results.Content($"<h1>File not found: {fileName}</h1>", "text/html");

        var html = File.ReadAllText(htmlPath);
        if (error)
            html = html.Replace("<!-- ERROR -->", "<div class=\"error\">Invalid username or password</div>");
        return Results.Content(html, "text/html");
    }
}

// ═══════════════════════════════════════════════════════════════
// Models
// ═══════════════════════════════════════════════════════════════

public sealed class ReindexRequest
{
    public string? Branch { get; set; }
    public string? Tag { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Reindex service
// ═══════════════════════════════════════════════════════════════

public sealed class ReindexService
{
    private readonly CatalogStore _store;
    private readonly ILogger<ReindexService> _logger;
    private readonly object _lock = new();

    public bool IsRunning { get; private set; }
    public string Progress { get; private set; } = "";
    public object? LastResult { get; private set; }

    private readonly string _remoteUrl;

    public ReindexService(CatalogStore store, IConfiguration config, ILogger<ReindexService> logger)
    {
        _store = store;
        _remoteUrl = CatalogConfiguration.ResolveRemoteUrl(config);
        _logger = logger;
    }

    public bool TryStart()
    {
        lock (_lock)
        {
            if (IsRunning) return false;
            IsRunning = true;
            Progress = "Starting...";
            LastResult = null;
            return true;
        }
    }

    public async Task RunAsync(string? branchOrTag)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"lumina-mcp-src-{Guid.NewGuid():N}");
        try
        {
            Progress = $"Cloning {branchOrTag ?? "default branch"}...";
            _logger.LogInformation("Cloning {Ref} from {Remote}", branchOrTag ?? "default branch", _remoteUrl);

            var cloneArgs = new List<string> { "clone", "--depth", "1" };
            if (branchOrTag is not null)
            {
                cloneArgs.Add("--branch");
                cloneArgs.Add(branchOrTag);
            }

            cloneArgs.Add(_remoteUrl);
            cloneArgs.Add(workDir);
            await RunGitAsync(Path.GetTempPath(), cloneArgs);

            Progress = "Extracting components...";
            _logger.LogInformation("Building catalog from {Dir}", workDir);

            CatalogData catalog;
            using (var loggerFactory = LoggerFactory.Create(b => b.AddConsole()))
            {
                var catalogBuilder = new CatalogBuilder(workDir, loggerFactory.CreateLogger<CatalogBuilder>());
                catalog = await Task.Run(() => catalogBuilder.Build());
            }

            var commit = await RunGitOutputAsync(workDir, ["rev-parse", "--short", "HEAD"]);
            catalog.SourceCommit = commit;

            Progress = "Saving to database...";
            _store.SaveCatalog(catalog);

            LastResult = new
            {
                status = "success",
                catalog.LibraryVersion,
                commit,
                ComponentCount = catalog.Components.Count,
                EnumCount = catalog.Enums.Count,
                ExampleCount = catalog.Examples.Count,
                TokenCount = catalog.DesignTokens.Count,
                completedAt = DateTime.UtcNow.ToString("o")
            };

            Progress = "Done";
            _logger.LogInformation("Reindex complete: {Components} components", catalog.Components.Count);
        }
        catch (Exception ex)
        {
            LastResult = new { status = "error", error = ex.Message };
            Progress = $"Error: {ex.Message}";
            _logger.LogError(ex, "Reindex failed");
        }
        finally
        {
            try { if (Directory.Exists(workDir)) Directory.Delete(workDir, true); } catch { }
            IsRunning = false;
        }
    }

    private static async Task RunGitAsync(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}{stdout}");
        }
    }

    private static async Task<string> RunGitOutputAsync(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {error}{output}");

        return output.Trim();
    }
}

public static class GitRefValidator
{
    private static readonly Regex RefRegex = new(@"^[A-Za-z0-9][A-Za-z0-9._/-]{0,127}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryResolve(ReindexRequest request, out string? branchOrTag, out string? error)
    {
        var branch = NormalizeOptional(request.Branch);
        var tag = NormalizeOptional(request.Tag);

        branchOrTag = null;
        error = null;

        if (branch is not null && tag is not null)
        {
            error = "Specify either branch or tag, not both.";
            return false;
        }

        branchOrTag = branch ?? tag;
        if (branchOrTag is null)
            return true;

        if (IsAllowedRef(branchOrTag))
            return true;

        error = "Invalid git ref. Use a branch or tag like 'master', 'v0.2.0', or 'feature/name'.";
        return false;
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static bool IsAllowedRef(string value)
    {
        return RefRegex.IsMatch(value)
            && !value.StartsWith("-", StringComparison.Ordinal)
            && !value.Contains("..", StringComparison.Ordinal)
            && !value.Contains("//", StringComparison.Ordinal)
            && !value.Contains("@{", StringComparison.Ordinal)
            && !value.Contains('\\')
            && !value.EndsWith("/", StringComparison.Ordinal)
            && !value.EndsWith(".", StringComparison.Ordinal);
    }
}

public static class CatalogConfiguration
{
    private const string DefaultRemoteUrl = "https://github.com/j4587698/LuminaUI.git";

    public static string ResolveDatabasePath(IConfiguration configuration)
    {
        var configured = configuration.GetValue<string>("Catalog:DatabasePath");
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine("data", "lumina-mcp.db")
            : configured;

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);
    }

    public static string ResolveRemoteUrl(IConfiguration configuration)
    {
        var configured = configuration.GetValue<string>("Catalog:RemoteUrl");
        return string.IsNullOrWhiteSpace(configured)
            ? DefaultRemoteUrl
            : configured.Trim();
    }
}

public static class AdminConfiguration
{
    public static string? ResolveUsername(IConfiguration configuration)
    {
        var envValue = Environment.GetEnvironmentVariable("MCP_ADMIN_USERNAME");
        return string.IsNullOrWhiteSpace(envValue)
            ? configuration.GetValue<string>("Admin:Username")
            : envValue.Trim();
    }

    public static string? ResolvePassword(IConfiguration configuration)
    {
        var envValue = Environment.GetEnvironmentVariable("MCP_ADMIN_PASSWORD");
        return string.IsNullOrWhiteSpace(envValue)
            ? configuration.GetValue<string>("Admin:Password")
            : envValue;
    }
}
