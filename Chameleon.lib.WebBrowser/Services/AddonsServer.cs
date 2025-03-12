using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Chameleon.lib.WebBrowser.Services;
public class AddonsServer {
  private WebApplication? app;

  public ConcurrentDictionary<string, object> AddonInstances { get; } = [];
  public int Port { get; } = new[] { 3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696 }.FirstOrDefault(port => {
    try {
      // Create a listener to check if the port is available
      var listener = new TcpListener(IPAddress.Loopback, port);
      listener.Start();
      listener.Stop();

      // If we get here, the port is available
      return true;
    } catch (SocketException) {
      // Port is in use, try the next one
      return false;
    }
  });

  public bool IsRunning => app != null;

  AddonsServer() {
    if (Port == 0) {
      throw new InvalidOperationException("No available port found to start the AddonsServer");
    }
  }

  public async Task Start() {
    if (IsRunning) return;

    // Create a TaskCompletionSource to signal when the server is ready
    var serverStarted = new TaskCompletionSource<bool>();

    _ = Task.Factory.StartNew(async () => {
      // builder configuration
      var builder = WebApplication.CreateBuilder();

			// Add minimal required services
			_ = builder.Services.AddEndpointsApiExplorer();

			// Configure to listen on all available interfaces, not just localhost
			_ = builder.WebHost.ConfigureKestrel(options => {
				options.Listen(IPAddress.Any, Port);
			});
      app = builder.Build();
      // Use minimal middleware
      app.UseRouting();

      #region routes
      // Health check endpoint
      app.MapGet("/ping", () => 
        Results.Json(new { status = "ok", time = DateTime.Now })
      );

      // Get application state endpoint
      app.MapGet("/app/state", () => 
        Results.Json(new {
          appName = "Chameleon",
          version = "1.0.0",
          status = "running",
          timestamp = DateTime.Now
        })
      );

      // endpoint to receive data from extensions
      app.MapPost("/app/data", (HttpContext context, [FromBody] JsonElement body) => {
        // Extract launch information from headers
        var instanceId = context.Request.Headers["X-Instance-ID"];
        var sessionId = context.Request.Headers["X-Session-ID"];

        try {
					var type = body.GetProperty("type").GetString();
          if(body.TryGetProperty("data", out var data)){
            // TODO: Handle the data
          }

          return Results.Json(type switch {
            "init" => new { config = AddonInstances[sessionId!] },
            "event" => new { status = "ok", message = "Event received" },
            "action" => new { status = "ok", message = "Action received" },
            _ => new { status = "error", message = "Invalid type" }
          });
        } catch {
          return Results.BadRequest(new { error = "Invalid JSON" });
        }
      });

      #endregion

      // Start the server
      await app.StartAsync();
      do {
        await Task.Delay(1000);
        try {
          using var httpClient = new HttpClient();
          httpClient.Timeout = TimeSpan.FromMilliseconds(500);
          var response = await httpClient.GetAsync($"http://localhost:{Port}/ping");
          _ = response.EnsureSuccessStatusCode();
          break;
        } catch (Exception e) {
          // Server not yet responding, wait and retry
          Console.WriteLine($"AddonsServer not yet responding: {e.Message}");
          continue;
        }
      } while (IsRunning);

      // Signal that the server has started successfully
      _ = serverStarted.TrySetResult(true);
      Console.WriteLine($"AddonsServer started successfully on port {Port}");
    }, TaskCreationOptions.LongRunning);

		// Wait for the server to start
		_ = await serverStarted.Task;
  }

  public async Task Stop() {
    if (IsRunning) {
      await app!.StopAsync();
      await app.DisposeAsync();
      app = null;
    }
  }

  public static AddonsServer Instance { get; } = new();
}
