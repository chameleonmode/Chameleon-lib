using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Chameleon.lib.Const;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Chameleon.lib.WebBrowser.Services;
public class AddonsServer {
  private WebApplication? app;

  public ConcurrentDictionary<string, object> AddonInstances { get; } = [];

  public int Port { get; }
  public bool IsRunning => app != null;

  AddonsServer() {
    Port = FindFreePort();
  }

  public async Task Start() {
    if (IsRunning) return;

    // Create a TaskCompletionSource to signal when the server is ready
    var serverStarted = new TaskCompletionSource<bool>();

    // Start the server on a background thread
    
    _ = Task.Factory.StartNew(async () => {
      // builder configuration
      var builder = WebApplication.CreateBuilder();

      // Add minimal required services
      builder.Services.AddEndpointsApiExplorer();

      // Configure to listen on all available interfaces, not just localhost
      builder.WebHost.ConfigureKestrel(options => {
        options.Listen(IPAddress.Any, Port);
      });

      app = builder.Build();

      // Use minimal middleware
      app.UseRouting();

      // Health check endpoint
      app.MapGet("/ping", () => Results.Json(new { status = "ok", time = DateTime.Now }));

      // Get application state endpoint
      app.MapGet("/app/state", () => Results.Json(new {
        appName = "Avalonia App",
        version = "1.0.0",
        status = "running",
        timestamp = DateTime.Now
      }));

      // API endpoint to receive data from extensions
      app.MapPost("/app/data", async (HttpContext context) => {
        // Extract launch information from headers
        var instanceId = context.Request.Headers["X-Instance-ID"];
        var sessionId = context.Request.Headers["X-Session-ID"];

        // Read and parse the request body
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        try {
          var data = JS.DeserializeSafely<object>(body);
          return Results.Json(new { data = AddonInstances[sessionId!] });
        } catch {
          return Results.BadRequest(new { error = "Invalid JSON" });
        }
      });

      // Start the server
      await app.StartAsync();

			// Signal that the server has started successfully
			_ = serverStarted.TrySetResult(true);

      Console.WriteLine($"AddonsServer started successfully on port {Port}");
    }, TaskCreationOptions.LongRunning);

		// Wait for the server to start
		_ = await serverStarted.Task;
    
    // Now verify the server is actually responding by pinging it
    var isResponding = false;
    var maxRetries = 10;
    var retryCount = 0;
    
    while (!isResponding && retryCount < maxRetries) {
        try {
				using var httpClient = new HttpClient();
				httpClient.Timeout = TimeSpan.FromSeconds(1);
				var response = await httpClient.GetAsync($"http://localhost:{Port}/ping");
				if (response.IsSuccessStatusCode) {
					isResponding = true;
					Console.WriteLine("AddonsServer is responding to ping requests!");
				}
			}
        catch (Exception) {
            // Server not yet responding, wait and retry
            await Task.Delay(500);
        }
        
        retryCount++;
    }
    
    if (!isResponding) {
        app = null;
        throw new TimeoutException($"AddonsServer started but is not responding on port {Port} after {maxRetries} attempts");
    }
  }

  public async Task Stop() {
    if (app != null) {
      await app.StopAsync();
      await app.DisposeAsync();
      app = null;
    }
  }

  private int FindFreePort() {
    // List of ports that the Chrome extension will check
    int[] candidatePorts = [5016, 5031, 7034, 8032, 8084, 9027];

    foreach (var port in candidatePorts) {
      try {
        // Create a listener to check if the port is available
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();

        // If we get here, the port is available
        return port;
      } catch (SocketException) {
        // Port is in use, try the next one
        continue;
      }
    }
    throw new Exception("No free ports available");
  }

  public void Dispose() {
    if (app != null) {
      app.DisposeAsync().AsTask().Wait();
      app = null;
    }
  }

  public static AddonsServer Instance { get; } = new();
}
