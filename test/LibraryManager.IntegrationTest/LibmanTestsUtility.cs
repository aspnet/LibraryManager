using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Test.Apex.Editor;
using Microsoft.Test.Apex.VisualStudio.Editor;
using Omni.Common;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    public class LibmanTestsUtility
    {
        public static string WaitForCompletionEntries(IVisualStudioTextEditorTestExtension editor, IEnumerable<string> expectedCompletionEntries, bool caseInsensitive, int timeout)
        {
            string errorMessage = null;
            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    IVisualStudioCompletionListTestExtension completionList = editor.Intellisense.GetActiveCompletionList();
                    if (completionList == null)
                    {
                        errorMessage = "Completion list not present.";
                        return false;
                    }

                    Dictionary<string, CompletionItem> comparisonSet = null;
                    StringComparer comparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                    if (caseInsensitive)
                    {
                        comparisonSet = completionList.Items.ToDictionary(x => x.Text, x => x, comparer);
                    }

                    errorMessage = null;
                    if (expectedCompletionEntries != null)
                    {
                        foreach (string curEntry in expectedCompletionEntries)
                        {
                            CompletionItem entry;

                            if (caseInsensitive)
                            {
                                comparisonSet.TryGetValue(curEntry, out entry);
                            }
                            else
                            {
                                entry = completionList[curEntry];
                            }

                            if (entry == null)
                            {
                                errorMessage = String.Concat(errorMessage, "\r\nTimed out waiting for completion entry: ", curEntry, ".");
                            }
                        }
                    }
                }
                catch (EditorException exc)
                {
                    errorMessage = exc.ToString();
                }

                return (errorMessage == null);
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return errorMessage;
        }
    }
}
