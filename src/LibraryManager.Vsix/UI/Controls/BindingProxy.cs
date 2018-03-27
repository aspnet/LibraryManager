using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    [ContentProperty(nameof(Input))]
    internal class BindingProxy : FrameworkElement
    {
        public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
            nameof(Input), typeof(object), typeof(BindingProxy), new PropertyMetadata(default(object), InputChanged));

        public static readonly DependencyProperty OutputProperty = DependencyProperty.Register(
            nameof(Output), typeof(object), typeof(BindingProxy), new PropertyMetadata(default(object)));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public object Input
        {
            get { return (string)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public object Output
        {
            get { return (string)GetValue(OutputProperty); }
            set { SetValue(OutputProperty, value); }
        }

        private static void InputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BindingProxy)d).Output = e.NewValue;
        }
    }
}
