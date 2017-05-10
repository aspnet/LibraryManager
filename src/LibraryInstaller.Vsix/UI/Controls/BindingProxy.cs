using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls
{
    [ContentProperty(nameof(Input))]
    public class BindingProxy : FrameworkElement
    {
        public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
            nameof(Input), typeof(object), typeof(BindingProxy), new PropertyMetadata(default(object), InputChanged));

        public static readonly DependencyProperty OutputProperty = DependencyProperty.Register(
            nameof(Output), typeof(object), typeof(BindingProxy), new PropertyMetadata(default(object)));

        private static void InputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BindingProxy)d).Output = e.NewValue;
        }

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
    }

    [ContentProperty(nameof(Binding))]
    public class MultiBindingAdapter : MarkupExtension
    {
        private MultiBinding _binding;

        private BindingProxy _proxy = new BindingProxy();
        private Binding _target;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public MultiBinding Binding
        {
            get { return _binding; }
            set
            {
                _binding = value;
                _proxy.SetBinding(BindingProxy.InputProperty, _binding);
                _target = new Binding
                {
                    Source = _proxy,
                    Path = new PropertyPath("Output")
                };
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _target.ProvideValue(serviceProvider);
        }

        public static implicit operator Binding(MultiBindingAdapter self)
        {
            return self._target;
        }
    }
}
