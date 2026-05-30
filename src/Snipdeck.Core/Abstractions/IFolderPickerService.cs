namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// Lets the user pick a directory (e.g. a new storage location). Returns the
    /// absolute path, or null if the picker was cancelled.
    /// </summary>
    public interface IFolderPickerService
    {
        Task<string?> PickFolderAsync();
    }
}
