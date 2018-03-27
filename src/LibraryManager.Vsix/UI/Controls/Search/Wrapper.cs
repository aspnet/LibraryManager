using System.Windows;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls.Search
{
    internal class Wrapper : DependencyObject
    {
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(
            "Parameter", typeof (bool), typeof (Wrapper), new PropertyMetadata(default(bool)));

        public bool Parameter
        {
            get { return (bool) GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }
    }
}