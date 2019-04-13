// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows.Markup;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Extensions
{
    [MarkupExtensionReturnType(typeof(string))]
    internal class RemoveSubstringExtension : MarkupExtension
    {
        private readonly object _text;
        private readonly string _remove;

        public RemoveSubstringExtension(object text)
            : this(text, null)
        {
        }

        public RemoveSubstringExtension(object text, string remove)
        {
            _text = text;
            _remove = remove ?? "_";
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_text == null)
            {
                return null;
            }

            string s = null;

            if (_text is MarkupExtension m)
            {
                s = m.ProvideValue(serviceProvider) as string;
            }

            if (s == null)
            {
                s = _text as string;
            }

            if (s != null)
            {
                return s.Replace(_remove, string.Empty);
            }

            return null;
        }
    }
}
