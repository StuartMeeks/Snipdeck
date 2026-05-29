using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    /// <summary>
    /// Picks the right <see cref="DataTemplate"/> for the shell's content area
    /// based on the view-model type held by <c>ShellViewModel.CurrentContent</c>.
    /// </summary>
    public sealed partial class ShellContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? HomeTemplate { get; set; }

        public DataTemplate? CliTemplate { get; set; }

        public DataTemplate? SettingsTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item)
        {
            return item switch
            {
                HomeViewModel => HomeTemplate,
                CliViewModel => CliTemplate,
                SettingsViewModel => SettingsTemplate,
                _ => null,
            };
        }

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
