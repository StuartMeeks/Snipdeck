using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ParameterFillViewModelTests
    {
        [Fact]
        public void Snip_with_no_parameters_is_immediately_copy_enabled_with_template_as_preview()
        {
            var snip = new Snip { CommandTemplate = "echo hi" };

            var vm = new ParameterFillViewModel(snip);

            Assert.True(vm.IsCopyEnabled);
            Assert.Equal("echo hi", vm.Preview);
            Assert.Empty(vm.Inputs);
        }

        [Fact]
        public void Parameterless_snip_with_token_like_template_text_stays_copy_enabled()
        {
            // No declared parameters, but the template contains a bare {token} that
            // the engine treats as an unresolved placeholder. There's nothing to
            // fill, so it must copy verbatim (as the direct copy path does) rather
            // than being gated off as "unresolved".
            var snip = new Snip { CommandTemplate = "git commit -m {message}" };

            var vm = new ParameterFillViewModel(snip);

            Assert.Empty(vm.Inputs);
            Assert.True(vm.IsCopyEnabled);
            Assert.Equal("git commit -m {message}", vm.Preview);
        }

        [Fact]
        public void Defaults_pre_fill_inputs_and_drive_copy_enabled_state()
        {
            var snip = new Snip
            {
                CommandTemplate = "echo {name}",
                Parameters = [new Parameter { Name = "name", Default = "world" }],
            };

            var vm = new ParameterFillViewModel(snip);

            Assert.True(vm.IsCopyEnabled);
            Assert.Equal("echo world", vm.Preview);
        }

        [Fact]
        public void Missing_value_keeps_copy_disabled_and_leaves_token_in_preview()
        {
            var snip = new Snip
            {
                CommandTemplate = "echo {name}",
                Parameters = [new Parameter { Name = "name" }],
            };

            var vm = new ParameterFillViewModel(snip);

            Assert.False(vm.IsCopyEnabled);
            Assert.Equal("echo {name}", vm.Preview);
        }

        [Fact]
        public void Editing_an_input_refreshes_the_preview_live()
        {
            var snip = new Snip
            {
                CommandTemplate = "deploy {env}",
                Parameters = [new Parameter { Name = "env", Default = "dev" }],
            };

            var vm = new ParameterFillViewModel(snip);
            Assert.Equal("deploy dev", vm.Preview);

            vm.Inputs[0].Value = "prod";

            Assert.Equal("deploy prod", vm.Preview);
            Assert.True(vm.IsCopyEnabled);
        }

        [Fact]
        public void Multiple_inputs_resolve_independently()
        {
            var snip = new Snip
            {
                CommandTemplate = "git tag -a {tag} -m \"{message}\"",
                Parameters =
                [
                    new Parameter { Name = "tag", Default = "v1.0.0" },
                    new Parameter { Name = "message", Default = "Release" },
                ],
            };

            var vm = new ParameterFillViewModel(snip);

            Assert.Equal("git tag -a v1.0.0 -m \"Release\"", vm.Preview);
            Assert.True(vm.IsCopyEnabled);
        }
    }
}
