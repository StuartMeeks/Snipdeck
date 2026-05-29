using System.Reflection;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snipdeck.Core.ViewModels
{
    /// <summary>
    /// Settings stub for Phase 3 — real settings UI (storage path, backups,
    /// theme, hotkey, close behaviour) lands in Phase 6, About becomes a
    /// fully-fledged expander backed by Nerdbank.GitVersioning then.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        public SettingsViewModel()
        {
            var assembly = typeof(SettingsViewModel).Assembly;
            VersionDisplay = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0-dev";
            CopyrightDisplay = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
                ?? "Copyright © Stuart Meeks";
        }

        public static string AppName => "Snipdeck";

        public string VersionDisplay { get; }

        public string CopyrightDisplay { get; }

        public static string TaglineDisplay => "Parameterised CLI snippet manager.";
    }
}
