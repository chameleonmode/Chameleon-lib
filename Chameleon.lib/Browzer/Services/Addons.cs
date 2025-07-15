using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Chameleon.lib.Services;
using Chameleon.lib.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
namespace Chameleon.lib.Browzer.Services;

public class AddonsServer : IStartUp {
	public TaskCompletionSource<bool> Initialized { get; } = new();
	private WebApplication? app;

	public int Port { get; }
	public string RedirectUri { get; }
	public ConcurrentDictionary<string, (object config, int port, BrowserType bt)> AddonInstances { get; } = [];

	AddonsServer() {
		foreach (var port in new[] { 3663, 3993, 3693, 3963, 6969, 6996, 9669, 9696 }) {
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

	public async Task Init() {
		if (app != null) return;

		// builder configuration
		var builder = WebApplication.CreateBuilder();

		// Add minimal required services
		_ = builder.Services.AddEndpointsApiExplorer()
		 .AddCors(o => o.AddPolicy("AllowAnyOrigin", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

		// Configure to listen on all available interfaces, not just localhost
		_ = builder.WebHost.ConfigureKestrel(options => {
			options.Listen(IPAddress.Loopback, Port);
		});
		app = builder.Build();
		// Use minimal middleware
		_ = app.UseRouting().UseCors("AllowAnyOrigin");

		#region routes
		// Health check endpoint
		app.MapGet("/ping", () =>
			Results.Json(new { status = "ok", time = DateTime.Now })
		);

		app.MapGet("/init", ([FromQuery] string instanceId, [FromQuery] string sessionId) => {
			if (sessionId.Is()) return Results.BadRequest("Missing sessionId parameter");
			else if (AddonInstances.TryGetValue(sessionId, out var config)) return Results.Content(JSON.Serialize(config.Item1), "application/json");

			// Log the missing session for debugging
			Debug.WriteLine($"Session {sessionId} not found in AddonInstances. Available sessions: {string.Join(", ", AddonInstances.Keys)}");
			return Results.NotFound($"Session {sessionId} not found");
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
			try {
				var instanceId = int.TryParse(context.Request.Headers["X-Instance-ID"].ToString(), out var id) ? id : 0;
				var sessionId = context.Request.Headers["X-Session-ID"].ToString();
				var gotSession = AddonInstances.TryGetValue(sessionId, out var instance);
				IBrowserInstance? browser = null; // Ensure browser is initialized  
				if (gotSession) _ = Browzio.I.Browsers.TryGetValue((instance.bt, instanceId), out browser);
				return body.GetProperty("type").GetString() switch {
					"init" => gotSession ? Results.Json(new { instance.config, instance.port }) : Results.NotFound(new { error = "Session not found" }),
					"port" => Results.Ok(new { port = browser?.Settings.Profile.Port }),
					// @TODO: Implement proper handling for "init" type
					//"port" when body.TryGetProperty("port", out var ele) && ele.TryGetInt32(out var port) && browser != null =>
					//	Results.Ok(new { status = "ok", port = browser.Settings.Profile.Port = port }),
					"event" or "action" => Results.Ok(new { status = "ok" }),
					_ => Results.BadRequest(new { error = "Invalid type" })
				};
			} catch (Exception e) {
				return Results.BadRequest(new { error = "Invalid", e });
			}
		});

		#endregion

		// Start the server
		await app.StartAsync();

		// Wait for the server to be ready
		do await Task.Delay(100);
		while (await EX.Poly(async () => {
			// Wait for the server to be ready
			if (app == null) throw new InvalidOperationException("AddonsServer is not initialized");
			using var httpClient = new HttpClient();
			httpClient.Timeout = TimeSpan.FromMilliseconds(500);
			var response = await httpClient.GetAsync($"http://127.0.0.1:{Port}/ping");
			return response.EnsureSuccessStatusCode().StatusCode != HttpStatusCode.OK;
		}));
		// Signal that the server has started successfully
		Console.WriteLine($"AddonsServer started successfully on port {Port}");
		_ = Initialized.TrySetResult(true);
	}

	public async Task Stop() {
		if (app == null) return;
		await app.StopAsync();
		await app.DisposeAsync();
		app = null;
	}

	public static AddonsServer I { get; } = new();
}
