using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Support
{
    /// <summary>
    /// Programmable test double for <see cref="IShellInteractions"/>. Set the
    /// <c>Next*</c> properties before triggering a command, then inspect the
    /// <c>Last*</c> properties after.
    /// </summary>
    public sealed class FakeShellInteractions : IShellInteractions
    {
        public bool NextConfirmResult { get; set; }

        public SnipEditResult? NextSnipEditResult { get; set; }

        public CliEditResult? NextCliEditResult { get; set; }

        public ParameterFillResult? NextParameterFillResult { get; set; }

        public string? LastConfirmTitle { get; private set; }

        public string? LastNotifyTitle { get; private set; }

        public string? LastNotifyMessage { get; private set; }

        public int NotifyCount { get; private set; }

        public Snip? LastEditedSnip { get; private set; }

        public Cli? LastEditedCli { get; private set; }

        public Snip? LastFilledSnip { get; private set; }

        public Task<bool> ConfirmAsync(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "Cancel")
        {
            LastConfirmTitle = title;
            return Task.FromResult(NextConfirmResult);
        }

        public Task NotifyAsync(string title, string message, string buttonText = "OK")
        {
            LastNotifyTitle = title;
            LastNotifyMessage = message;
            NotifyCount++;
            return Task.CompletedTask;
        }

        public Task<SnipEditResult?> EditSnipAsync(Snip snip, IReadOnlyList<Cli> availableClis)
        {
            LastEditedSnip = snip;
            return Task.FromResult(NextSnipEditResult);
        }

        public Task<CliEditResult?> EditCliAsync(Cli cli)
        {
            LastEditedCli = cli;
            return Task.FromResult(NextCliEditResult);
        }

        public Task<ParameterFillResult?> FillParametersAsync(Snip snip)
        {
            LastFilledSnip = snip;
            return Task.FromResult(NextParameterFillResult);
        }
    }
}
