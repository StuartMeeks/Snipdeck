using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Snipdeck.Core.Engine;
using Snipdeck.Core.Models;

namespace Snipdeck.Core.ViewModels
{
    public sealed partial class SnipEditorViewModel : ObservableObject
    {
        public SnipEditorViewModel(Snip snip, IReadOnlyList<Cli>? availableClis = null)
        {
            ArgumentNullException.ThrowIfNull(snip);

            Snip = snip;
            Title = snip.Title;
            CommandTemplate = snip.CommandTemplate;
            Description = snip.Description ?? string.Empty;
            TagsText = string.Join(", ", snip.Tags);
            Parameters = new ObservableCollection<ParameterEditorRowViewModel>(
                snip.Parameters.Select(p => new ParameterEditorRowViewModel(p)));

            AvailableClis = new ObservableCollection<Cli>(availableClis ?? []);
            SelectedCli = AvailableClis.FirstOrDefault(c => c.Id == snip.CliId);
        }

        public Snip Snip { get; }

        /// <summary>The CLIs the snip may belong to, so the user can re-home it.</summary>
        public ObservableCollection<Cli> AvailableClis { get; }

        /// <summary>
        /// The CLI the snip will belong to once saved. Initialised from the snip's current
        /// CLI; changing it moves the snip to a different CLI.
        /// </summary>
        [ObservableProperty]
        public partial Cli? SelectedCli { get; set; }

        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CommandTemplate { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Description { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TagsText { get; set; } = string.Empty;

        public ObservableCollection<ParameterEditorRowViewModel> Parameters { get; }

        public bool CanSave =>
            !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(CommandTemplate);

        public void AddParameter()
        {
            Parameters.Add(new ParameterEditorRowViewModel(new Parameter { Name = "param" }));
        }

        public void RemoveParameter(ParameterEditorRowViewModel row)
        {
            _ = Parameters.Remove(row);
        }

        public Snip BuildUpdatedSnip()
        {
            return new Snip
            {
                Id = Snip.Id,
                CliId = SelectedCli?.Id ?? Snip.CliId,
                Title = Title.Trim(),
                CommandTemplate = CommandTemplate.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Tags = TagParser.Parse(TagsText),
                IsFavourite = Snip.IsFavourite,
                IsTrash = Snip.IsTrash,
                UsageCount = Snip.UsageCount,
                LastUsedAt = Snip.LastUsedAt,
                Parameters = [.. Parameters.Select(r => r.BuildParameter())],
            };
        }
    }
}
