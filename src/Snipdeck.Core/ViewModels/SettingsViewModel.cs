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
        private readonly AppConfig _config;
        private bool _suppressPersist;

        public SettingsViewModel(
            ISettingsStore settingsStore,
            IThemeApplier themeApplier,
            IUpdateService updateService,
            IPathProvider pathProvider,
            AppConfig config)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(themeApplier);
            ArgumentNullException.ThrowIfNull(updateService);
            ArgumentNullException.ThrowIfNull(pathProvider);
            ArgumentNullException.ThrowIfNull(config);

            _settingsStore = settingsStore;
            _themeApplier = themeApplier;
            _updateService = updateService;
            _config = config;

            _suppressPersist = true;
            Theme = config.Theme;
            ThemeIndex = ThemeIndexFor(config.Theme);
            CloseBehaviour = config.CloseBehaviour;
            CloseBehaviourIndex = CloseBehaviourIndexFor(config.CloseBehaviour);
            HotkeyDisplay = FormatHotkey(config.Hotkey);
            StorageDirectory = config.StoragePath ?? pathProvider.DefaultStorageDirectory;
            BackupDirectory = config.BackupDirectory ?? pathProvider.DefaultBackupDirectory;
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

        public string StorageDirectory { get; }

        public string BackupDirectory { get; }

        public string HotkeyDisplay { get; }

        [ObservableProperty]
        public partial ThemePreference Theme { get; set; }

        [ObservableProperty]
        public partial int ThemeIndex { get; set; }

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

        private async Task PersistAsync()
        {
            if (_suppressPersist)
            {
                return;
            }
            _config.Theme = Theme;
            _config.CloseBehaviour = CloseBehaviour;
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

        private static string FormatHotkey(HotkeyBinding binding)
        {
            if (binding.IsEmpty)
            {
                return "(unbound)";
            }
            var parts = new List<string>();
            if (binding.Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }
            if (binding.Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }
            if (binding.Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }
            if (binding.Modifiers.HasFlag(HotkeyModifiers.Windows))
            {
                parts.Add("Win");
            }
            parts.Add(binding.Key);
            return string.Join('+', parts);
        }
    }
}
