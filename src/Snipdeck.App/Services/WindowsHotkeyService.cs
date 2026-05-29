using System.Runtime.InteropServices;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Win32 RegisterHotKey-backed global hotkey. The WM_HOTKEY message arrives
    /// at the main window, so we subclass its WndProc to forward it as an
    /// <see cref="IHotkeyService.Pressed"/> event on the original thread.
    /// </summary>
    internal sealed partial class WindowsHotkeyService : IHotkeyService, IDisposable
    {
        private const int _hotkeyId = 0x5DEC;
        private const uint _wmHotkey = 0x0312;
        private const uint _subclassId = 0xDECA;

        private readonly IServiceProvider _services;
        private readonly SubclassProc _subclassProc;
        private IntPtr _windowHandle;
        private bool _subclassed;
        private bool _registered;

        public WindowsHotkeyService(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
            _subclassProc = OnSubclassedMessage;
        }

        public event EventHandler? Pressed;

        public bool TryRegister(HotkeyBinding binding)
        {
            ArgumentNullException.ThrowIfNull(binding);
            if (binding.IsEmpty)
            {
                return false;
            }

            EnsureSubclassed();
            if (_windowHandle == IntPtr.Zero)
            {
                return false;
            }

            if (_registered)
            {
                Unregister();
            }

            if (!TryGetVirtualKey(binding.Key, out var vk))
            {
                return false;
            }

            var modifiers = (uint)binding.Modifiers;
            if (!RegisterHotKey(_windowHandle, _hotkeyId, modifiers, vk))
            {
                return false;
            }

            _registered = true;
            return true;
        }

        public void Unregister()
        {
            if (_registered && _windowHandle != IntPtr.Zero)
            {
                _ = UnregisterHotKey(_windowHandle, _hotkeyId);
                _registered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
            if (_subclassed && _windowHandle != IntPtr.Zero)
            {
                _ = RemoveWindowSubclass(_windowHandle, _subclassProc, _subclassId);
                _subclassed = false;
            }
        }

        private void EnsureSubclassed()
        {
            if (_subclassed)
            {
                return;
            }
            var mainWindow = (MainWindow?)_services.GetService(typeof(MainWindow));
            if (mainWindow is null)
            {
                return;
            }
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            if (_windowHandle == IntPtr.Zero)
            {
                return;
            }
            _ = SetWindowSubclass(_windowHandle, _subclassProc, _subclassId, IntPtr.Zero);
            _subclassed = true;
        }

        private IntPtr OnSubclassedMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint id, IntPtr refData)
        {
            if (msg == _wmHotkey && wParam.ToInt32() == _hotkeyId)
            {
                Pressed?.Invoke(this, EventArgs.Empty);
            }
            return DefSubclassProc(hWnd, msg, wParam, lParam);
        }

        private static bool TryGetVirtualKey(string key, out uint vk)
        {
            vk = 0;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var trimmed = key.Trim();
            if (trimmed.Length == 1)
            {
                var c = char.ToUpperInvariant(trimmed[0]);
                if (c is >= 'A' and <= 'Z')
                {
                    vk = c;
                    return true;
                }
                if (c is >= '0' and <= '9')
                {
                    vk = c;
                    return true;
                }
            }

            return trimmed.ToUpperInvariant() switch
            {
                "F1" => Assign(0x70, out vk),
                "F2" => Assign(0x71, out vk),
                "F3" => Assign(0x72, out vk),
                "F4" => Assign(0x73, out vk),
                "F5" => Assign(0x74, out vk),
                "F6" => Assign(0x75, out vk),
                "F7" => Assign(0x76, out vk),
                "F8" => Assign(0x77, out vk),
                "F9" => Assign(0x78, out vk),
                "F10" => Assign(0x79, out vk),
                "F11" => Assign(0x7A, out vk),
                "F12" => Assign(0x7B, out vk),
                "SPACE" => Assign(0x20, out vk),
                "ESCAPE" or "ESC" => Assign(0x1B, out vk),
                "TAB" => Assign(0x09, out vk),
                "ENTER" or "RETURN" => Assign(0x0D, out vk),
                _ => false,
            };
        }

        private static bool Assign(uint value, out uint vk)
        {
            vk = value;
            return true;
        }

        private delegate IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint id, IntPtr refData);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        [LibraryImport("comctl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowSubclass(
            IntPtr hWnd,
            [MarshalAs(UnmanagedType.FunctionPtr)] SubclassProc pfnSubclass,
            uint uIdSubclass,
            IntPtr dwRefData);

        [LibraryImport("comctl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RemoveWindowSubclass(
            IntPtr hWnd,
            [MarshalAs(UnmanagedType.FunctionPtr)] SubclassProc pfnSubclass,
            uint uIdSubclass);

        [LibraryImport("comctl32.dll")]
        private static partial IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    }
}
