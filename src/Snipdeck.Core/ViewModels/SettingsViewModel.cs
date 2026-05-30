using System.Reflection;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Editable application settings. Changes are persisted to
    /// <see cref="ISettingsStore"/> as they happen; theme changes also fire
    /// <see cref="IThemeApplier.Apply"/> so the UI updates live.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IThemeApplier _themeApplier;
        private readonly IUpdateService _updateService;
        private readonly IHotkeyService _hotkeyService;
        private readonly IFolderPickerService _folderPicker;
        private readonly IStorageRelocationService _relocation;
        private readonly IAppRestartService _restart;
        private readonly IShellInteractions _interactions;
        private readonly IPathProvider _pathProvider;
        private readonly AppConfig _config;
        private bool _suppressPersist;

        public SettingsViewModel(
            ISettingsStore settingsStore,
            IThemeApplier themeApplier,
            IUpdateService updateService,
            IHotkeyService hotkeyService,
            IFolderPickerService folderPicker,
            IStorageRelocationService relocation,
            IAppRestartService restart,
            IShellInteractions interactions,
            IPathProvider pathProvider,
            AppConfig config)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(themeApplier);
            ArgumentNullException.ThrowIfNull(updateService);
            ArgumentNullException.ThrowIfNull(hotkeyService);
            ArgumentNullException.ThrowIfNull(folderPicker);
            ArgumentNullException.ThrowIfNull(relocation);
            ArgumentNullException.ThrowIfNull(restart);
            ArgumentNullException.ThrowIfNull(interactions);
            ArgumentNullException.ThrowIfNull(pathProvider);
            ArgumentNullException.ThrowIfNull(config);

            _settingsStore = settingsStore;
            _themeApplier = themeApplier;
            _updateService = updateService;
            _hotkeyService = hotkeyService;
            _folderPicker = folderPicker;
            _relocation = relocation;
            _restart = restart;
            _interactions = interactions;
            _pathProvider = pathProvider;
            _config = config;

            _suppressPersist = true;
            Theme = config.Theme;
            ThemeIndex = ThemeIndexFor(config.Theme);
            CloseBehaviour = config.CloseBehaviour;
            CloseBehaviourIndex = CloseBehaviourIndexFor(config.CloseBehaviour);
            HotkeyDisplay = config.Hotkey.ToDisplayString();
            StorageDirectory = config.StoragePath ?? pathProvider.DefaultStorageDirectory;
            BackupDirectory = config.BackupDirectory ?? pathProvider.DefaultBackupDirectory;
            BackupRetention = config.BackupRetention;
            _suppressPersist = false;

            var assembly = typeof(SettingsViewModel).Assembly;
            VersionDisplay = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0-dev";
            CopyrightDisplay = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
                ?? "Copyright © Stuart Meeks";
        }

#pragma warning disable CA1822 // not actually static — bound from XAML via DataContext.
        public string AppName => "Snipdeck";

        public string TaglineDisplay => "Parameterised CLI snippet manager.";
