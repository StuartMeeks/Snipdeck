namespace Snipdeck.Execution.ViewModels
{
    /// <summary>Lifecycle state of a command run.</summary>
    public enum RunStatus
    {
        /// <summary>Created but not yet started.</summary>
        Pending,

        /// <summary>Process is running; output is streaming.</summary>
        Running,

        /// <summary>Process finished (see the exit code).</summary>
        Finished,

        /// <summary>The user cancelled the run before it completed.</summary>
        Cancelled,
    }
}
