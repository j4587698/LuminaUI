using LuminaUI.Diagnostics.Mcp.Tools;
using LuminaUI.Diagnostics.Mcp.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LuminaUI.Diagnostics.Mcp;

public static class ServerHostBuilder
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        Configure(builder);
        return builder.Build();
    }

    public static void Configure(HostApplicationBuilder builder)
    {
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddSingleton<PipeDiagnosticClient>();
        builder.Services.AddSingleton<ToolForwarder>();

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = McpServerMetadata.Name,
                    Version = McpServerMetadata.Version
                };
                options.ServerInstructions =
                    "LuminaUI.Diagnostics exposes local Avalonia diagnostics. Provide a pid or pipe parameter when calling target-specific tools.";
            })
            .WithStdioServerTransport()
            .WithTools<DiscoveryTools>()
            .WithTools<InspectionTools>()
            .WithTools<PropertyTools>()
            .WithTools<VisualTools>()
            .WithTools<InteractionTools>()
            .WithTools<ScrollTools>();
    }
}
