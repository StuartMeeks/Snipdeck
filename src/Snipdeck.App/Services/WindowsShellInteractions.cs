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
        private readonly IGlyphCatalogueProvider _glyphCatalogue;

        public WindowsShellInteractions(
            IServiceProvider services,
            IIconNormaliser iconNormaliser,
            IFilePickerService filePicker,
            IGlyphCatalogueProvider glyphCatalogue)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(iconNormaliser);
            ArgumentNullException.ThrowIfNull(filePicker);
            ArgumentNullException.ThrowIfNull(glyphCatalogue);
            _services = services;
            _iconNormaliser = iconNormaliser;
            _filePicker = filePicker;
            _glyphCatalogue = glyphCatalogue;
        }

        public async Task<bool> ConfirmAsync(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "Cancel", bool destructive = false)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = confirmButtonText,
                CloseButtonText = cancelButtonText,
                // For destructive confirms, make Cancel the default: it's the safe
                // choice, and it stops the default-button accent treatment from
                // overriding the primary button's subtle-red foreground.
                DefaultButton = destructive ? ContentDialogButton.Close : ContentDialogButton.Primary,
                XamlRoot = GetXamlRoot(),
                RequestedTheme = CurrentTheme(),
            };
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
                RequestedTheme = CurrentTheme(),
            };
            _ = await dialog.ShowAsync();
        }

        public async Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis)
        {
            ArgumentNullException.ThrowIfNull(snip);
            var editor = new SnipEditorViewModel(snip, availableClis);
            var dialog = new SnipEditorDialog(editor)
            {
                XamlRoot = GetXamlRoot(),
                RequestedTheme = CurrentTheme(),
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
                RequestedTheme = CurrentTheme(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? new CliEditResult(editor.BuildUpdatedCli(), editor.PickedIconBytes)
                : null;
        }

        public async Task<Parameter?> EditParameterAsync(string title, Parameter? existing)
        {
            var row = new ParameterEditorRowViewModel(existing ?? new Parameter { Name = "param" });
            var dialog = new ParameterEditorDialog(title, row)
            {
                XamlRoot = GetXamlRoot(),
                RequestedTheme = CurrentTheme(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? row.BuildParameter() : null;
        }

        public async Task<ParameterFillResult?> FillParametersAsync(Snip snip, IReadOnlyList<Parameter> parameters)
        {
            ArgumentNullException.ThrowIfNull(snip);
            ArgumentNullException.ThrowIfNull(parameters);
            var fill = new ParameterFillViewModel(snip, parameters);
            var dialog = new ParameterFillDialog(fill)
            {
                XamlRoot = GetXamlRoot(),
                RequestedTheme = CurrentTheme(),
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary && fill.IsCopyEnabled
                ? new ParameterFillResult(fill.ResolvedCommand)
                : null;
        }

        public async Task<string?> PickGlyphAsync(string? currentGlyph)
        {
            // Re-read the catalogue each open, so edits to icon-catalogue.json take
            // effect without a restart.
            var picker = new GlyphPickerViewModel(_glyphCatalogue.GetEntries(), currentGlyph);
            var dialog = new GlyphPickerDialog(picker)
            {
                XamlRoot = GetXamlRoot(),
                RequestedTheme = CurrentTheme(),
            };
            _ = await dialog.ShowAsync();
            // The dialog records the chosen glyph itself (Choose button or
            // double-tap); a cancel leaves it null.
            return dialog.ChosenGlyph;
        }

        private XamlRoot GetXamlRoot()
        {
            var mainWindow = (MainWindow)_services.GetService(typeof(MainWindow))!;
            var content = mainWindow.Content
                ?? throw new InvalidOperationException("MainWindow has no content; XamlRoot is unavailable.");
            return ((FrameworkElement)content).XamlRoot;
        }

        // Dialogs are separate visual roots, so they don't inherit the in-app theme
        // (applied via RequestedTheme on the main window content). Mirror it so a
        // dialog opened after a Light/Dark switch matches, instead of the OS theme.
        private ElementTheme CurrentTheme()
        {
            var mainWindow = (MainWindow)_services.GetService(typeof(MainWindow))!;
            return mainWindow.Content is FrameworkElement content
                ? content.RequestedTheme
                : ElementTheme.Default;
        }
    }
}
