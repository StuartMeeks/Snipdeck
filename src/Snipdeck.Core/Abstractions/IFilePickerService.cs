namespace Snipdeck.Core.Abstractions
{
    public sealed record PickedFile(string FileName, byte[] Bytes);

    public interface IFilePickerService
    {
        Task<PickedFile?> PickImageAsync();
    }
}
