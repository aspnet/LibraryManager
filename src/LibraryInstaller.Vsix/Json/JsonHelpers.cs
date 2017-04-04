using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Parser.TreeItems;

namespace LibraryInstaller.Vsix
{
    public static class JsonHelpers
    {
        public static bool TryGetProviderId(JSONObject parent, out string providerId, out string libraryId)
        {
            providerId = null;
            libraryId = null;

            if (parent == null)
                return false;

            foreach (JSONMember child in parent.Children.OfType<JSONMember>())
            {
                if (child.UnquotedNameText == "provider")
                    providerId = child.UnquotedValueText;
                else if (child.UnquotedNameText == "id")
                    libraryId = child.UnquotedValueText;
            }

            return !string.IsNullOrEmpty(providerId);
        }
    }
}
