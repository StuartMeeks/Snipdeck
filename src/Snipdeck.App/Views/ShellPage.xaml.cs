using System.Collections.Specialized;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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

        /// <summary>Toggle the navigation pane (driven by the title-bar hamburger).</summary>
        public void TogglePane()
        {
            ShellNavigation.IsPaneOpen = !ShellNavigation.IsPaneOpen;
        }

        // Reflect the view model's current content onto the NavigationView selection
        // so the right item (tag, Home, or a footer destination) shows the selected look.
        private void SyncSelectionFromViewModel()
        {
            ShellNavigation.SelectedItem = ViewModel.CurrentContent switch
            {
                HomeViewModel => HomeNavItem,
                SettingsViewModel => SettingsNavItem,
                TrashViewModel => TrashNavItem,
                GlobalParametersViewModel => SharedParametersNavItem,
                TagIconsViewModel => TagsNavItem,
                CliViewModel => _tagItems.FirstOrDefault(i => ReferenceEquals(i.Tag, ViewModel.SelectedTagItem)),
                _ => null,
            };
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
            else if (ReferenceEquals(item, SharedParametersNavItem))
            {
                ViewModel.OpenGlobalParameters();
            }
            else if (ReferenceEquals(item, TagsNavItem))
            {
                ViewModel.OpenTagIcons();
            }
            else if (ReferenceEquals(item, TrashNavItem))
            {
                ViewModel.OpenTrash();
            }
            else if (ReferenceEquals(item, SettingsNavItem))
            {
                ViewModel.OpenSettings(App.Services.GetRequiredService<SettingsViewModel>());
            }
            else if (item.Tag is TagItemViewModel tag)
            {
                ViewModel.SelectedTagItem = tag;
            }
        }

        // Keep the active category toggle checked even when it's re-clicked (the
        // category command is a no-op then, so the OneWay binding wouldn't re-assert).
        private void OnHomeCategoryToggled(object sender, RoutedEventArgs e)
        {
            ((ToggleButton)sender).IsChecked = true;
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
