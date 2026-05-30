namespace Snipdeck.Core.Abstractions
{
    /// <summary>
    /// What changing the storage directory to a given target implies.
    /// </summary>
    public enum StorageChangeOutcome
    {
        /// <summary>Target is the current directory — nothing to do.</summary>
        NoChange,

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
        /// Moves the store file and the icons subdirectory from the current
        /// directory to the target (copy-then-remove). Only valid for
        /// <see cref="StorageChangeOutcome.MoveToTarget"/>.
        /// </summary>
        void MoveStore(string currentDirectory, string targetDirectory);
    }
}
