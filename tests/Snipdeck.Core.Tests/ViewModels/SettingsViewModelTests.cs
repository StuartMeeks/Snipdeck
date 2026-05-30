using Snipdeck.Core.Models;
using Snipdeck.Core.Tests.Support;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class SettingsViewModelTests
    {
        private static SettingsViewModel Build(
            out FakeHotkeyService hotkey,
            out FakeSettingsStore store,
            AppConfig? config = null)
        {
            hotkey = new FakeHotkeyService();
            store = new FakeSettingsStore();
            return new SettingsViewModel(
                store,
                new FakeThemeApplier(),
                new FakeUpdateService(),
                hotkey,
                new FakePathProvider(),
                config ?? new AppConfig());
        }

        [Fact]
        public void Initial_hotkey_display_reflects_config()
        {
            var vm = Build(out _, out _, new AppConfig { Hotkey = HotkeyBinding.Default });
            Assert.Equal("Ctrl+Alt+S", vm.HotkeyDisplay);
        }

        [Fact]
        public void RebindHotkey_registers_persists_and_updates_display_on_success()
        {
            var vm = Build(out var hotkey, out var store);
            var binding = new HotkeyBinding { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift, Key = "K" };

            vm.RebindHotkeyCommand.Execute(binding);

            Assert.Same(binding, hotkey.LastRegistered);
            Assert.Equal("Ctrl+Shift+K", vm.HotkeyDisplay);
            Assert.Equal(string.Empty, vm.HotkeyError);
            Assert.Equal(1, store.SaveCount);
            Assert.Equal(binding, store.Current.Hotkey);
        }

        [Fact]
        public void RebindHotkey_rejects_a_binding_with_no_modifier()
        {
            var vm = Build(out var hotkey, out var store);

            vm.RebindHotkeyCommand.Execute(new HotkeyBinding { Modifiers = HotkeyModifiers.None, Key = "K" });

            Assert.Equal(0, hotkey.RegisterCount);
            Assert.Equal(0, store.SaveCount);
            Assert.NotEqual(string.Empty, vm.HotkeyError);
        }

        [Fact]
        public void RebindHotkey_restores_previous_and_reports_error_when_registration_fails()
        {
            var vm = Build(out var hotkey, out _, new AppConfig { Hotkey = HotkeyBinding.Default });
            hotkey.NextRegisterResult = false;

            vm.RebindHotkeyCommand.Execute(new HotkeyBinding { Modifiers = HotkeyModifiers.Alt, Key = "F1" });

            // The failed binding is not adopted; display reverts to the previous chord.
            Assert.Equal("Ctrl+Alt+S", vm.HotkeyDisplay);
            Assert.Contains("in use", vm.HotkeyError);
            // Two register attempts: the (failed) new one, then the restore of the old.
            Assert.Equal(2, hotkey.RegisterCount);
        }

        [Fact]
        public void ResetHotkey_rebinds_to_the_default_chord()
        {
            var vm = Build(out var hotkey, out _, new AppConfig
            {
                Hotkey = new HotkeyBinding { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift, Key = "K" },
            });

            vm.ResetHotkeyCommand.Execute(null);

            Assert.Equal("Ctrl+Alt+S", vm.HotkeyDisplay);
            Assert.NotNull(hotkey.LastRegistered);
            Assert.Equal(HotkeyBinding.Default.Modifiers, hotkey.LastRegistered!.Modifiers);
            Assert.Equal("S", hotkey.LastRegistered.Key);
        }
    }
}
