using Snipdeck.Core.Models;

namespace Snipdeck.Core.Abstractions
{
    public interface IBackupService
    {
        string BackupDirectory { get; }

        Task<BackupInfo?> CreateBackupAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default);
    }
}
