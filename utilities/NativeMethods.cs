using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace utilities
{
	public class NativeMethods
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Message
		{
			public IntPtr hWnd;
			public IntPtr msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}
		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out Message msg, IntPtr hWnd,
											  uint messageFilterMin,
											  uint messageFilterMax, uint flags);
		public NativeMethods() { }
	}
}
