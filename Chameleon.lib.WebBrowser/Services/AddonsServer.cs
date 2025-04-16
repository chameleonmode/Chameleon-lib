using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;
using Chameleon.lib.Const;
using Chameleon.lib.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Chameleon.lib.WebBrowser.Services;
public class AddonsServer : IStartUp {
  private WebApplication? app;

  public int Port { get; } 
  public string RedirectUri { get; }
  public ConcurrentDictionary<string, object> AddonInstances { get; } = [];

  public bool IsRunning => app != null;

  AddonsServer() {
    foreach (var port in new [] { 3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696 }) {
      try {
        // Create a listener to check if the port is available
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        Port = port;
        break;
      } catch (SocketException) {
        // Port is in use, try the next one
      }
    }

    RedirectUri = $"http://127.0.0.1:{Port}/callback";
  }

  public async Task WaitListener() {
    using var listener = new HttpListener();
    listener.Prefixes.Add(RedirectUri + "/");
    listener.Start();

    var context = await listener.GetContextAsync();

    // Send response after extracting the code
    using var response = context.Response;
    response.ContentType = "application/json";
    var queryParams = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
    var sessionId = queryParams["sessionId"];
    if (string.IsNullOrEmpty(sessionId) || !AddonInstances.TryGetValue(sessionId, out var instance)) {
      response.StatusCode = 400;
      var errorJson = JsonSerializer.SerializeToUtf8Bytes(new { error = "Invalid or missing sessionId" });
      await response.OutputStream.WriteAsync(errorJson);
      return;
    }
    var json = JS.Serialize(instance);
    var jsonBytes = Encoding.UTF8.GetBytes(json);
    await response.OutputStream.WriteAsync(jsonBytes);
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

      // Add CORS services
      _ = builder.Services.AddCors(options => {
        options.AddPolicy("AllowAnyOrigin", policy => {
          _ = policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
      });

      // Configure to listen on all available interfaces, not just localhost
      _ = builder.WebHost.ConfigureKestrel(options => {
				options.Listen(IPAddress.Loopback, Port);
			});
      app = builder.Build();
			// Use minimal middleware
			_ = app.UseRouting()
				     .UseCors("AllowAnyOrigin");

      #region routes
      // Health check endpoint
      app.MapGet("/ping", () => 
        Results.Json(new { status = "ok", time = DateTime.Now })
      );

      app.MapGet("/init", ([FromQuery] string instanceId, [FromQuery] string sessionId) => {
        return $"{JS.Serialize(AddonInstances[sessionId])}";
      });

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
          Debug.WriteLine($"Received data from instance {instanceId} with session {sessionId}");

          return Results.Json(type switch {
            "init" => new { config = AddonInstances[sessionId!] },
            "event" => new { status = "ok", message = "Event received" },
            "action" => new { status = "ok", message = "Action received" },
            _ => new { status = "error", message = "Invalid type" }
          });
        } catch(Exception e) {
          return Results.BadRequest(new { error = "Invalid", e});
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
          var response = await httpClient.GetAsync($"http://127.0.0.1:{Port}/ping");
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
