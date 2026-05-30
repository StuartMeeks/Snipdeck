namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// What changing the storage directory to a given target implies.
    /// </summary>
    public enum StorageChangeOutcome
    {
        /// <summary>Target is the current directory — nothing to do.</summary>
        NoChange,

        /// <summary>
        /// Target is nested inside the current storage directory (or vice
        /// versa) — relocating would copy a directory into itself / delete the
        /// copy. Reject it.
        /// </summary>
        Invalid,

        /// <summary>Neither current nor target holds a store — just adopt the empty target.</summary>
        SetEmptyTarget,

        /// <summary>Current holds a store and the target is empty — move the store there.</summary>
        MoveToTarget,

        /// <summary>The target already holds a store — adopt it (the current store is left in place).</summary>
        AdoptTarget,
    }

    /// <summary>
    /// Decides what a storage-directory change means and performs the on-disk
    /// relocation. UI-free and filesystem-only so it can be unit-tested against
    /// real temp directories.
    /// </summary>
    public interface IStorageRelocationService
    {
        /// <summary>The store file name within a storage directory (e.g. "store.json").</summary>
        string StoreFileName { get; }

        /// <summary>Classifies a proposed change from <paramref name="currentDirectory"/> to <paramref name="targetDirectory"/>.</summary>
        StorageChangeOutcome Inspect(string currentDirectory, string targetDirectory);

        /// <summary>
        /// Copies the store file and the icons subdirectory from the current
        /// directory to the target. Non-destructive — the originals are left in
        /// place so the new config can be persisted durably before
        /// <see cref="RemoveStore"/> removes them.
        /// </summary>
        void CopyStore(string currentDirectory, string targetDirectory);

        /// <summary>Removes the store file and icons subdirectory from a directory.</summary>
        void RemoveStore(string directory);
    }
}
