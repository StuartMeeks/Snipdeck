using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

using Windows.Storage.Streams;

namespace Snipdeck.App.Controls
{
    /// <summary>
    /// Renders a CLI's icon. Falls back to a deterministic identicon seeded
    /// off <see cref="Seed"/> when <see cref="IconRef"/> is empty or the
    /// referenced file can't be read.
    /// </summary>
    public sealed partial class Identicon : UserControl
    {
        public static readonly DependencyProperty SeedProperty =
            DependencyProperty.Register(nameof(Seed), typeof(Guid), typeof(Identicon),
                new PropertyMetadata(Guid.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconRefProperty =
            DependencyProperty.Register(nameof(IconRef), typeof(string), typeof(Identicon),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        public Identicon()
        {
            InitializeComponent();
        }

        public Guid Seed
        {
            get => (Guid)GetValue(SeedProperty);
            set => SetValue(SeedProperty, value);
        }

        public string? IconRef
        {
            get => (string?)GetValue(IconRefProperty);
            set => SetValue(IconRefProperty, value);
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Identicon icon)
            {
                _ = icon.UpdateImageAsync();
            }
        }

        private async Task UpdateImageAsync()
        {
            // Prefer the uploaded icon if present.
            var storage = App.Services.GetService(typeof(IIconAssetStorage)) as IIconAssetStorage;
            var absolute = storage?.ResolveAbsolutePath(IconRef);
            if (!string.IsNullOrEmpty(absolute) && System.IO.File.Exists(absolute))
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(absolute);
                IconImage.Source = await DecodeAsync(bytes);
                return;
            }

            if (Seed == Guid.Empty)
            {
                IconImage.Source = null;
                return;
            }

            var identicon = IdenticonService.GeneratePng(Seed);
            IconImage.Source = await DecodeAsync(identicon);
        }

        private static async Task<BitmapImage> DecodeAsync(byte[] bytes)
        {
            var image = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(stream);
            writer.WriteBytes(bytes);
            _ = await writer.StoreAsync();
            _ = writer.DetachStream();
            stream.Seek(0);
            await image.SetSourceAsync(stream);
            return image;
        }
    }
}
