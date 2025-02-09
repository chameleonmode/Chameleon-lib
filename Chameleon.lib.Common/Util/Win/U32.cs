using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Extensions;

namespace Chameleon.lib.Common.Util.Win;
/**
 * This is a subset of events from winuser.h.
 * See: https://docs.microsoft.com/en-us/windows/win32/winauto/event-constants
 */
public enum User32Events : uint {
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
	public delegate IntPtr MouseHookHandler(
			int nCode, uint wParam, IntPtr lParam);

	public delegate bool MonitorEnumDelegate(
			IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

	public delegate bool EnumWindowsProc(
			IntPtr hWnd, IntPtr lParam);
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
	public static partial bool UnhookWinEvent(
			IntPtr hWinEventHook);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial IntPtr SetActiveWindow(
			IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool IsWindow(
			IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr FindWindow(
			string lpClassName, string lpWindowName);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial uint GetWindowThreadProcessId(
			IntPtr hWnd, out uint lpdwProcessId);

	[LibraryImport("user32.dll", SetLastError = true)]
	public static partial IntPtr GetWindow(
			IntPtr hWnd, uint uCmd);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool SetForegroundWindow(
			IntPtr hWnd);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool EnumWindows(
			EnumWindowsProc lpEnumFunc, IntPtr lParam);

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
}

[SupportedOSPlatform("windows")]
public class WindowEventHandler {
	private IntPtr _hook;
	private U32.WinEventDelegate? _delegate;

	public event Action<nint>? OnForeground;
	public event Action<nint>? OnDestroy;

	public void StartListening(int tries = 0)
	{
		_delegate = new U32.WinEventDelegate(WinEventProc);
		_hook = U32.SetWinEventHook(
				(uint)User32Events.EVENT_SYSTEM_FOREGROUND,
				(uint)User32Events.EVENT_SYSTEM_FOREGROUND,
				IntPtr.Zero,
				_delegate,
				0,
				0,
				(uint)User32Events.WINEVENT_OUTOFCONTEXT);

		_ = U32.SetWinEventHook(
				(uint)User32Events.EVENT_OBJECT_DESTROY,
				(uint)User32Events.EVENT_OBJECT_DESTROY,
				IntPtr.Zero,
				_delegate,
				0,
				0,
				(uint)User32Events.WINEVENT_OUTOFCONTEXT);

		if (_hook == IntPtr.Zero && tries < 3) {
			StartListening(tries++);
		}
	}

	public void StopListening()
	{
		if (_hook != IntPtr.Zero) {
			_ = U32.UnhookWinEvent(_hook);
			_hook = IntPtr.Zero;
		}
	}

	private void WinEventProc(IntPtr hWinEventHook, uint eventType,
			IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		switch (eventType) {
			case (uint)User32Events.EVENT_SYSTEM_FOREGROUND:
				OnForeground?.Invoke(hwnd);
				break;

			case (uint)User32Events.EVENT_OBJECT_DESTROY:
				OnDestroy?.Invoke(hwnd);
				break;

			default:
				break;
		}
	}
}

[SupportedOSPlatform("windows")]
public static class U32til {
	public static IntPtr FindMainWindowHandle(int processId)
	{
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

	public static Process GetProcessByMainWindowHandle(IntPtr mainWindowHandle)
	{
		_ = U32.GetWindowThreadProcessId(mainWindowHandle, out var processId);
		return processId == 0
			? throw new InvalidOperationException("Unable to get process ID from window handle.")
			: Process.GetProcessById((int)processId);
	}

	public static bool BringWindowToForeground(IntPtr hWnd)
	{
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
	}
}

[SupportedOSPlatform("windows")]
public class MWHandleTrackerUtility(Process aprocess, Enums.SystemBrowserType systemBrowserType, CancellationTokenSource cts) {
	private readonly List<int> _childProcessIds = [];

	private IntPtr _mainWindowHandle = IntPtr.Zero;
	private TaskCompletionSource<Tuple<IntPtr, Process?>> _tcs = new();

	private Process _process = aprocess ?? throw new ArgumentNullException(nameof(aprocess));

	public void StartTracking()
	{
		new Thread(() => TrackMainWindowHandle(cts.Token)) { IsBackground = true }.Start();
	}

	private void TrackMainWindowHandle(CancellationToken token)
	{
		while (!token.IsCancellationRequested) {
			try {
				if (_process.HasExited) {
					_tcs.SetResult(new(0, null));
					break;
				}
				if (_mainWindowHandle == IntPtr.Zero) {
					if (systemBrowserType == Enums.SystemBrowserType.Firefox) {
						Thread.Sleep(500);
						var currentProcesses = Process.GetProcessesByName(
								systemBrowserType == Enums.SystemBrowserType.Firefox ? "firefox"
								: systemBrowserType == Enums.SystemBrowserType.Chrome ? "chrome"
								: "chrome").Where(p => p.Id != 0);
						foreach (var p in currentProcesses) {
							if (!_childProcessIds.Contains(p.Id) && p.ParentProcessId() == _process.Id) {
								_childProcessIds.Add(p.Id);
								var childProcess = Process.GetProcessById(p.Id);
								if (childProcess != null && !childProcess.HasExited) {
									var thishandle = U32til.FindMainWindowHandle(childProcess.Id);
									if (U32.IsWindow(thishandle)) {
										_process = childProcess;
										break;
									}
								}
							}
						}
					}
				}

				var handle = U32til.FindMainWindowHandle(_process.Id);
				if (handle != _mainWindowHandle && U32.IsWindow(handle)) {
					_mainWindowHandle = handle;
					var tcs = _tcs;
					_tcs = new();
					tcs.SetResult(new(_mainWindowHandle, _process));
					break;
				}

			} catch (Exception ex) {
				Console.WriteLine(ex.StackTrace);
				_tcs.SetResult(new(0, null));
				break;
			}

			Thread.Sleep(1000);  // Poll every second
		}
	}

	public void StopTracking()
	{
		cts.Cancel();
	}

	public Task<Tuple<IntPtr, Process?>> WaitForMainWindowHandleChangeAsync() => _tcs.Task;
}

/*
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowsPosition
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int Left;
        public int Top;
        public int Width;
        public int Height;
        public int Flags;
    }
    */

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

	public override readonly string ToString()
	{
		return string.Format("({0}, {1}), {2} x {3}", Left, Top, Width, Height);
	}
}

