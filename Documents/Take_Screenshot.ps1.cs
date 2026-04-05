using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Diagnostics;
using System.Threading;

public class TakeScreenshot
{
	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);
	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
	[DllImport("user32.dll")]
	public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
	[DllImport("dwmapi.dll")]
	public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left, Top, Right, Bottom;
	}

	public static RECT GetExtendedFrameBounds(IntPtr hwnd)
	{
		RECT rect;
		DwmGetWindowAttribute(hwnd, 9, out rect, Marshal.SizeOf(typeof(RECT)));
		return rect;
	}

	public static void CenterAndResize(IntPtr hwnd, int w, int h, int yOffset = 0)
	{
		var screen = Screen.PrimaryScreen.WorkingArea;
		int x = (screen.Width - w) / 2 + screen.Left;
		int y = (screen.Height - h) / 2 + screen.Top + yOffset;
		SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h, 0x0040);
	}

	/// <summary>
	/// Captures a window using CopyFromScreen with DWM extended frame bounds.
	/// </summary>
	public static void CaptureWindow(IntPtr hwnd, string filePath)
	{
		var rect = GetExtendedFrameBounds(hwnd);
		int w = rect.Right - rect.Left;
		int h = rect.Bottom - rect.Top;
		using (var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb))
		{
			using (var graphics = Graphics.FromImage(bitmap))
			{
				graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h));
			}
			bitmap.Save(filePath, ImageFormat.Png);
		}
	}

	/// <summary>
	/// Find the main window of a process via UI Automation.
	/// </summary>
	public static AutomationElement FindMainWindow(int processId, int timeoutMs = 10000)
	{
		var desktop = AutomationElement.RootElement;
		var sw = Stopwatch.StartNew();
		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			var condition = new AndCondition(
				new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
			);
			var window = desktop.FindFirst(TreeScope.Children, condition);
			if (window != null)
				return window;
			Thread.Sleep(200);
		}
		return null;
	}

	/// <summary>
	/// Find a descendant element by AutomationId.
	/// </summary>
	public static AutomationElement FindDescendant(AutomationElement parent, string automationId, int timeoutMs = 5000)
	{
		var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
		var sw = Stopwatch.StartNew();
		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			var element = parent.FindFirst(TreeScope.Descendants, condition);
			if (element != null)
				return element;
			Thread.Sleep(200);
		}
		return null;
	}

	/// <summary>
	/// Click a button element via InvokePattern.
	/// </summary>
	public static bool ClickButton(AutomationElement button)
	{
		if (button == null) return false;
		var pattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
		if (pattern == null) return false;
		pattern.Invoke();
		return true;
	}

	/// <summary>
	/// Get the text value of a TextBox element via ValuePattern.
	/// </summary>
	public static string GetTextBoxValue(AutomationElement textBox)
	{
		if (textBox == null) return null;
		object patternObj;
		if (textBox.TryGetCurrentPattern(ValuePattern.Pattern, out patternObj))
			return ((ValuePattern)patternObj).Current.Value;
		return null;
	}

	/// <summary>
	/// Wait for a TextBox to contain the specified text.
	/// </summary>
	public static bool WaitForText(AutomationElement textBox, string text, int timeoutMs = 30000)
	{
		var sw = Stopwatch.StartNew();
		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			var value = GetTextBoxValue(textBox);
			if (value != null && value.Contains(text))
				return true;
			Thread.Sleep(300);
		}
		return false;
	}
}
