using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Snipdeck.App.Views;
using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Services
{
    /// <summary>
    /// Presents shell-level dialogs as WinUI <see cref="ContentDialog"/>s.
    /// Resolves the parent window lazily so the singleton can be constructed
    /// before the main window exists.
    /// </summary>
    internal sealed class WindowsShellInteractions : IShellInteractions
    {
        private readonly IServiceProvider _services;
        private readonly IIconNormaliser _iconNormaliser;
        private readonly IFilePickerService _filePicker;

        public WindowsShellInteractions(
            IServiceProvider services,
            IIconNormaliser iconNormaliser,
            IFilePickerService filePicker)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(iconNormaliser);
            ArgumentNullException.ThrowIfNull(filePicker);
            _services = services;
            _iconNormaliser = iconNormaliser;
            _filePicker = filePicker;
        }

        public async Task<bool> ConfirmAsync(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "Cancel", bool destructive = false)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = confirmButtonText,
                CloseButtonText = cancelButtonText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = GetXamlRoot(),
            };
            // Destructive confirmations (delete) get the subtle-red primary button.
            if (destructive && Application.Current.Resources["DangerDialogPrimaryButtonStyle"] is Style dangerStyle)
            {
                dialog.PrimaryButtonStyle = dangerStyle;
            }
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public async Task NotifyAsync(string title, string message, string buttonText = "OK")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = buttonText,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = GetXamlRoot(),
            };
            _ = await dialog.ShowAsync();
        }

        public async Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis)
        {
            ArgumentNullException.ThrowIfNull(snip);
            var editor = new SnipEditorViewModel(snip);
            var dialog = new SnipEditorDialog(editor)
            {
                XamlRoot = GetXamlRoot(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? new SnipEditResult(editor.BuildUpdatedSnip())
                : null;
        }

        public async Task<CliEditResult?> EditCliAsync(Cli cli)
        {
            ArgumentNullException.ThrowIfNull(cli);
            var editor = new CliEditorViewModel(cli);
            var dialog = new CliEditorDialog(editor, _iconNormaliser, _filePicker)
            {
                XamlRoot = GetXamlRoot(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? new CliEditResult(editor.BuildUpdatedCli(), editor.PickedIconBytes)
                : null;
        }

        public async Task<ParameterFillResult?> FillParametersAsync(Snip snip, IReadOnlyList<Parameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(snip);
            ArgumentNullException.ThrowIfNull(parameters);
            var fill = new ParameterFillViewModel(snip, parameters);
            var dialog = new ParameterFillDialog(fill)
            {
                XamlRoot = GetXamlRoot(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary && fill.IsCopyEnabled
                ? new ParameterFillResult(fill.ResolvedCommand)
                : null;
        }

        private XamlRoot GetXamlRoot()
        {
            var mainWindow = (MainWindow)_services.GetService(typeof(MainWindow))!;
            var content = mainWindow.Content
                ?? throw new InvalidOperationException("MainWindow has no content; XamlRoot is unavailable.");
            return ((FrameworkElement)content).XamlRoot;
        }

    }
}
