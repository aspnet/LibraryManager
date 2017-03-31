using System.Windows;

namespace LibraryInstaller.Vsix.Controls.Search
{
    public class Wrapper : DependencyObject
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