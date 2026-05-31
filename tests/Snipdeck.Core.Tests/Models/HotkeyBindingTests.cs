using Snipdeck.Core.Models;

namespace Snipdeck.Core.Tests.Models
{
    public class HotkeyBindingTests
    {
        [Fact]
        public void Default_is_ctrl_alt_s()
        {
            var d = HotkeyBinding.Default;
            Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, d.Modifiers);
            Assert.Equal("S", d.Key);
            Assert.True(d.IsValid);
            Assert.Equal("Ctrl+Alt+S", d.ToDisplayString());
        }

        [Fact]
        public void IsValid_requires_a_modifier_and_a_key()
        {
            Assert.False(new HotkeyBinding { Modifiers = HotkeyModifiers.None, Key = "S" }.IsValid);
            Assert.False(new HotkeyBinding { Modifiers = HotkeyModifiers.Control, Key = "" }.IsValid);
            Assert.True(new HotkeyBinding { Modifiers = HotkeyModifiers.Control, Key = "S" }.IsValid);
        }

        [Fact]
        public void ToDisplayString_orders_modifiers_and_handles_unbound()
        {
            Assert.Equal("(unbound)", new HotkeyBinding().ToDisplayString());
            var all = new HotkeyBinding
            {
                Modifiers = HotkeyModifiers.Windows | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control,
                Key = "F5",
            };
            Assert.Equal("Ctrl+Alt+Shift+Win+F5", all.ToDisplayString());
        }
    }
}
