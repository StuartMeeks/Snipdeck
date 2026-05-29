using Snipdeck.Core.Models;
using Snipdeck.Core.ViewModels;

namespace Snipdeck.Core.Tests.ViewModels
{
    public class SnipEditorViewModelTests
    {
        [Fact]
        public void Loads_initial_values_from_the_snip()
        {
            var snip = new Snip
            {
                Title = "Deploy",
                CommandTemplate = "deploy --env {env}",
                Description = "Deploys to env",
                Tags = ["deploy", "prod"],
                Parameters = [new Parameter { Name = "env", Type = ParameterType.Choice, Options = ["dev", "prod"], Default = "dev" }],
            };

            var vm = new SnipEditorViewModel(snip);

            Assert.Equal("Deploy", vm.Title);
            Assert.Equal("deploy --env {env}", vm.CommandTemplate);
            Assert.Equal("Deploys to env", vm.Description);
            Assert.Equal("deploy, prod", vm.TagsText);
            Assert.Single(vm.Parameters);
        }

        [Fact]
        public void CanSave_requires_non_empty_title_and_template()
        {
            var snip = new Snip { Title = "", CommandTemplate = "" };
            var vm = new SnipEditorViewModel(snip);
            Assert.False(vm.CanSave);

            vm.Title = "x";
            Assert.False(vm.CanSave);

            vm.CommandTemplate = "echo";
            Assert.True(vm.CanSave);

            vm.Title = "   ";
            Assert.False(vm.CanSave);
        }

        [Fact]
        public void AddParameter_appends_an_editable_row()
        {
            var vm = new SnipEditorViewModel(new Snip());

            vm.AddParameter();
            vm.AddParameter();

            Assert.Equal(2, vm.Parameters.Count);
        }

        [Fact]
        public void RemoveParameter_removes_the_given_row()
        {
            var snip = new Snip
            {
                Parameters =
                [
                    new Parameter { Name = "a" },
                    new Parameter { Name = "b" },
                ],
            };
            var vm = new SnipEditorViewModel(snip);

            vm.RemoveParameter(vm.Parameters[0]);

            Assert.Single(vm.Parameters);
            Assert.Equal("b", vm.Parameters[0].Name);
        }

        [Fact]
        public void BuildUpdatedSnip_preserves_identity_and_applies_edits()
        {
            var snip = new Snip
            {
                Id = Guid.NewGuid(),
                CliId = Guid.NewGuid(),
                Title = "Original",
                IsFavourite = true,
                UsageCount = 3,
            };
            var vm = new SnipEditorViewModel(snip)
            {
                Title = "Updated",
                CommandTemplate = "echo {x}",
                Description = "  ",
                TagsText = "alpha, beta, alpha",
            };
            vm.AddParameter();
            vm.Parameters[0].Name = "x";

            var built = vm.BuildUpdatedSnip();

            Assert.Equal(snip.Id, built.Id);
            Assert.Equal(snip.CliId, built.CliId);
            Assert.Equal("Updated", built.Title);
            Assert.Equal("echo {x}", built.CommandTemplate);
            Assert.Null(built.Description);
            Assert.Equal(2, built.Tags.Count);
            Assert.True(built.IsFavourite);
            Assert.Equal(3, built.UsageCount);
            Assert.Single(built.Parameters);
            Assert.Equal("x", built.Parameters[0].Name);
        }
    }
}
