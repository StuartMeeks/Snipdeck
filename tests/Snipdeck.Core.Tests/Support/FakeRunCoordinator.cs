using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Support
{
    /// <summary>Programmable <see cref="IRunCoordinator"/>: returns a preset run view and records the call.</summary>
    public sealed class FakeRunCoordinator : IRunCoordinator
    {
        public object? NextResult { get; set; }

        public Snip? LastSnip { get; private set; }

        public Cli? LastCli { get; private set; }

        public int CallCount { get; private set; }

        public Task<object?> CreateRunAsync(Snip snip, Cli? cli, IReadOnlyList<Parameter> resolvedParameters)
        {
            LastSnip = snip;
            LastCli = cli;
            CallCount++;
            return Task.FromResult(NextResult);
        }
    }
}
