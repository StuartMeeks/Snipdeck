using Microsoft.Extensions.DependencyInjection;

using Snipdeck.App.Services;
using Snipdeck.App.Views;
using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App
{
    /// <summary>
    /// Builds the dependency-injection container. Settings are loaded
    /// synchronously here so the snip-store and backup paths can be resolved
    /// from <see cref="AppConfig"/> before the rest of the graph is built.
    /// </summary>
    internal static class Bootstrap
    {
        private const string _snipStoreFileName = "store.json";

        public static IServiceProvider Build()
        {
            var pathProvider = new WindowsPathProvider();
            var clock = new SystemClock();
            var settingsStore = new JsonSettingsStore(pathProvider.SettingsFilePath);

            var config = settingsStore.LoadAsync().GetAwaiter().GetResult();

            var storageDirectory = config.StoragePath ?? pathProvider.DefaultStorageDirectory;
            var backupDirectory = config.BackupDirectory ?? pathProvider.DefaultBackupDirectory;
            var snipStoreFilePath = Path.Combine(storageDirectory, _snipStoreFileName);

            var snipStore = new JsonSnipStore(snipStoreFilePath);
            var backupService = new BackupService(snipStoreFilePath, backupDirectory, clock, () => config.BackupRetention);
            var iconStorage = new IconAssetStorage(storageDirectory);

            var services = new ServiceCollection();
            _ = services
                .AddSingleton<IPathProvider>(pathProvider)
                .AddSingleton<IClock>(clock)
                .AddSingleton<IDispatcher, WinUiDispatcher>()
                .AddSingleton<IClipboardService, WindowsClipboardService>()
                .AddSingleton<IIconNormaliser, WindowsIconNormaliser>()
                .AddSingleton<IFilePickerService, WindowsFilePickerService>()
                .AddSingleton<IFolderPickerService, WindowsFolderPickerService>()
                .AddSingleton<IAppRestartService, WindowsAppRestartService>()
                .AddSingleton<IExternalLinkService, WindowsExternalLinkService>()
                .AddSingleton<IStorageRelocationService>(new StorageRelocationService(_snipStoreFileName))
                .AddSingleton<IHotkeyService, WindowsHotkeyService>()
                .AddSingleton<ITrayService, HNotifyIconTrayService>()
                .AddSingleton<IShellInteractions, WindowsShellInteractions>()
                .AddSingleton<IThemeApplier, WindowsThemeApplier>()
                .AddSingleton<IUpdateService, WindowsUpdateService>()
                .AddSingleton<ISettingsStore>(settingsStore)
                .AddSingleton<ISnipStore>(snipStore)
                .AddSingleton<IBackupService>(backupService)
                .AddSingleton<IIconAssetStorage>(iconStorage)
                .AddSingleton(config)
                .AddTransient<SettingsViewModel>()
                .AddSingleton<ShellViewModel>()
                .AddSingleton<ShellPage>()
                .AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
