using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace Chameleon.lib.Util;
/// <summary>
/// Utility Class	to check network
/// </summary>
public static class TcpUtil {
	/// <summary>
	/// Check if a port is free
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static bool IsFree(int port) {
		var properties = IPGlobalProperties.GetIPGlobalProperties();
		var listeners = properties.GetActiveTcpListeners();
		var openPorts = listeners.Select(item => item.Port).ToArray<int>();
		return openPorts.All(openPort => openPort != port);
	}

	/// <summary>
	/// Get the next free port
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static int NextFreePort(int port = 0, int max = 99999) {
		port = (port > 0) ? port : new Random().Next(1, 65535);
		while (!IsFree(port)) {
			port += 1;
			if (port > max)
				throw new Exception("No free ports available");
		}
		return port;
	}

	/// <summary>
	/// Get a random unused port
	/// </summary>
	public static int GetRandomUnusedPort() {
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}
}
