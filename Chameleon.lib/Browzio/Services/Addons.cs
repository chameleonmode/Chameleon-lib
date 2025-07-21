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
namespace Chameleon.lib.Browzio.Services;

public class AddonsServer : IStartUp {
	private WebApplication? app;

	public TaskCompletionSource? IsBusy { get; private set; }

	public int Port { get; }
	public string RedirectUri { get; }

	public TaskCompletionSource<bool> Initialized { get; } = new();
	private readonly ConcurrentDictionary<(string sessionId, int instanceId), (object config, int port, BrowserType bt)> sessions = [];

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

	public void AddSession(string sessionId, BrowserSetting settings, object config) {
		IsBusy = new TaskCompletionSource();
		sessions[(sessionId, settings.Profile.Id)] = (config, settings.Port, settings.BrowserType);
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

		app.MapGet("/init", ([FromQuery] int instanceId, [FromQuery] string sessionId) => {
			if (
				sessionId.Is() ||
				!sessions.TryGetValue((sessionId, instanceId), out var instance)
			) return Results.NotFound(new { error = "Session not found" });
			else return Results.Content(JSON.Serialize(instance.config), "application/json");
		});

		// endpoint to receive data from extensions
		app.MapPost("/app/data", (HttpContext context, [FromBody] JsonElement body) => {
			try {
				var instanceId = int.Parse(context.Request.Headers["X-Instance-ID"].ToString());
				var sessionId = context.Request.Headers["X-Session-ID"].ToString();
				return body.GetProperty("type").GetString() switch {
					"init" => sessions.TryGetValue((sessionId, instanceId), out var instance)
						? Results.Json(new { instance.config, instance.port }) 
						: Results.Json(new { error = "Session not found" }),
					// @TODO: Implement proper handling for "port" type
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
