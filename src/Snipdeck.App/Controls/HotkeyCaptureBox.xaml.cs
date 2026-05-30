using System.Windows.Input;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Snipdeck.Core.Models;

using Windows.System;
using Windows.UI.Core;

namespace Snipdeck.App.Controls
{
    /// <summary>
    /// A focusable box that records a global-hotkey chord. Click (or focus) it,
    /// then press a modifier combination plus a key; it builds a
    /// <see cref="HotkeyBinding"/> and invokes <see cref="Command"/>. Esc cancels.
    /// Modifier-only and unsupported keys are ignored so the box keeps listening
    /// until a usable chord (≥1 modifier + key) is pressed.
    /// </summary>
    public sealed partial class HotkeyCaptureBox : UserControl
    {
        private const string _recordingPrompt = "Press a shortcut… (Esc to cancel)";

        private bool _recording;

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(HotkeyCaptureBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CurrentTextProperty =
            DependencyProperty.Register(nameof(CurrentText), typeof(string), typeof(HotkeyCaptureBox),
                new PropertyMetadata(string.Empty, OnCurrentTextChanged));

        public HotkeyCaptureBox()
        {
            InitializeComponent();
            UpdateLabel();
        }

        /// <summary>Executed with the captured <see cref="HotkeyBinding"/> when a chord is recorded.</summary>
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>The current binding's display text, shown when not recording.</summary>
        public string CurrentText
        {
            get => (string)GetValue(CurrentTextProperty);
            set => SetValue(CurrentTextProperty, value);
        }

        private static void OnCurrentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HotkeyCaptureBox)d).UpdateLabel();
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            StartRecording();
            _ = Focus(FocusState.Programmatic);
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_recording)
            {
                return;
            }

            if (e.Key == VirtualKey.Escape)
            {
                StopRecording();
                e.Handled = true;
                return;
            }

            // A modifier on its own isn't a chord yet — keep listening.
            if (IsModifierKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            var modifiers = ReadModifiers();
            var key = MapKey(e.Key);

            // Require a modifier and a supported key; otherwise swallow and wait.
            if (modifiers == HotkeyModifiers.None || key is null)
            {
                e.Handled = true;
                return;
            }

            var binding = new HotkeyBinding { Modifiers = modifiers, Key = key };
            StopRecording();
            if (Command?.CanExecute(binding) == true)
            {
                Command.Execute(binding);
            }
            e.Handled = true;
        }

        private void StartRecording()
        {
            _recording = true;
            Label.Text = _recordingPrompt;
        }

        private void StopRecording()
        {
            _recording = false;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (!_recording)
            {
                Label.Text = string.IsNullOrEmpty(CurrentText) ? "(unbound)" : CurrentText;
            }
        }

        private static bool IsModifierKey(VirtualKey key) =>
            key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
                or VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
                or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
                or VirtualKey.LeftWindows or VirtualKey.RightWindows;

        private static bool IsDown(VirtualKey key) =>
            InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

        private static HotkeyModifiers ReadModifiers()
        {
            var modifiers = HotkeyModifiers.None;
            if (IsDown(VirtualKey.Control))
            {
                modifiers |= HotkeyModifiers.Control;
            }
            if (IsDown(VirtualKey.Menu))
            {
                modifiers |= HotkeyModifiers.Alt;
            }
            if (IsDown(VirtualKey.Shift))
            {
                modifiers |= HotkeyModifiers.Shift;
            }
            if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
            {
                modifiers |= HotkeyModifiers.Windows;
            }
            return modifiers;
        }

        /// <summary>
        /// Maps a virtual key to the Core key vocabulary the hotkey service
        /// understands (A-Z, 0-9, F1-F12, Space, Tab, Enter). Returns null for
        /// anything unsupported so the caller keeps listening.
        /// </summary>
        // Expressed as conditional chains rather than a switch over VirtualKey:
        // that enum has hundreds of members, so a switch can satisfy neither the
        // "populate every case" nor the "use a switch expression" analysers.
        private static string? MapKey(VirtualKey key) =>
            key is >= VirtualKey.A and <= VirtualKey.Z ? key.ToString()
            : key is >= VirtualKey.Number0 and <= VirtualKey.Number9 ? ((char)('0' + (key - VirtualKey.Number0))).ToString()
            : key is >= VirtualKey.F1 and <= VirtualKey.F12 ? key.ToString()
            : NamedKey(key);

        private static string? NamedKey(VirtualKey key) =>
            key == VirtualKey.Space ? "Space"
            : key == VirtualKey.Tab ? "Tab"
            : key == VirtualKey.Enter ? "Enter"
            : null;
    }
}
