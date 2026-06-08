namespace Snipdeck.Core.Abstractions
{
    public sealed record PickedFile(string FileName, byte[] Bytes);

    public interface IFilePickerService
    {
        Task<PickedFile?> PickImageAsync();

        /// <summary>
        /// Picks an executable and returns its absolute path (not its bytes), or null if
        /// cancelled. Used to set a CLI's optional executable path.
        /// </summary>
        Task<string?> PickExecutablePathAsync();
    }
}
