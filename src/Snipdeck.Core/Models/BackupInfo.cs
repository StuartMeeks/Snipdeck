namespace Snipdeck.Core.Models
{
    public sealed record BackupInfo(string FilePath, DateTimeOffset CreatedAtUtc, long SizeBytes);
}
