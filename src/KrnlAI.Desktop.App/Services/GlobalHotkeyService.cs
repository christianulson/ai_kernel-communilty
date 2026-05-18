using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace KrnlAI.Desktop.App.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int MOD_ALT = 0x0001;

    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _currentId;
    private HwndSource? _source;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public GlobalHotkeyService(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        if (_source == null)
        {
            throw new InvalidOperationException("GlobalHotkeyService: HwndSource is null. Hotkeys will not work. Ensure the window handle is available before creating this service.");
        }
        _source.AddHook(HwndHook);
    }

    public bool RegisterHotkey(ModifierKeys modifiers, Key key, Action callback)
    {
        if (_source == null) return false;

        int id = ++_currentId;
        uint fsModifiers = 0;

        if (modifiers.HasFlag(ModifierKeys.Control)) fsModifiers |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) fsModifiers |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Alt)) fsModifiers |= MOD_ALT;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (RegisterHotKey(_hwnd, id, fsModifiers, vk))
        {
            _hotkeyActions[id] = callback;
            return true;
        }

        return false;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source?.RemoveHook(HwndHook);

        foreach (var id in _hotkeyActions.Keys)
        {
            UnregisterHotKey(_hwnd, id);
        }

        _hotkeyActions.Clear();
    }
}