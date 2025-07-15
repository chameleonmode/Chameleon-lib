using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Chameleon.lib.Util;

#region windows

[StructLayout(LayoutKind.Sequential)]
public struct PROCESS_BASIC_INFORMATION {
	internal IntPtr Reserved1;
	internal IntPtr PebBaseAddress;
	internal IntPtr Reserved2_0;
	internal IntPtr Reserved2_1;
	internal IntPtr UniqueProcessId;
	internal IntPtr InheritedFromUniqueProcessId; // This is the Parent Process ID
}
/**
 * This is a subset of events from winuser.h.
 * See: https://docs.microsoft.com/en-us/windows/win32/winauto/event-constants
 */
public enum U32Events : uint {
	EVENT_MIN = 0x00000001,//WINEVENT_SKIPOWNTHREAD = 0x0001,
	EVENT_MAX = 0x7FFFFFFF,
	EVENT_SYSTEM_FOREGROUND = 0x0003,
	EVENT_SYSTEM_MENUSTART = 0x0004,//WINEVENT_INCONTEXT = 0x0004
	EVENT_SYSTEM_MENUEND = 0x0005,
	EVENT_SYSTEM_MENUPOPUPSTART = 0x0006,
	EVENT_SYSTEM_MENUPOPUPEND = 0x0007,
	EVENT_SYSTEM_CAPTURESTART = 0x0008,
	EVENT_SYSTEM_CAPTUREEND = 0x0009,
	EVENT_SYSTEM_MOVESIZESTART = 0x000A,
	EVENT_SYSTEM_MOVESIZEEND = 0x000B,
	EVENT_SYSTEM_CONTEXTHELPSTART = 0x000C,
	EVENT_SYSTEM_CONTEXTHELPEND = 0x000D,
	EVENT_SYSTEM_DRAGDROPSTART = 0x000E,
	EVENT_SYSTEM_DRAGDROPEND = 0x000F,
	EVENT_SYSTEM_DIALOGSTART = 0x0010,
	EVENT_SYSTEM_DIALOGEND = 0x0011,
	EVENT_SYSTEM_SCROLLINGSTART = 0x0012,
	EVENT_SYSTEM_SCROLLINGEND = 0x0013,
	EVENT_SYSTEM_SWITCHSTART = 0x0014,
	EVENT_SYSTEM_SWITCHEND = 0x0015,
	EVENT_SYSTEM_MINIMIZESTART = 0x0016,
	EVENT_SYSTEM_MINIMIZEEND = 0x0017,
	EVENT_SYSTEM_DESKTOPSWITCH = 0x0020,
	EVENT_SYSTEM_SWITCHER_APPGRABBED = 0x0024,
	EVENT_SYSTEM_SWITCHER_APPOVERTARGET = 0x0025,
	EVENT_SYSTEM_SWITCHER_APPDROPPED = 0x0026,
	EVENT_SYSTEM_SWITCHER_CANCELLED = 0x0027,
	EVENT_SYSTEM_IME_KEY_NOTIFICATION = 0x0029,
	EVENT_SYSTEM_END = 0x00FF,

	EVENT_OBJECT_IME_SHOW = 0x8027,
	EVENT_OBJECT_FOCUS = 0x8005,
	EVENT_OBJECT_DESTROY = 0x8001,
	EVENT_OBJECT_REORDER = 0x8004,
	EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
	EVENT_OBJECT_NAMECHANGE = 0x800C,

	WINEVENT_OUTOFCONTEXT = 0x0000,

	WINEVENT_SKIPOWNPROCESS = 0x0002
}

public enum ShowWindowCommands : int {
	SW_HIDE = 0,
	SW_NORMAL = 1,
	SW_SHOWMINIMIZED = 2,
	SW_MAXIMIZE = 3,
	SW_SHOWNOACTIVATE = 4,
	SW_SHOW = 5,
	SW_MINIMIZE = 6,
	SW_SHOWMINNOACTIVE = 7,
	SW_SHOWNA = 8,
	SW_RESTORE = 9,
	SW_SHOWDEFAULT = 10,
	SW_FORCEMINIMIZE = 11
}

