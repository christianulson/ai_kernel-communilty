using System.Runtime.InteropServices;

namespace KrnlAI.VisualStudio.Extensibility.Core.Commands;

/// <summary>
/// Win32 clipboard implementation (works from any Windows process, no WPF/STA required).
/// </summary>
public sealed class Win32Clipboard : IClipboard
{
    /// <inheritdoc/>
    public Task SetTextAsync(string text, CancellationToken cancellationToken)
    {
        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("Failed to open the clipboard.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!EmptyClipboard())
                throw new InvalidOperationException("Failed to empty the clipboard.");

            var bytes = System.Text.Encoding.Unicode.GetBytes(text + "\0");
            var handle = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, handle, bytes.Length);
                if (SetClipboardData(CF_UNICODETEXT, handle) == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to set clipboard data.");
                handle = IntPtr.Zero;
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    Marshal.FreeHGlobal(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }

        return Task.CompletedTask;
    }

    private const int CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();
}