#pragma warning restore CA1822

        public string VersionDisplay { get; }

        public string CopyrightDisplay { get; }

        [ObservableProperty]
        public partial string StorageDirectory { get; set; } = string.Empty;

        public string BackupDirectory { get; }

        [ObservableProperty]
        public partial string HotkeyDisplay { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string HotkeyError { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ThemePreference Theme { get; set; }

        [ObservableProperty]
        public partial int ThemeIndex { get; set; }

        [ObservableProperty]
        public partial int BackupRetention { get; set; }

        [ObservableProperty]
        public partial CloseBehaviour CloseBehaviour { get; set; }

        [ObservableProperty]
        public partial int CloseBehaviourIndex { get; set; }

        [ObservableProperty]
        public partial string UpdateStatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsCheckingForUpdates { get; set; }

        [ObservableProperty]
        public partial bool UpdateAvailable { get; set; }

        partial void OnThemeIndexChanged(int value)
        {
            Theme = value switch
            {
                1 => ThemePreference.Light,
                2 => ThemePreference.Dark,
                _ => ThemePreference.System,
            };
            _themeApplier.Apply(Theme);
            _ = PersistAsync();
        }

        partial void OnBackupRetentionChanged(int value)
        {
            if (value < 1)
            {
                // Re-entrant set lands back here with a valid value, which persists.
                BackupRetention = 1;
                return;
            }
            _ = PersistAsync();
        }

        partial void OnCloseBehaviourIndexChanged(int value)
        {
            CloseBehaviour = value == 1 ? CloseBehaviour.Exit : CloseBehaviour.HideToTray;
            _ = PersistAsync();
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            IsCheckingForUpdates = true;
            try
            {
                var result = await _updateService.CheckForUpdatesAsync().ConfigureAwait(true);
                UpdateAvailable = result.UpdateAvailable;
                UpdateStatusMessage = result.UpdateAvailable
                    ? $"Update available: {result.AvailableVersion}"
                    : "You're on the latest version.";
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        [RelayCommand]
        private async Task ApplyUpdateAsync()
        {
            var applied = await _updateService.ApplyUpdateAndRestartAsync().ConfigureAwait(true);
            if (!applied)
            {
                UpdateStatusMessage = "Couldn't apply the update; try again later.";
            }
        }

        [RelayCommand]
        private void RebindHotkey(HotkeyBinding? binding)
        {
            if (binding is null || !binding.IsValid)
            {
                HotkeyError = "Use at least one modifier (Ctrl, Alt or Shift) plus a key.";
                return;
            }

            var previous = _config.Hotkey;
            if (_hotkeyService.TryRegister(binding))
            {
                _config.Hotkey = binding;
                HotkeyDisplay = binding.ToDisplayString();
                HotkeyError = string.Empty;
                _ = PersistAsync();
                return;
            }

            // TryRegister unregisters the old binding before attempting the new
            // one, so on failure (chord already taken by another app) the old
            // hotkey is gone — restore it so the user isn't left with nothing.
            _ = _hotkeyService.TryRegister(previous);
            HotkeyDisplay = previous.ToDisplayString();
            HotkeyError = $"Couldn't set {binding.ToDisplayString()} — it may already be in use by another app.";
        }

        [RelayCommand]
        private void ResetHotkey()
        {
            RebindHotkey(HotkeyBinding.Default);
        }

        [RelayCommand]
        private async Task ChangeStoragePathAsync()
        {
            var target = await _folderPicker.PickFolderAsync().ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            var current = _config.StoragePath ?? _pathProvider.DefaultStorageDirectory;
            var outcome = _relocation.Inspect(current, target);

            // The storage path is only read at startup, so apply-then-restart keeps
            // the immutable startup state consistent and avoids the running app
            // writing snips to the old location after the switch.
            switch (outcome)
            {
                case StorageChangeOutcome.NoChange:
                    return;

                case StorageChangeOutcome.Invalid:
                    await _interactions.NotifyAsync(
                        "Can't use that folder",
                        "Choose a folder that isn't inside (or a parent of) your current storage folder.").ConfigureAwait(true);
                    return;

                case StorageChangeOutcome.AdoptTarget:
                    if (!await _interactions.ConfirmAsync(
                        "Use this folder?",
                        $"“{target}” already contains a Snipdeck store. Snipdeck will use it from now on; the snips in your current folder won't be moved. Snipdeck will restart to apply this.",
                        "Use it",
                        "Cancel").ConfigureAwait(true))
                    {
                        return;
                    }
                    break;

                case StorageChangeOutcome.MoveToTarget:
                    if (!await _interactions.ConfirmAsync(
                        "Move storage?",
                        $"Move your snips to “{target}”? Snipdeck will restart to apply this.",
                        "Move",
                        "Cancel").ConfigureAwait(true))
                    {
                        return;
                    }
                    _relocation.MoveStore(current, target);
                    break;

                case StorageChangeOutcome.SetEmptyTarget:
                    if (!await _interactions.ConfirmAsync(
                        "Change storage location?",
                        $"Use “{target}” as the storage location? Snipdeck will restart to apply this.",
                        "Change",
                        "Cancel").ConfigureAwait(true))
                    {
                        return;
                    }
                    break;

                default:
                    return;
            }

            _config.StoragePath = target;
            await PersistAsync().ConfigureAwait(true);
            StorageDirectory = target;
            _restart.Restart();
        }

        private async Task PersistAsync()
        {
            if (_suppressPersist)
            {
                return;
            }
            _config.Theme = Theme;
            _config.CloseBehaviour = CloseBehaviour;
            _config.BackupRetention = BackupRetention;
            // _config.Hotkey is set directly by RebindHotkey before this runs.
            await _settingsStore.SaveAsync(_config).ConfigureAwait(true);
        }

        private static int ThemeIndexFor(ThemePreference theme) => theme switch
        {
            ThemePreference.System => 0,
            ThemePreference.Light => 1,
            ThemePreference.Dark => 2,
            _ => 0,
        };

        private static int CloseBehaviourIndexFor(CloseBehaviour behaviour) => behaviour switch
        {
            CloseBehaviour.HideToTray => 0,
            CloseBehaviour.Exit => 1,
            _ => 0,
        };
    }
}
