using System;
using System.Collections.Generic;
using System.IO;
using Omni.Common;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    public class FileIOHelper
    {
        private delegate string WaiterDelegate(string currentWorkingDirectory, IEnumerable<string> files, bool caseInsensitive, int timeout);

        private static HashSet<string> GetSubDirectoriesAndFiles(string currentWorkingDirectory, bool caseInsensitive)
        {
            if(!Directory.Exists(currentWorkingDirectory))
            {
                return new HashSet<string>();
            }

            StringComparer comparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            IEnumerable<string> subItems = Directory.EnumerateFileSystemEntries(currentWorkingDirectory, "*", SearchOption.AllDirectories);

            return new HashSet<string>(subItems, comparer);
        }

        private static void WaitForFiles(string currentWorkingDirectory, IEnumerable<string> files, bool caseInsensitive, int timeout, WaiterDelegate waiter)
        {
            string errorMessage = waiter(currentWorkingDirectory, files, caseInsensitive, timeout);

            if (errorMessage != null)
            {
                string newErrorMessage = waiter(currentWorkingDirectory, files, caseInsensitive, timeout);

                if (newErrorMessage != null)
                {
                    errorMessage = String.Concat(errorMessage, "\r\nFailed even when forcing completion with double timeout");
                }
                else
                {
                    errorMessage = String.Concat(errorMessage, "\r\n*Didn't* fail when forcing completion with double timeout");
                }

                throw new TimeoutException(errorMessage);
            }
        }

        public void WaitForRestoredFiles(string currentWorkingDirectory, IEnumerable<string> expectedFiles, bool caseInsensitive, int timeout = 10000)
        {
            WaitForFiles(currentWorkingDirectory, expectedFiles, caseInsensitive, timeout, WaitForRestoredFilesHelper);
        }

        public void WaitForRestoredFile(string currentWorkingDirectory, string expectedFile, bool caseInsensitive, int timeout = 10000)
        {
            WaitForRestoredFiles(currentWorkingDirectory, new[] { expectedFile }, caseInsensitive, timeout);
        }

        // TODO: This method expects that expectedFiles will be full paths, not relative to currentWorkingDirectory.  Instead it should use relative paths.
        //       With the current design, each caller basically uses the form WaitFor*File(dir, Path.Combine(dir, expectedFile)), which is a silly API to use.
        private static string WaitForRestoredFilesHelper(string currentWorkingDirectory, IEnumerable<string> expectedFiles, bool caseInsensitive, int timeout)
        {
            string errorMessage = null;

            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    HashSet<string> subItems = GetSubDirectoriesAndFiles(currentWorkingDirectory, caseInsensitive);

                    if (subItems.Count == 0)
                    {
                        errorMessage = "Restore failed for the library.";
                        return false;
                    }

                    errorMessage = null;
                    if (expectedFiles != null)
                    {
                        foreach (string item in expectedFiles)
                        {
                            if (!subItems.Contains(item))
                            {
                                errorMessage = string.Concat(errorMessage, "\r\nTimed out waiting for: ", item, ".");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    errorMessage = exc.ToString();
                }

                return errorMessage == null;
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return errorMessage;
        }

        public void WaitForDeletedFiles(string currentWorkingDirectory, IEnumerable<string> deletedFiles, bool caseInsensitive, int timeout = 10000)
        {
            WaitForFiles(currentWorkingDirectory, deletedFiles, caseInsensitive, timeout, WaitForDeletedFilesHelper);
        }

        public void WaitForDeletedFile(string currentWorkingDirectory, string deletedFile, bool caseInsensitive, int timeout = 10000)
        {
            WaitForDeletedFiles(currentWorkingDirectory, new[] { deletedFile }, caseInsensitive, timeout);
        }

        private static string WaitForDeletedFilesHelper(string currentWorkingDirectory, IEnumerable<string> deletedFiles, bool caseInsensitive, int timeout)
        {
            string errorMessage = null;

            WaitFor.TryIsTrue(() =>
            {
                try
                {
                    HashSet<string> subItems = GetSubDirectoriesAndFiles(currentWorkingDirectory, caseInsensitive);

                    errorMessage = null;
                    if (deletedFiles != null)
                    {
                        foreach (string item in deletedFiles)
                        {
                            if (subItems.Contains(item))
                            {
                                errorMessage = string.Concat(errorMessage, "\r\nTimed out waiting for: ", item, " to be deleted.");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    errorMessage = exc.ToString();
                }

                return errorMessage == null;
            }, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(500));

            return errorMessage;
        }
    }
}
