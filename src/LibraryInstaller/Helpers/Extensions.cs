using Microsoft.Web.LibraryInstaller.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryInstaller
{
    internal static class Extensions
    {
        public static bool IsValid(this ILibraryInstallationState state, out IEnumerable<IError> errors)
        {
            errors = null;
            var list = new List<IError>();

            if (state == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(state.DestinationPath))
            {
                list.Add(PredefinedErrors.PathIsUndefined());
            }

            if (string.IsNullOrEmpty(state.LibraryId))
            {
                list.Add(PredefinedErrors.LibraryIdIsUndefined());
            }

            errors = list;

            return list.Count == 0;
        }
    }
}
