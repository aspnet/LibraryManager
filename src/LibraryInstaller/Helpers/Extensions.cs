using Microsoft.Web.LibraryManager.Contracts;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// A collection of extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns true if the <see cref="ILibraryInstallationState"/> is valid.
        /// </summary>
        /// <param name="state">The state to test.</param>
        /// <param name="errors">The errors contained in the <see cref="ILibraryInstallationState"/> if any.</param>
        /// <returns>
        ///   <c>true</c> if the specified state is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValid(this ILibraryInstallationState state, out IEnumerable<IError> errors)
        {
            errors = null;
            var list = new List<IError>();

            if (state == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(state.ProviderId))
            {
                list.Add(PredefinedErrors.ProviderIsUndefined());
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
