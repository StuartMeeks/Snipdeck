using System.Collections.Specialized;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class ShellPage : UserControl
    {
        // The tag nav items are rebuilt from ViewModel.Tags; tracked so they can
        // be removed without disturbing the static Home/Documentation/header items.
        private readonly List<NavigationViewItem> _tagItems = [];

        public ShellPage(ShellViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ViewModel = viewModel;
            InitializeComponent();

            ViewModel.Tags.CollectionChanged += OnTagsChanged;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnLoaded;
        }

        public ShellViewModel ViewModel { get; }

        private static FontFamily SymbolFont =>
            Application.Current.Resources.TryGetValue("SymbolThemeFontFamily", out var resource)
                && resource is FontFamily family
                ? family
                : new FontFamily("Segoe Fluent Icons");

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync();
        }

        private void OnTagsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildTagNavItems();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ShellViewModel.SelectedTagItem) or nameof(ShellViewModel.CurrentContent))
            {
                SyncSelectionFromViewModel();
            }
        }

        private void RebuildTagNavItems()
        {
            foreach (var item in _tagItems)
            {
                _ = ShellNavigation.MenuItems.Remove(item);
            }
            _tagItems.Clear();

            foreach (var tag in ViewModel.Tags)
            {
                var navItem = new NavigationViewItem
                {
                    Content = tag.Name,
                    Tag = tag,
                    Icon = new FontIcon
                    {
                        FontFamily = SymbolFont,
                        Glyph = tag.Glyph,
                    },
                };
                ShellNavigation.MenuItems.Add(navItem);
                _tagItems.Add(navItem);
            }

            SyncSelectionFromViewModel();
        }

        // Reflect the view model's logical selection onto the NavigationView:
        // a selected tag highlights its item; Home content highlights Home; any
        // other content (Settings, Trash, etc.) clears the highlight.
        private void SyncSelectionFromViewModel()
        {
            if (ViewModel.SelectedTagItem is { } selected)
            {
                ShellNavigation.SelectedItem =
                    _tagItems.FirstOrDefault(i => ReferenceEquals(i.Tag, selected));
                return;
            }

            ShellNavigation.SelectedItem =
                ViewModel.CurrentContent is HomeViewModel ? HomeNavItem : null;
        }

        private async void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is not NavigationViewItem item)
            {
                return;
            }

            if (ReferenceEquals(item, HomeNavItem))
            {
                ViewModel.ShowHome();
            }
            else if (ReferenceEquals(item, DocumentationNavItem))
            {
                await ViewModel.OpenDocumentationAsync();
            }
            else if (item.Tag is TagItemViewModel tag)
            {
                ViewModel.SelectedTagItem = tag;
            }
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            var settings = App.Services.GetRequiredService<SettingsViewModel>();
            ViewModel.OpenSettings(settings);
        }

        private void OnTrashClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenTrash();
        }

        private void OnSharedParametersClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenGlobalParameters();
        }

        private void OnTagsClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenTagIcons();
        }

        private void OnNewCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewCliCommand.CanExecute(null))
            {
                ViewModel.NewCliCommand.Execute(null);
            }
        }

        private void OnNewSnipClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NewSnipCommand.CanExecute(null))
            {
                ViewModel.NewSnipCommand.Execute(null);
            }
        }

        private void OnEditCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.EditCurrentCliCommand.CanExecute(null))
            {
                ViewModel.EditCurrentCliCommand.Execute(null);
            }
        }

        private void OnDeleteCliClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.DeleteCurrentCliCommand.CanExecute(null))
            {
                ViewModel.DeleteCurrentCliCommand.Execute(null);
            }
        }
    }
}
