using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class ParametersEditorViewModelTests
    {
        [Fact]
        public void Loads_existing_parameters_into_rows()
        {
            var vm = new ParametersEditorViewModel("Shared parameters", [new Parameter { Name = "tenant" }]);
            Assert.Equal("tenant", Assert.Single(vm.Parameters).Name);
            Assert.False(vm.IsEmpty);
        }

        [Fact]
        public void Add_then_build_yields_the_new_parameter()
        {
            var vm = new ParametersEditorViewModel("Shared parameters", []);
            Assert.True(vm.IsEmpty);

            vm.AddParameterCommand.Execute(null);
            vm.Parameters[0].Name = "yes_no";
            vm.Parameters[0].TypeIndex = 1; // Choice
            vm.Parameters[0].OptionsText = "yes, no";

            var built = vm.BuildParameters();
            var p = Assert.Single(built);
            Assert.Equal("yes_no", p.Name);
            Assert.Equal(ParameterType.Choice, p.Type);
            Assert.Equal(["yes", "no"], p.Options);
        }

        [Fact]
        public void Remove_drops_the_row()
        {
            var vm = new ParametersEditorViewModel("Shared parameters", [new Parameter { Name = "a" }, new Parameter { Name = "b" }]);

            vm.RemoveParameterCommand.Execute(vm.Parameters[0]);

            Assert.Equal("b", Assert.Single(vm.Parameters).Name);
        }
    }
}