[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT {
	public int length;
	public int flags;
	public ShowWindowCommands showCmd;
	public POINT ptMinPosition;
	public POINT ptMaxPosition;
	public RECT rcNormalPosition;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "<Pending>")]
[SupportedOSPlatform("windows")]
public static partial class U32 {
	#region delegates
	public delegate IntPtr MouseHookHandler(int nCode, uint wParam, IntPtr lParam);

	public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

	public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
	#endregion

	// Delegate for the WinEventProc callback
	public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[DllImport("user32.dll")]
	public static extern IntPtr SetWinEventHook(
			uint eventMin,
			uint eventMax,
			IntPtr hmodWinEventProc,
			WinEventDelegate lpfnWinEventProc,
			uint idProcess,
			uint idThread,
			uint dwFlags);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool UnhookWinEvent(IntPtr hWinEventHook);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial IntPtr SetActiveWindow(IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool IsWindow(IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr FindWindow(string lpClassName, string lpWindowName);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool SetForegroundWindow(IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[LibraryImport("kernel32.dll")]
	public static partial uint GetCurrentThreadId();

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

	[DllImport("ntdll.dll")]
	public static extern int NtQueryInformationProcess(
		IntPtr processHandle,
		int processInformationClass, // 0 for ProcessBasicInformation (PROCESSINFOCLASS enum)
		ref PROCESS_BASIC_INFORMATION processInformation,
		int processInformationLength,
		out int returnLength);
}

[SupportedOSPlatform("windows")]
public class WindowEventHandler(Action<nint>? onForeground = null, Action<nint>? onDestroy = null) {
	private readonly IntPtr[] hooks = [IntPtr.Zero, IntPtr.Zero];
	public async Task StartListening(int tries = 3) {
		var @delegate = new U32.WinEventDelegate((hWinEventHook, eventType, hwnd, idObject, idChild, dwEventThread, dwmsEventTime) => {
			if (eventType == (uint)U32Events.EVENT_SYSTEM_FOREGROUND) onForeground?.Invoke(hwnd);
			else if (eventType == (uint)U32Events.EVENT_OBJECT_DESTROY) onDestroy?.Invoke(hwnd);
		});
		IntPtr SetWinEventHook(uint eventType) =>
			 U32.SetWinEventHook(eventType, eventType, IntPtr.Zero, @delegate, 0, 0, (uint)U32Events.WINEVENT_OUTOFCONTEXT);
		hooks[0] = SetWinEventHook((uint)U32Events.EVENT_SYSTEM_FOREGROUND);
		hooks[1] = SetWinEventHook((uint)U32Events.EVENT_OBJECT_DESTROY);

		if (hooks.Any(hook => hook == IntPtr.Zero) && tries > 0) {
			// StopListening();
			await Task.Delay(1000);
			await StartListening(tries - 1);
		}
	}

	public void StopListening() {
		foreach (var hook in hooks.Where(hook => hook != IntPtr.Zero)) {
			_ = U32.UnhookWinEvent(hook);
		}
	}
}

[SupportedOSPlatform("windows")]
public static class U32til {
	public static IntPtr FindMainWindowHandle(int processId) {
		var foundWindow = IntPtr.Zero;
		_ = U32.EnumWindows((hWnd, lParam) => {
			_ = U32.GetWindowThreadProcessId(hWnd, out var windowProcessId);
			if (windowProcessId == processId) {
				foundWindow = hWnd;
				return false; // Stop enumeration
			}
			return true; // Continue enumeration
		}, IntPtr.Zero);
		return foundWindow;
	}

	public static Process GetProcessByMainWindowHandle(IntPtr mainWindowHandle) {
		_ = U32.GetWindowThreadProcessId(mainWindowHandle, out var processId);
		return processId == 0
			? throw new InvalidOperationException("Unable to get process ID from window handle.")
			: Process.GetProcessById((int)processId);
	}

	public static bool BringWindowToForeground(IntPtr hWnd) {
		// Check if the window handle is valid
		if (hWnd == IntPtr.Zero) {
			return false;
		}

		// Get the thread ID of the target window
		var targetThreadId = U32.GetWindowThreadProcessId(hWnd, out _);
		var currentThreadId = U32.GetCurrentThreadId();

		// Attach the current thread's input to the target window's thread
		var threadInputAttached = false;
		if (targetThreadId != currentThreadId) {
			threadInputAttached = U32.AttachThreadInput(currentThreadId, targetThreadId, true);
		}

		try {
			// Check if the window is maximized
			var placement = new WINDOWPLACEMENT();
			placement.length = Marshal.SizeOf(placement);
			if (!U32.GetWindowPlacement(hWnd, ref placement)) {
				return false;
			}

			var wasMinimized = placement.showCmd == ShowWindowCommands.SW_SHOWMINIMIZED;

			if (wasMinimized) {
				// Restore the window if it's minimized
				_ = U32.ShowWindow(hWnd, (int)ShowWindowCommands.SW_RESTORE);
			}

			// Set the window as the foreground window
			var result = U32.SetForegroundWindow(hWnd);

			// If setting the foreground window fails, try again after a short delay
			if (!result) {
				Thread.Sleep(100);
				result = U32.SetForegroundWindow(hWnd);
			}

			// Maximize the window if it was maximized before
			//if (wasMaximized) {
			//	_ = U32.ShowWindow(hWnd, (int)ShowWindowCommands.SW_MAXIMIZE);
			//}

			return result;
		} finally {
			// Detach the thread input if it was attached
			if (threadInputAttached) {
				_ = U32.AttachThreadInput(currentThreadId, targetThreadId, false);
			}
		}
	}   // Gets the parent process ID for a given process.
			// Returns -1 if the parent process ID cannot be determined (e.g., process has exited, access denied, or other error).
	public static int ParentProcessId(this Process process) {
		(process == null || process.HasExited || process.Handle == IntPtr.Zero).ThrowTrue();

		var pbi = new PROCESS_BASIC_INFORMATION();
		var sizeOfPbi = Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
		var status = U32.NtQueryInformationProcess(process.Handle, 0, ref pbi, sizeOfPbi, out var _);
		(status == 0).ThrowFalse();

		return pbi.InheritedFromUniqueProcessId.ToInt32();
	}
}
// workaround LiteDB compatibility issue in RECT data structure
[StructLayout(LayoutKind.Sequential)]
public struct POINT(int x, int y) {
	public int X = x;
	public int Y = y;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT {
	public int Left { get; set; }
	public int Top { get; set; }
	public int Right { get; set; }
	public int Bottom { get; set; }

	public readonly int Height {
		get {
			return Bottom - Top;
		}
	}
	public readonly int Width {
		get {
			return Right - Left;
		}
	}

	public override readonly string ToString() {
		return string.Format("({0}, {1}), {2} x {3}", Left, Top, Width, Height);
	}
}

#endregion

#region mac

public class MacOSWindowListener {
	public static MacOSWindowListener Instance { get; } = new MacOSWindowListener();

	public event Action<int>? WindowForegroundChanged;
	private readonly System.Timers.Timer pollingTimer;
	private readonly List<int> targetPids = [];

	public MacOSWindowListener() {
		pollingTimer = new System.Timers.Timer(1000); // Poll every second
		pollingTimer.Elapsed += OnPollingTimerElapsed;
	}

	private async void OnPollingTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) {
		var fgPid = await Task.Run(MacOSUtil.GetWindowForeground);
		if (fgPid.HasValue && targetPids.Contains(fgPid.Value))
			WindowForegroundChanged?.Invoke(fgPid.Value);
	}
	
	public void AddPid(int pid) {
		if (!targetPids.Contains(pid)) targetPids.Add(pid);
		if (targetPids.Count >= 1) pollingTimer.Start();
	}

	public void RemPid(int? pid) {
		if (!OperatingSystem.IsMacOS()) return;
		if (pid is int id) _ = targetPids.Remove(id);
		if (targetPids.Count == 0) pollingTimer.Stop();
	}
}

internal enum NSApplicationActivateOptions : uint {
	ActivatingIgnoringOtherApps = 1 << 0
}

public static class MacOSUtil {
	public static bool SetForegroundWindow(int pid) {
		return EX.Catch(() => {
			var windowId = FindWindowByPID(pid);
			return windowId.HasValue && BringWindowToForeground(pid);
		});
	}

	private static IntPtr GetWindowList() {
		return MacOSInterop.CGWindowListCopyWindowInfo(0x00000001, 0);
	}

	public static int? FindWindowByPID(int pid) {
		var windowListInfo = GetWindowList();
		if (windowListInfo == IntPtr.Zero)
			return null;

		using var windowList = new CFArray(windowListInfo);
		for (var i = 0; i < windowList.Count; i++) {
			var dict = new CFDictionary(windowList[i]);
			if (dict.ContainsKey("kCGWindowOwnerPID") && dict.GetInt32Value("kCGWindowOwnerPID") == pid) {
				return dict.GetInt32Value("kCGWindowNumber");
			}

		}

		return null;
	}

	private static bool BringWindowToForeground(int pid) {
		var nsRunningApplicationClass = ObjectiveCRuntime.ObjCGetClass("NSRunningApplication");
		var runningApp = ObjectiveCRuntime.ObjCMsgSend(nsRunningApplicationClass, ObjectiveCRuntime.SelRegisterName("runningApplicationWithProcessIdentifier:"), new IntPtr(pid));

		if (runningApp != IntPtr.Zero) {
			_ = ObjectiveCRuntime.ObjCMsgSend(runningApp,
					ObjectiveCRuntime.SelRegisterName("activateWithOptions:"),
					new IntPtr((int)NSApplicationActivateOptions.ActivatingIgnoringOtherApps));
			return true;
		} else {
			Console.WriteLine("Failed to find running application with specified PID.");
			return false;
		}
	}

	public static int? GetWindowForeground() {
		var windowListInfo = GetWindowList(); // Get list of all windows
		if (windowListInfo == IntPtr.Zero)
			return null;

		using var windowList = new CFArray(windowListInfo);
		for (var i = 0; i < windowList.Count; i++) {
			var dict = new CFDictionary(windowList[i]);
			if (dict.ContainsKey("kCGWindowOwnerPID")) {
				// Check if the window's layer is 0, indicating it is the frontmost window
				var layer = dict.GetInt32Value("kCGWindowLayer");
				if (layer == 0) {
					return dict.GetInt32Value("kCGWindowOwnerPID"); // Window is in the foreground
				}
			}
		}

		return null; // Window is not in the foreground
	}
}

internal static partial class MacOSInterop {
	// Import Quartz functions for window manipulation
	[LibraryImport("/System/Library/Frameworks/Quartz.framework/Quartz")]
	internal static partial IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

	[LibraryImport("/System/Library/Frameworks/Quartz.framework/Quartz")]
	internal static partial void CFRelease(IntPtr cfRef);
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "<Pending>")]
public class ObjectiveCRuntime {
	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName", CharSet = CharSet.Unicode)]
	public static extern IntPtr SelRegisterName(string name);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass", CharSet = CharSet.Unicode)]
	public static extern IntPtr ObjCGetClass(string name);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	public static extern IntPtr ObjCMsgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
}

internal partial class MacOSWindowManipulator {
	[LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	internal static partial IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

	public static IntPtr GetWindowInfo(int processId) {
		return CGWindowListCopyWindowInfo(0, (uint)processId);
	}
}

public partial class CFArray(IntPtr array) : IDisposable {
	private IntPtr _array = array;

	public int Count => CFArrayGetCount(_array);

	public IntPtr this[int index] => CFArrayGetValueAtIndex(_array, index);

	public void Dispose() {
		if (_array != IntPtr.Zero) {
			MacOSInterop.CFRelease(_array);
			_array = IntPtr.Zero;
		}
		GC.SuppressFinalize(this);
	}

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	internal static partial int CFArrayGetCount(IntPtr array);

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	internal static partial IntPtr CFArrayGetValueAtIndex(IntPtr array, int index);
}

public partial class CFDictionary(IntPtr dict) {
	private readonly IntPtr _dict = dict;

	public bool ContainsKey(string key) {
		var cfKey = CFString.Create(key);
		return CFDictionaryContainsKey(_dict, cfKey);
	}

	public int GetInt32Value(string key) {
		var cfKey = CFString.Create(key);
		var value = CFDictionaryGetValue(_dict, cfKey);
		return CFNumber.ToInt32(value);
	}

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	internal static partial IntPtr CFDictionaryGetValue(IntPtr dict, IntPtr key);

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool CFDictionaryContainsKey(IntPtr dict, IntPtr key);
}

public static partial class CFString {
	public static IntPtr Create(string str) {
		return CFStringCreateWithCString(IntPtr.Zero, str, 0x08000100); // kCFStringEncodingUTF8
	}

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, int encoding);
}

public static partial class CFNumber {
	public static int ToInt32(IntPtr number) {
		return number == IntPtr.Zero
			? throw new ArgumentNullException(nameof(number))
			: !CFNumberGetValue(number, 9, out var value)
			? throw new InvalidOperationException("Could not convert CFNumber to Int32.")
			: value;
	}

	[LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool CFNumberGetValue(IntPtr number, int theType, out int value);
}

public class MacFileVersionInfo {
  MacFileVersionInfo() { }

  public string? FilePath { get; private set; }
  public string? ProductVersion { get; private set; }
  public string? BuildVersion { get; private set; }
  public string? BundleIdentifier { get; private set; }
  public string? ProductName { get; private set; }

  public static MacFileVersionInfo GetVersionInfo(string filePath) {
    var info = new MacFileVersionInfo {
      FilePath = filePath
    };

    if (Directory.Exists(filePath) && filePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) {
      // Handle application bundles
      var plistPath = Path.Combine(filePath, "Contents", "Info.plist");
      if (File.Exists(plistPath)) {
        info.LoadFromPlist(plistPath);
      }
    } else if (File.Exists(filePath)) {
      // Handle regular files using mdls
      info.LoadFromMdls();
    }

    return info;
  }

  private void LoadFromPlist(string plistPath) {
    ProductVersion = ExecutePlistBuddy(plistPath, "CFBundleShortVersionString");
    BuildVersion = ExecutePlistBuddy(plistPath, "CFBundleVersion");
    BundleIdentifier = ExecutePlistBuddy(plistPath, "CFBundleIdentifier");
    ProductName = ExecutePlistBuddy(plistPath, "CFBundleName");
  }

  private void LoadFromMdls() {
    // For non-app files, try to get metadata using mdls
    var output = ExecuteCommand("mdls", $"\"{FilePath}\"");

    // Parse the output to extract relevant metadata
    // This is simplified and might need enhancement for specific cases
    foreach (var line in output.Split('\n')) {
      if (line.Contains("kMDItemVersion")) {
        ProductVersion = ExtractValue(line);
      }
    }
  }

  private static string ExtractValue(string line) {
    var equalsPos = line.IndexOf('=');
    if (equalsPos > 0 && equalsPos < line.Length - 1) {
      var value = line[(equalsPos + 1)..].Trim();
      // Remove quotes if present
      if (value.StartsWith('\"') && value.EndsWith('\"')) {
        value = value[1..^1];
      }
      return value;
    }
    return string.Empty;
  }

  private static string ExecutePlistBuddy(string plistPath, string property) {
    return ExecuteCommand("/usr/libexec/PlistBuddy", $"-c \"Print {property}\" \"{plistPath}\"").Trim();
  }

  private static string ExecuteCommand(string command, string arguments) {
		using var process = Process.Start(new ProcessStartInfo {
      FileName = command,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    });
		if (process == null)
			return string.Empty;

		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		return output;
	}
}

#endregion