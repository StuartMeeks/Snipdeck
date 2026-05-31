using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Support
{
    /// <summary>
    /// Programmable <see cref="IHotkeyService"/> double. Set
    /// <see cref="NextRegisterResult"/> to control whether registration
    /// "succeeds"; inspect <see cref="LastRegistered"/> / <see cref="RegisterCount"/>.
    /// </summary>
    public sealed class FakeHotkeyService : IHotkeyService
    {
        public bool NextRegisterResult { get; set; } = true;

        public HotkeyBinding? LastRegistered { get; private set; }

        public int RegisterCount { get; private set; }

        public int UnregisterCount { get; private set; }

#pragma warning disable CS0067 // Pressed is required by the interface but unused in tests.
        public event EventHandler? Pressed;
#pragma warning restore CS0067

        public bool TryRegister(HotkeyBinding binding)
        {
            RegisterCount++;
            if (NextRegisterResult)
            {
                LastRegistered = binding;
            }
            return NextRegisterResult;
        }

        public void Unregister() => UnregisterCount++;
    }
}
