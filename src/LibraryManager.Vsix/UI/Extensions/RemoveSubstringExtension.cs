using System;
using System.Windows.Markup;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Extensions
{
    [MarkupExtensionReturnType(typeof(string))]
    internal class RemoveSubstringExtension : MarkupExtension
    {
        private object _text;
        private string _remove;

        public RemoveSubstringExtension(object text)
            : this(text, null)
        {
        }

        public RemoveSubstringExtension(object text, string remove)
        {
            _text = text;
            _remove = remove;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_text == null)
            {
                return null;
            }

            string remove = _remove ?? "_";
            string s = null;

            MarkupExtension m = _text as MarkupExtension;
            if (m != null)
            {
                s = m.ProvideValue(serviceProvider) as string;
            }

            if (s == null)
            {
                s = _text as string;
            }

            if (s != null)
            {
                return s.Replace(remove, string.Empty);
            }

            return null;
        }
    }
}
