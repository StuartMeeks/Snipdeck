using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Snipdeck.Core.Services;

using Windows.Storage.Streams;

namespace Snipdeck.App.Controls
{
    /// <summary>
    /// Renders an identicon from a <see cref="Guid"/> seed. The seed should be
    /// the immutable <c>Cli.Id</c> so renaming a CLI doesn't change its icon.
    /// </summary>
    public sealed partial class Identicon : UserControl
    {
        public static readonly DependencyProperty SeedProperty =
            DependencyProperty.Register(
                nameof(Seed),
                typeof(Guid),
                typeof(Identicon),
                new PropertyMetadata(Guid.Empty, OnSeedChanged));

        public Identicon()
        {
            InitializeComponent();
        }

        public Guid Seed
        {
            get => (Guid)GetValue(SeedProperty);
            set => SetValue(SeedProperty, value);
        }

        private static void OnSeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Identicon icon)
            {
                _ = icon.UpdateImageAsync();
            }
        }

        private async Task UpdateImageAsync()
        {
            if (Seed == Guid.Empty)
            {
                IconImage.Source = null;
                return;
            }

            var bytes = IdenticonService.GeneratePng(Seed);
            var image = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(stream);
            writer.WriteBytes(bytes);
            _ = await writer.StoreAsync();
            _ = writer.DetachStream();
            stream.Seek(0);
            await image.SetSourceAsync(stream);
            IconImage.Source = image;
        }
    }
}
