using Microsoft.Extensions.DependencyInjection;

using Snipdeck.App.Services;
using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.Services;

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
            var backupService = new BackupService(snipStoreFilePath, backupDirectory, clock);

            var services = new ServiceCollection();
            services.AddSingleton<IPathProvider>(pathProvider);
            services.AddSingleton<IClock>(clock);
            services.AddSingleton<IDispatcher, WinUiDispatcher>();
            services.AddSingleton<ISettingsStore>(settingsStore);
            services.AddSingleton<ISnipStore>(snipStore);
            services.AddSingleton<IBackupService>(backupService);
            services.AddSingleton(config);

            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
