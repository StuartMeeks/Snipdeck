using System.Text.Json;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using Snipdeck.Execution.ViewModels;

namespace Snipdeck.App.Controls
{
    /// <summary>
    /// Hosts a WebView2 running a locally-vendored xterm.js terminal. Output bytes
    /// from the run view model are written to xterm; keystrokes and resize events
    /// flow back to the view model (and thence to the PTY). Live and replay runs both
    /// use this control: a replay simply writes the stored raw stream once.
    /// </summary>
    public sealed partial class TerminalView : UserControl
    {
        public static readonly DependencyProperty RunViewModelProperty =
            DependencyProperty.Register(
                nameof(RunViewModel),
                typeof(CommandRunViewModel),
                typeof(TerminalView),
                new PropertyMetadata(null, OnRunViewModelChanged));

        private bool _webReady;
        private bool _wired;

        public TerminalView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public CommandRunViewModel? RunViewModel
        {
            get => (CommandRunViewModel?)GetValue(RunViewModelProperty);
            set => SetValue(RunViewModelProperty, value);
        }

        private static void OnRunViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((TerminalView)d).TryWire();

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();

                // The terminal owns the keyboard: stop the WebView grabbing accelerators
                // (Ctrl+F find, Ctrl+P print, F5 reload, …) so keys reach xterm/the PTY.
                var settings = WebView.CoreWebView2.Settings;
                settings.AreBrowserAcceleratorKeysEnabled = false;
                settings.IsZoomControlEnabled = false;
                settings.AreDefaultContextMenusEnabled = false;

                var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "terminal");
                WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "snipdeck.terminal",
                    assetsDirectory,
                    CoreWebView2HostResourceAccessKind.Allow);
                WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                WebView.CoreWebView2.Navigate("https://snipdeck.terminal/terminal.html");
            }
            catch (Exception)
            {
                // WebView2 runtime missing or init failed — the panel stays blank
                // rather than crashing the app.
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (RunViewModel is not null)
            {
                RunViewModel.OutputReceived -= OnOutput;
            }

            if (WebView.CoreWebView2 is not null)
            {
                WebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
        }

        private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            string json;
            try
            {
                json = args.TryGetWebMessageAsString();
            }
            catch (ArgumentException)
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                switch (root.GetProperty("t").GetString())
                {
                    case "ready":
                        _webReady = true;
                        TryWire();
                        break;
                    case "i":
                        if (root.TryGetProperty("d", out var data) && data.GetString() is { } encoded)
                        {
                            RunViewModel?.SendInput(Convert.FromBase64String(encoded));
                        }

                        break;
                    case "r":
                        if (root.TryGetProperty("c", out var columns) && root.TryGetProperty("r", out var rows))
                        {
                            RunViewModel?.ResizeTerminal(columns.GetInt32(), rows.GetInt32());
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed messages.
            }
            catch (FormatException)
            {
                // Ignore a bad base64 payload.
            }
        }

        // Wire up only once both the WebView is ready and the run view model is set,
        // so no early output is lost: subscribe first, then start (or replay).
        private void TryWire()
        {
            if (_wired || !_webReady || RunViewModel is null)
            {
                return;
            }

            _wired = true;
            var viewModel = RunViewModel;
            viewModel.OutputReceived += OnOutput;

            if (viewModel.ReplayOutput is { Length: > 0 } replay)
            {
                PostWrite(replay);
            }
            else
            {
                _ = viewModel.StartAsync();
            }
        }

        private void OnOutput(byte[] chunk) => PostWrite(chunk);

        private void PostWrite(byte[] data)
        {
            var core = WebView.CoreWebView2;
            if (core is null || data.Length == 0)
            {
                return;
            }

            // base64 is JSON- and JS-safe (no quotes or backslashes to escape).
            core.PostWebMessageAsString("{\"t\":\"w\",\"d\":\"" + Convert.ToBase64String(data) + "\"}");
        }
    }
}
