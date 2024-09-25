using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon.lib.Common.Extensions;
public static class ProcessExts {
}

public static partial class Procvoke {
	[LibraryImport("ntdll.dll", SetLastError = true)]
	private static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, out uint returnLength);

	public static int ParentProcessId(this Process process)
	{
		var pbi = new PROCESS_BASIC_INFORMATION();
		var status = NtQueryInformationProcess(process.Handle, 0, ref pbi, (uint)Marshal.SizeOf(pbi), out _);
		return status != 0
			?      throw new Exception("NtQueryInformationProcess failed with status: " + status)
			: pbi.InheritedFromUniqueProcessId.ToInt32();
	}

	private struct PROCESS_BASIC_INFORMATION {
		public IntPtr ExitStatus;
		public IntPtr PebBaseAddress;
		public IntPtr AffinityMask;
		public IntPtr BasePriority;
		public IntPtr UniqueProcessId;
		public IntPtr InheritedFromUniqueProcessId;
	}
}
