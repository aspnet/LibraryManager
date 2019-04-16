using System;
using System.Windows.Markup;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Extensions
{
    [MarkupExtensionReturnType(typeof(string))]
    internal class RemoveCharacterExtension : MarkupExtension
    {
        private object _text;
        private string _remove;

        public RemoveCharacterExtension(object text, string remove)
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

            for (int i = 0; i < _remove.Length; i++)
            {
                if (s != null)
                {
                    s = s.Replace(_remove[i].ToString(), string.Empty);
                }
            }

            return s;
        }
    }
}
