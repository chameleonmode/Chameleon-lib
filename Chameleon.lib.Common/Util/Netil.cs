using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Util;
/// <summary>
/// Utility Class	to check network
/// </summary>
public static class Netil {
		/// <summary>
		/// Check if a port is free
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public static bool IsFree(int port)
  {
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
		public static int NextFreePort(int port = 0)
  {
    port = (port > 0) ? port : new Random().Next(1, 65535);
    while (!IsFree(port))
    {
      port += 1;
    }
    return port;
  }
}
