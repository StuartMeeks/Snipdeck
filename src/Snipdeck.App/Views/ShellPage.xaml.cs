using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.ViewModels;
using Snipdeck.Execution.Abstractions;
using Snipdeck.Execution.ViewModels;

using Windows.UI;

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
            if (e.PropertyName == nameof(ShellViewModel.CurrentContent))
            {
                // Wire "Run again" on whichever run view became current (live or replay),
                // re-subscribing idempotently.
                if (ViewModel.CurrentContent is CommandRunViewModel runViewModel)
                {
                    runViewModel.RunAgainRequested -= OnRunAgainRequested;
                    runViewModel.RunAgainRequested += OnRunAgainRequested;
                }

                SyncSelectionFromViewModel();
            }
            else if (e.PropertyName == nameof(ShellViewModel.SelectedTagItem))
            {
                SyncSelectionFromViewModel();
            }
        }

        private async void OnRunAgainRequested(object? sender, Guid snipId)
        {
            await ViewModel.RunSnipByIdAsync(snipId);
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
                // Only the global set maps to the footer item; a CLI-scoped set is
                // reached from the CLI view, so it leaves the footer unselected.
                SharedParametersViewModel { IsGlobal: true } => SharedParametersNavItem,
                TagIconsViewModel => TagsNavItem,
                HistoryViewModel => HistoryNavItem,
                // A live/replay run is a transient destination with no nav entry.
                CommandRunViewModel => null,
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
            else if (ReferenceEquals(item, HistoryNavItem))
            {
                OpenHistory();
            }
            else if (ReferenceEquals(item, SettingsNavItem))
            {
                ViewModel.OpenSettings(App.Services.GetRequiredService<SettingsViewModel>());
            }
            else if (item.Tag is TagItemViewModel tag)
            {
                ViewModel.SelectTag(tag);
            }
        }

        // History view models hold Execution types, so they're built here (the App
        // references Execution) rather than in the Core ShellViewModel. The current
        // snip/CLI names are resolved live from the document via the shell.
        private void OpenHistory()
        {
            var store = App.Services.GetRequiredService<ICommandHistoryStore>();
            var interactions = App.Services.GetRequiredService<IShellInteractions>();
            var history = new HistoryViewModel(store, interactions, ViewModel.ResolveSnipTitle, ViewModel.ResolveCliName);
            history.OpenRequested += OnHistoryOpenRequested;
            ViewModel.CurrentContent = history;
            _ = history.LoadAsync();
        }

        private async void OnHistoryOpenRequested(object? sender, Guid entryId)
        {
            var store = App.Services.GetRequiredService<ICommandHistoryStore>();
            var clipboard = App.Services.GetRequiredService<IClipboardService>();
            var entry = await store.GetAsync(entryId);
            if (entry is null)
            {
                return;
            }

            // Same view as a live run, in replay mode (Cancel disabled, Run again enabled).
            ViewModel.CurrentContent = new CommandRunViewModel(ViewModel.ResolveSnipTitle(entry.SnipId), entry, clipboard);
        }

        private void OnHistorySearchSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (ViewModel.CurrentContent is HistoryViewModel history)
            {
                _ = history.LoadAsync();
            }
        }

        // Keep the active category toggle checked even when it's re-clicked (the
        // category command is a no-op then, so the OneWay binding wouldn't re-assert).
        private void OnHomeCategoryToggled(object sender, RoutedEventArgs e)
        {
            ((ToggleButton)sender).IsChecked = true;
        }

        // The hero image is painted via a Composition mask brush so its lower edge
        // fades to transparent — revealing the page's Mica — with no solid colour
        // (which previously created a hard line / shade mismatch).
        private void OnHeroHostLoaded(object sender, RoutedEventArgs e)
        {
            var host = (Border)sender;
            host.ActualThemeChanged -= OnHeroHostThemeChanged;
            host.ActualThemeChanged += OnHeroHostThemeChanged;
            ApplyHeroVisual(host);
        }

        private void OnHeroHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var host = (Border)sender;
            if (ElementCompositionPreview.GetElementChildVisual(host) is SpriteVisual visual)
            {
                visual.Size = new Vector2((float)host.ActualWidth, (float)host.ActualHeight);
            }
            else
            {
                ApplyHeroVisual(host);
            }
        }

        private void OnHeroHostThemeChanged(FrameworkElement sender, object args)
        {
            ApplyHeroVisual((Border)sender);
        }

        private static void ApplyHeroVisual(Border host)
        {
            if (host.ActualWidth <= 0 || host.ActualHeight <= 0)
            {
                return;
            }

            var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;

            var uri = new Uri(host.ActualTheme == ElementTheme.Dark
                ? "ms-appx:///Assets/HomeHeroDark.png"
                : "ms-appx:///Assets/HomeHeroLight.png");
            var surfaceBrush = compositor.CreateSurfaceBrush(LoadedImageSurface.StartLoadFromUri(uri));
            surfaceBrush.Stretch = CompositionStretch.UniformToFill;

            // Mask alpha: white = visible, transparent = hidden. The image fades out
            // toward the bottom, revealing the page background behind it.
            var gradient = compositor.CreateLinearGradientBrush();
            gradient.StartPoint = new Vector2(0f, 0f);
            gradient.EndPoint = new Vector2(0f, 1f);
            gradient.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.White));
            gradient.ColorStops.Add(compositor.CreateColorGradientStop(0.6f, Colors.White));
            gradient.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, Color.FromArgb(0, 255, 255, 255)));

            var mask = compositor.CreateMaskBrush();
            mask.Source = surfaceBrush;
            mask.Mask = gradient;

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = mask;
            visual.Size = new Vector2((float)host.ActualWidth, (float)host.ActualHeight);

            ElementCompositionPreview.SetElementChildVisual(host, visual);
        }

        private async void OnCopyCloneCommandClicked(object sender, RoutedEventArgs e)
        {
            var clipboard = App.Services.GetRequiredService<IClipboardService>();
            await clipboard.SetTextAsync("git clone https://github.com/StuartMeeks/Snipdeck");
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
