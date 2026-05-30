using Snipdeck.Core.Models;
using Snipdeck.Core.Services;
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
            store = new FakeSettingsStore();
            hotkey = new FakeHotkeyService();
            return new SettingsViewModel(
                store,
                new FakeThemeApplier(),
                new FakeUpdateService(),
                hotkey,
                new FakeFolderPickerService(),
                new StorageRelocationService(),
                new FakeAppRestartService(),
                new FakeShellInteractions(),
                new FakePathProvider(),
                config ?? new AppConfig());
        }

        private static SettingsViewModel BuildForStorage(
            string currentDirectory,
            out FakeFolderPickerService folderPicker,
            out FakeShellInteractions interactions,
            out FakeAppRestartService restart,
            out FakeSettingsStore store,
            AppConfig? config = null)
        {
            folderPicker = new FakeFolderPickerService();
            interactions = new FakeShellInteractions();
            restart = new FakeAppRestartService();
            store = new FakeSettingsStore();
            var pathProvider = new FakePathProvider { DefaultStorageDirectory = currentDirectory };
            return new SettingsViewModel(
                store,
                new FakeThemeApplier(),
                new FakeUpdateService(),
                new FakeHotkeyService(),
                folderPicker,
                new StorageRelocationService(),
                restart,
                interactions,
                pathProvider,
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

        [Fact]
        public async Task ChangeStoragePath_moves_the_store_to_an_empty_target_then_restarts()
        {
            var current = Directory.CreateTempSubdirectory("snipdeck-cur-").FullName;
            var target = Directory.CreateTempSubdirectory("snipdeck-tgt-").FullName;
            try
            {
                await File.WriteAllTextAsync(Path.Combine(current, "store.json"), "{}");
                _ = Directory.CreateDirectory(Path.Combine(current, "icons"));
                await File.WriteAllTextAsync(Path.Combine(current, "icons", "a.png"), "x");
                Directory.Delete(target); // target must not exist yet for a clean "move"

                var vm = BuildForStorage(current, out var picker, out var ix, out var restart, out var store);
                picker.NextFolder = target;
                ix.NextConfirmResult = true;

                await vm.ChangeStoragePathCommand.ExecuteAsync(null);

                Assert.True(File.Exists(Path.Combine(target, "store.json")));
                Assert.True(File.Exists(Path.Combine(target, "icons", "a.png")));
                Assert.False(File.Exists(Path.Combine(current, "store.json")));
                Assert.Equal(target, store.Current.StoragePath);
                Assert.Equal(target, vm.StorageDirectory);
                Assert.Equal(1, restart.RestartCount);
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                if (Directory.Exists(target)) { Directory.Delete(target, recursive: true); }
            }
        }

        [Fact]
        public async Task ChangeStoragePath_adopts_an_existing_store_without_moving_the_current_one()
        {
            var current = Directory.CreateTempSubdirectory("snipdeck-cur-").FullName;
            var target = Directory.CreateTempSubdirectory("snipdeck-tgt-").FullName;
            try
            {
                await File.WriteAllTextAsync(Path.Combine(current, "store.json"), "{}");
                await File.WriteAllTextAsync(Path.Combine(target, "store.json"), "{}"); // target already has a store

                var vm = BuildForStorage(current, out var picker, out var ix, out var restart, out var store);
                picker.NextFolder = target;
                ix.NextConfirmResult = true;

                await vm.ChangeStoragePathCommand.ExecuteAsync(null);

                // Adopt: current store is left in place, target's store untouched.
                Assert.True(File.Exists(Path.Combine(current, "store.json")));
                Assert.Equal(target, store.Current.StoragePath);
                Assert.Equal(1, restart.RestartCount);
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                Directory.Delete(target, recursive: true);
            }
        }

        [Fact]
        public async Task ChangeStoragePath_does_nothing_when_the_user_cancels()
        {
            var current = Directory.CreateTempSubdirectory("snipdeck-cur-").FullName;
            var target = Directory.CreateTempSubdirectory("snipdeck-tgt-").FullName;
            try
            {
                await File.WriteAllTextAsync(Path.Combine(current, "store.json"), "{}");

                var vm = BuildForStorage(current, out var picker, out var ix, out var restart, out var store);
                picker.NextFolder = target;
                ix.NextConfirmResult = false; // cancelled

                await vm.ChangeStoragePathCommand.ExecuteAsync(null);

                Assert.Null(store.Current.StoragePath);
                Assert.Equal(0, restart.RestartCount);
                Assert.True(File.Exists(Path.Combine(current, "store.json")));
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                Directory.Delete(target, recursive: true);
            }
        }

        [Fact]
        public async Task ChangeStoragePath_no_ops_when_the_picker_is_cancelled()
        {
            var current = Directory.CreateTempSubdirectory("snipdeck-cur-").FullName;
            try
            {
                var vm = BuildForStorage(current, out var picker, out _, out var restart, out var store);
                picker.NextFolder = null; // user cancelled the folder picker

                await vm.ChangeStoragePathCommand.ExecuteAsync(null);

                Assert.Equal(0, restart.RestartCount);
                Assert.Equal(0, store.SaveCount);
            }
            finally
            {
                Directory.Delete(current, recursive: true);
            }
        }
    }
}
