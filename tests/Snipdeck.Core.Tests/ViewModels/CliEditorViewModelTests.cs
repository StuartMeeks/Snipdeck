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
        public void Loads_and_rebuilds_shared_parameters_through_editor_rows()
        {
            // Existing CLI params load into editor rows and round-trip on save.
            var cli = new Cli
            {
                Name = "pl-app",
                Parameters = [new Parameter { Name = "env", Type = ParameterType.Choice, Options = ["dev", "prod"] }],
            };
            var vm = new CliEditorViewModel(cli) { Name = "renamed" };

            Assert.Equal("env", Assert.Single(vm.Parameters).Name);

            var updated = vm.BuildUpdatedCli();
            var p = Assert.Single(updated.Parameters);
            Assert.Equal("env", p.Name);
            Assert.Equal(ParameterType.Choice, p.Type);
            Assert.Equal(["dev", "prod"], p.Options);
        }

        [Fact]
        public void Add_and_remove_parameter_rows_reflect_in_the_built_cli()
        {
            var vm = new CliEditorViewModel(new Cli { Name = "pl-app" });
            Assert.Empty(vm.Parameters);

            vm.AddParameter();
            vm.Parameters[0].Name = "region";
            Assert.Single(vm.BuildUpdatedCli().Parameters);

            vm.RemoveParameter(vm.Parameters[0]);
            Assert.Empty(vm.BuildUpdatedCli().Parameters);
        }
    }
}
