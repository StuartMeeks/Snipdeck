using Snipdeck.Core.Abstractions;

namespace Snipdeck.Core.Tests.Support
{
    public sealed class FakeFolderPickerService : IFolderPickerService
    {
        public string? NextFolder { get; set; }

        public Task<string?> PickFolderAsync() => Task.FromResult(NextFolder);
    }
}
