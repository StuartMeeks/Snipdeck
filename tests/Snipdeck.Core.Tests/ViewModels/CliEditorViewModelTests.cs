using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class CliEditorViewModelTests
    {
        [Fact]
        public void BuildUpdatedCli_applies_the_edited_name_and_keeps_id_and_icon()
        {
            var cli = new Cli { Name = "pl-app", IconRef = "icons/pl.png" };
            var vm = new CliEditorViewModel(cli) { Name = "  pl  " };

            var updated = vm.BuildUpdatedCli();

            Assert.Equal(cli.Id, updated.Id);
            Assert.Equal("pl", updated.Name);
            Assert.Equal("icons/pl.png", updated.IconRef);
        }

        [Fact]
        public void BuildUpdatedCli_preserves_shared_parameters()
        {
            // A rename/icon edit must not drop CLI-scoped parameter definitions.
            var cli = new Cli
            {
                Name = "pl-app",
                Parameters = [new Parameter { Name = "env", Type = ParameterType.Choice, Options = ["dev", "prod"] }],
            };
            var vm = new CliEditorViewModel(cli) { Name = "renamed" };

            var updated = vm.BuildUpdatedCli();

            var p = Assert.Single(updated.Parameters);
            Assert.Equal("env", p.Name);
            Assert.Equal(ParameterType.Choice, p.Type);
            Assert.Equal(["dev", "prod"], p.Options);
        }
    }
}
