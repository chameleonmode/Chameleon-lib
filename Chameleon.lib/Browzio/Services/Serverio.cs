
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Chameleon.lib.Util;
using System.Net;
using System.Diagnostics;
using Chameleon.lib.ThirdParty.GeoIp;
namespace Chameleon.lib.Browzio.Services;

public class Serverio {
	private WebApplication? app;

	public int Port { get; }
	public string RedirectUri { get; }
	// @TODO: public ProxyHandler Proxio { get; } = new();

	public TaskCompletionSource<bool> Initialized { get; } = new();
	private readonly ConcurrentDictionary<(string sessionId, int instanceId), (object config, int port, BrowserType bt)> sessions = [];

	internal Serverio() {
		foreach (var port in new[] { 3663, 3993, 3693, 3963 }) { // ,6969, 6996, 9669, 9696
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

	public async Task AddSession(string sessionId, BrowserSetting settings) {
		// Make GeoIP call optional - use fallback values if it fails
		Ipapi ipapi;
		try {
			ipapi = await ThirdParty.GeoIp.Api.GeoIp(settings.Profile.Proxy.WebProxy);
		} catch (Exception ex) {
			// Log the error but continue with randomized fallback values
			Console.WriteLine($"GeoIP lookup failed: {ex.Message}. Using randomized fallback values.");
			ipapi = GenerateRandomGeoIpFallback();
		}
		// @TODO: Handle proxio configuration
		// settings.Proxio = settings.Profile.Proxy.WebProxy?.Address == null
		// 	? null 
		// 	: settings.Profile.Proxy.Credentials == null
		// 		? (host: settings.Profile.Proxy.WebProxy.Address.Host, port: settings.Profile.Proxy.WebProxy.Address.Port)
		// 		: (host: "127.0.0.1", port: Proxio.AssignProxy(settings));
		sessions[(sessionId, settings.Profile.Id)] = (
			config: new {
				proxy = new {
					enabled = settings.Profile.Proxy.WebProxy?.Address != null,
					scheme = settings.Profile.Proxy.WebProxy?.Address?.Scheme,
					host = settings.Profile.Proxy.WebProxy?.Address?.Host,
					port = settings.Profile.Proxy.WebProxy?.Address?.Port,
					username = settings.Profile.Proxy.Credentials?.UserName,
					password = settings.Profile.Proxy.Credentials?.Password,
				},
				urls = new {
					start = settings.Profile.StartPage,
					bookmarks = settings.Profile.Bookmarks,
				},
				tz = new {
					enabled = settings.Profile.Emulations.Timezone,
					ipapi.zone,
					ipapi.locale,
					ipapi.system,
				},
				geo = new {
					enabled = settings.Profile.Emulations.Geo,
    			accuracy = 64.0999,
					ipapi.lat,
					ipapi.lon,
				},
				navi = new { enabled = settings.Profile.Emulations.Navigator },
				canvas = new { enabled = settings.Profile.Emulations.Canvas },
				rects = new { enabled = settings.Profile.Emulations.Rects },
				audio = new { enabled = settings.Profile.Emulations.Audio },
				webgl = new { enabled = settings.Profile.Emulations.WebGL },
				fonts = new { enabled = settings.Profile.Emulations.Font },
			},
			port: settings.Port,
			bt: settings.BrowserType
		);
	}

	public async Task Init() {
		if (app != null) return;

		// builder configuration
		var builder = WebApplication.CreateBuilder();

		// Add minimal required services
		builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddCors(o => o.AddPolicy("AllowAnyOrigin", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

		// Configure to listen on all available interfaces, not just localhost
		builder.WebHost.ConfigureKestrel(options => {
			// Main API port
			// options.ListenLocalhost(5000);
			options.Listen(IPAddress.Loopback, Port);
		});
		app = builder.Build();
		// Use minimal middleware
		app.UseRouting()
			.UseCors("AllowAnyOrigin");

		#region routes
		// Health check endpoint
		app.MapGet("/ping", () =>
			Results.Json(new { status = "ok", time = DateTime.Now })
		);

		// Endpoint to get browser profile info
		app.MapGet("/init", ([FromQuery] int instanceId, [FromQuery] string sessionId) => {
			if (
				sessionId.Is() ||
				!sessions.TryGetValue((sessionId, instanceId), out var instance)
			) return Results.NotFound(new { error = "Session not found" });
			return Results.Content($@"
			<!DOCTYPE html>
			<html lang='en'>
			<head>
				<meta charset='UTF-8'>
				<meta name='viewport' content='width=device-width, initial-scale=1.0'>
				<title>Chameleon</title>
				<style>
					* {{
						margin: 0;
						padding: 0;
						box-sizing: border-box;
					}}
					
					body {{
						font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
						background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
						height: 100vh;
						display: flex;
						align-items: center;
						justify-content: center;
						color: white;
					}}
					
					.splash-container {{
						text-align: center;
						max-width: 600px;
						padding: 40px;
						background: rgba(255, 255, 255, 0.1);
						border-radius: 20px;
						backdrop-filter: blur(10px);
						border: 1px solid rgba(255, 255, 255, 0.2);
						box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
					}}
					
    		  @keyframes glow {{
            0%, 100% {{ filter: drop-shadow(0 0 10px rgba(255, 107, 107, 0.5)); }}
            25% {{ filter: drop-shadow(0 0 15px rgba(78, 205, 196, 0.5)); }}
            50% {{ filter: drop-shadow(0 0 20px rgba(69, 183, 209, 0.5)); }}
            75% {{ filter: drop-shadow(0 0 15px rgba(150, 206, 180, 0.5)); }}
        	}}
        
        	.logo {{
        	  font-size: 4rem;
        	  margin-bottom: 20px;
        	  animation: glow 3s ease infinite;
        	  cursor: pointer;
        	  transition: transform 0.3s ease;
        	}}
					
					.title {{
						font-size: 2.5rem;
						font-weight: 300;
						margin-bottom: 15px;
						opacity: 0.9;
					}}
					
					.subtitle {{
						font-size: 1.2rem;
						opacity: 0.7;
						margin-bottom: 30px;
					}}
					
					.browser-info {{
						background: rgba(255, 255, 255, 0.05);
						padding: 20px;
						border-radius: 10px;
						margin: 20px 0;
						border-left: 4px solid #4ecdc4;
					}}
					
					.browser-name {{
						font-size: 1.1rem;
						font-weight: 600;
						color: #4ecdc4;
						margin-bottom: 5px;
					}}
					
					.status {{
						font-size: 0.9rem;
						opacity: 0.8;
					}}
					
					.loading {{
						display: inline-block;
						width: 40px;
						height: 40px;
						border: 3px solid rgba(255, 255, 255, 0.3);
						border-radius: 50%;
						border-top-color: #4ecdc4;
						animation: spin 1s ease-in-out infinite;
						margin: 20px 0;
					}}
					
					@keyframes gradientShift {{
						0%, 100% {{ background-position: 0% 50%; }}
						50% {{ background-position: 100% 50%; }}
					}}
					
					@keyframes spin {{
						to {{ transform: rotate(360deg); }}
					}}
					
					.fade-in {{
						animation: fadeIn 0.8s ease-out;
					}}
					
					@keyframes fadeIn {{
						from {{ opacity: 0; transform: translateY(20px); }}
						to {{ opacity: 1; transform: translateY(0); }}
					}}
					
					.extensions-note {{
						font-size: 0.9rem;
						opacity: 0.6;
						margin-top: 20px;
						font-style: italic;
					}}
				</style>
			</head>
			<body>
				<div class='splash-container fade-in'>
					<div class='logo'>😎</div>
					<h1 class='title'>{(instance.bt == BrowserType.Firefox ? "Geckoleon" : "Chromeleon")}</h1>
					<p class='subtitle'>Advanced Browser Management</p>
					
					<div class='loading'></div>
					
					{(instance.bt == BrowserType.Vivaldi ?
						"<div class='extensions-note'>If the Chromeleon extension is not installed. While keeping this tab open." +
						" Right click this link to open it in a New Tab and flip the switch to enable Developer Mode. " +
						"Refresh the extension on that tab if this page persists<br />" +
						"<a href='chrome://extensions' target='_blank' rel='noopener noreferrer' style='color: #4ecdc4;' onclick='return false;' " +
						"onmousedown='window.open(this.href, \"_blank\", \"noopener,noreferrer\"); return false;'>chrome://extensions</a></div>"
						: "<div class='extensions-note'>Extensions and privacy features are being initialized...</div>")}
				</div>
				
				<script>
					// Auto-close after 3 seconds for non-Vivaldi browsers
					// {(instance.bt == BrowserType.Vivaldi ? @"setTimeout(function() {{
					// 	window.open('chrome://extensions', '_blank');
					// }}, 10000);" : "")}
					
					// Add some interactivity
					document.addEventListener('DOMContentLoaded', function() {{
						const container = document.querySelector('.splash-container');
						container.style.transition = 'transform 0.1s ease';
  					container.addEventListener('mouseenter', function() {{
  					  this.style.transform = 'scale(0.98)';
  					}});
  					container.addEventListener('mouseleave', function() {{
  					  this.style.transform = 'scale(1)';
  					}});
				}});
				</script>
			</body>
			</html>", "text/html");
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

	private static Ipapi GenerateRandomGeoIpFallback() {
		var random = new Random();
		
		// Array of realistic fallback locations
		var locations = new[] {
			new { country = "United States", countryCode = "US", city = "New York", lat = 40.7128, lon = -74.0060, timezone = "America/New_York" },
			new { country = "United States", countryCode = "US", city = "Los Angeles", lat = 34.0522, lon = -118.2437, timezone = "America/Los_Angeles" },
			new { country = "United States", countryCode = "US", city = "Chicago", lat = 41.8781, lon = -87.6298, timezone = "America/Chicago" },
			new { country = "United States", countryCode = "US", city = "Miami", lat = 25.7617, lon = -80.1918, timezone = "America/New_York" },
			new { country = "Canada", countryCode = "CA", city = "Toronto", lat = 43.6532, lon = -79.3832, timezone = "America/Toronto" },
			new { country = "Canada", countryCode = "CA", city = "Vancouver", lat = 49.2827, lon = -123.1207, timezone = "America/Vancouver" },
			new { country = "United Kingdom", countryCode = "GB", city = "London", lat = 51.5074, lon = -0.1278, timezone = "Europe/London" },
			new { country = "Germany", countryCode = "DE", city = "Berlin", lat = 52.5200, lon = 13.4050, timezone = "Europe/Berlin" },
			new { country = "France", countryCode = "FR", city = "Paris", lat = 48.8566, lon = 2.3522, timezone = "Europe/Paris" },
			new { country = "Australia", countryCode = "AU", city = "Sydney", lat = -33.8688, lon = 151.2093, timezone = "Australia/Sydney" }
		};

		var selectedLocation = locations[random.Next(locations.Length)];

		// Generate a random private IP address for the query field
		var privateIpRanges = new[] {
			"192.168", "10", "172.16", "172.17", "172.18", "172.19", "172.20"
		};
		var selectedRange = privateIpRanges[random.Next(privateIpRanges.Length)];
		string randomIp;
		
		if (selectedRange.StartsWith("192.168")) {
			randomIp = $"192.168.{random.Next(1, 255)}.{random.Next(1, 255)}";
		} else if (selectedRange == "10") {
			randomIp = $"10.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}";
		} else {
			var thirdOctet = random.Next(16, 32); // 172.16-31.x.x range
			randomIp = $"172.{thirdOctet}.{random.Next(1, 255)}.{random.Next(1, 255)}";
		}

		return new Ipapi {
			country = selectedLocation.country,
			countryCode = selectedLocation.countryCode,
			city = selectedLocation.city,
			lat = selectedLocation.lat,
			lon = selectedLocation.lon,
			timezone = selectedLocation.timezone,
			query = randomIp,
			system = true
		};
	}
}
// @TODO: Implement ProxyHandler
// public class ProxyHandler {
// 	private readonly ConcurrentDictionary<int, (BrowserSetting setting, CancellationTokenSource cts)> settings = [];

// 	public int AssignProxy(BrowserSetting setting) {
// 		var port = Processez.NextFreePort(33333);
// 		var listener = new TcpListener(IPAddress.Loopback, port);
// 		listener.Start();

// 		var cts = new CancellationTokenSource();
// 		settings[port] = (setting, cts);

// 		// Start accepting connections for this browser setting
// 		_ = Task.Run(async () => {
// 			while (!cts.Token.IsCancellationRequested) {
// 				try {
// 					// Accept a new client connection
// 					var client = await listener.AcceptTcpClientAsync();

// 					// Handle this connection in a separate task
// 					_ = Task.Run(async () => await HandleProxyConnection(client, setting, cts.Token));
// 				} catch (ObjectDisposedException) {
// 					break; // Listener was stopped
// 				} catch (Exception ex) {
// 					Debug.WriteLine($"Error accepting connection for profile {setting.Profile.Id}");
// 					EX.PrintException(ex);
// 				}
// 			}
// 		});

// 		return port;
// 	}

// 	public void Remove(BrowserSetting setting) {
// 		if(setting.Proxio?.port == null) return;
// 		if (settings.TryRemove(setting.Proxio.Value.port, out var value)) {
// 			value.cts.Cancel();
// 			value.cts.Dispose();
// 		}
// 	}

// 	private async Task HandleProxyConnection(TcpClient client, BrowserSetting settings, CancellationToken cancellationToken) {
// 		try {
// 			using (client) {
// 				var browserStream = client.GetStream();

// 				// Read the initial request
// 				var buffer = new byte[4096];
// 				var bytesRead = await browserStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
// 				if (bytesRead == 0) return;

// 				var request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
// 				var lines = request.Split(["\r\n"], StringSplitOptions.None);
// 				var firstLine = lines[0];

// 				if (firstLine.StartsWith("CONNECT")) {
// 					await HandleConnect(browserStream, firstLine, settings, cancellationToken);
// 				} else {
// 					await HandleHttpRequest(browserStream, request, bytesRead, buffer, settings, cancellationToken);
// 				}
// 			}
// 		} catch (Exception ex) {
// 			Debug.WriteLine($"Error handling proxy connection");
// 			EX.PrintException(ex);
// 		}
// 	}

// 	private async Task HandleConnect(NetworkStream browserStream, string connectLine, BrowserSetting settings, CancellationToken cancellationToken) {
// 		// Parse CONNECT request
// 		var parts = connectLine.Split(' ');
// 		if (parts.Length < 2) return;

// 		var hostPort = parts[1].Split(':');
// 		var targetHost = hostPort[0];
// 		var targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

// 		var proxy = settings.Profile.Proxy;
// 		if (proxy.WebProxy?.Address?.Host == null) return;

// 		try {
// 			// Connect to external proxy
// 			using var proxyClient = new TcpClient();
// 			await proxyClient.ConnectAsync(proxy.WebProxy.Address.Host, proxy.WebProxy.Address.Port);
// 			var proxyStream = proxyClient.GetStream();

// 			// Send CONNECT with auth to external proxy
// 			var authHeader = CreateProxyAuthorizationHeader(proxy.Credentials!.UserName, proxy.Credentials.Password);
// 			var proxyConnect = $"CONNECT {targetHost}:{targetPort} HTTP/1.1\r\n" +
// 											 $"Host: {targetHost}:{targetPort}\r\n" +
// 											 $"Proxy-Authorization: {authHeader}\r\n" +
// 											 $"Connection: keep-alive\r\n\r\n";

// 			var proxyConnectBytes = Encoding.ASCII.GetBytes(proxyConnect);
// 			await proxyStream.WriteAsync(proxyConnectBytes, 0, proxyConnectBytes.Length, cancellationToken);

// 			// Read proxy response
// 			var responseBuffer = new byte[4096];
// 			var responseBytes = await proxyStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, cancellationToken);
// 			var proxyResponse = Encoding.ASCII.GetString(responseBuffer, 0, responseBytes);

// 			if (proxyResponse.StartsWith("HTTP/1.1 200") || proxyResponse.StartsWith("HTTP/1.0 200")) {
// 				// Send 200 to browser
// 				var okResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
// 				await browserStream.WriteAsync(okResponse, 0, okResponse.Length, cancellationToken);

// 				// Relay data between browser and external proxy
// 				var relay1 = RelayDataAsync(browserStream, proxyStream, cancellationToken);
// 				var relay2 = RelayDataAsync(proxyStream, browserStream, cancellationToken);
// 				await Task.WhenAny(relay1, relay2);
// 			} else {
// 				// Send error to browser
// 				var errorResponse = Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
// 				await browserStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
// 			}
// 		} catch (Exception ex) {
// 			Debug.WriteLine($"CONNECT error");
// 			EX.PrintException(ex);
// 			var errorResponse = Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\n");
// 			await browserStream.WriteAsync(errorResponse, 0, errorResponse.Length, cancellationToken);
// 		}
// 	}

// 	private async Task HandleHttpRequest(NetworkStream browserStream, string initialRequest, int initialBytes, byte[] buffer, BrowserSetting settings, CancellationToken cancellationToken) {
// 		var proxy = settings.Profile.Proxy;
// 		if (proxy.WebProxy?.Address?.Host == null) return;

// 		try {
// 			using var proxyClient = new TcpClient();
// 			await proxyClient.ConnectAsync(proxy.WebProxy.Address.Host, proxy.WebProxy.Address.Port);
// 			var proxyStream = proxyClient.GetStream();

// 			// Modify request to add proxy auth
// 			var modifiedRequest = AddProxyAuthentication(initialRequest, proxy);
// 			var requestBytes = Encoding.ASCII.GetBytes(modifiedRequest);

// 			// Send the modified request to the proxy
// 			await proxyStream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken);

// 			// If there's additional data in the initial buffer, send it too
// 			if (initialBytes > initialRequest.Length) {
// 				var additionalData = initialBytes - initialRequest.Length;
// 				await proxyStream.WriteAsync(buffer, initialRequest.Length, additionalData, cancellationToken);
// 			}

// 			// Relay data between browser and proxy
// 			var relay1 = RelayDataAsync(browserStream, proxyStream, cancellationToken);
// 			var relay2 = RelayDataAsync(proxyStream, browserStream, cancellationToken);
// 			await Task.WhenAny(relay1, relay2);
// 		} catch (Exception ex) {
// 			Debug.WriteLine($"HTTP request error");
// 			EX.PrintException(ex);
// 		}
// 	}

// 	private string AddProxyAuthentication(string request, BrowserProxy proxy) {
// 		var lines = request.Split(["\r\n"], StringSplitOptions.None).ToList();
// 		var authHeader = $"Proxy-Authorization: {CreateProxyAuthorizationHeader(proxy.Credentials!.UserName, proxy.Credentials.Password)}";

// 		// Insert auth header before the empty line
// 		for (var i = 0; i < lines.Count; i++) {
// 			if (string.IsNullOrEmpty(lines[i])) {
// 				lines.Insert(i, authHeader);
// 				break;
// 			}
// 		}

// 		return string.Join("\r\n", lines);
// 	}

// 	private async Task RelayDataAsync(Stream from, Stream to, CancellationToken cancellationToken) {
// 		try {
// 			var buffer = new byte[4096];
// 			int bytesRead;
// 			while (!cancellationToken.IsCancellationRequested &&
// 						 (bytesRead = await from.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0) {
// 				await to.WriteAsync(buffer, 0, bytesRead, cancellationToken);
// 				await to.FlushAsync(cancellationToken);
// 			}
// 		} catch (Exception) when (cancellationToken.IsCancellationRequested) {
// 			// Expected when cancelling
// 		} catch (Exception ex) {
// 			Debug.WriteLine($"Relay error");
// 			EX.PrintException(ex);
// 		}
// 	}

// 	private string CreateProxyAuthorizationHeader(string username, string password) {
// 		var credentials = $"{username}:{password}";
// 		var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
// 		var base64Credentials = Convert.ToBase64String(credentialsBytes);
// 		return $"Basic {base64Credentials}";
// 	}
// }
