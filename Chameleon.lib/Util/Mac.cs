using System.Runtime.InteropServices;
using Chameleon.lib.Util;

namespace Chameleon.lib.Common.Util.Mac;

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
		if (pid is int id) _ = targetPids.Remove(id);
		if (targetPids.Count == 0) pollingTimer.Stop();
	}
}

internal enum NSApplicationActivateOptions : uint {
	ActivatingIgnoringOtherApps = 1 << 0
}

public static class MacOSUtil {
	public static bool SetForegroundWindow(int pid) {
		return Exceptionz.Catch(() => {
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
