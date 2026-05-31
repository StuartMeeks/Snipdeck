using Microsoft.UI.Xaml.Controls;

using Snipdeck.Core.ViewModels;

namespace Snipdeck.App.Views
{
    public sealed partial class ParameterEditorDialog : ContentDialog
    {
        public ParameterEditorDialog(string title, ParameterEditorRowViewModel row)
        {
            ArgumentNullException.ThrowIfNull(row);
            Row = row;
            InitializeComponent();
            Title = title;
        }

        public ParameterEditorRowViewModel Row { get; }
    }
}
