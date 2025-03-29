// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Web.LibraryManager.Helpers
{
    /// <summary>
    /// A collection of extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Validates <see cref="ILibraryInstallationState"/>
        /// </summary>
        /// <param name="state">The <see cref="ILibraryInstallationState"/> to validate.</param>
        /// <param name="dependencies">The <see cref="IDependencies"/> used to validate <see cref="ILibraryInstallationState"/></param>
        /// <returns><see cref="OperationResult{LibraryInstallationGoalState}"/> with the result of the validation</returns>
        public static async Task<OperationResult<LibraryInstallationGoalState>> IsValidAsync(this ILibraryInstallationState state, IDependencies dependencies)
        {
            if (state == null)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.UnknownError());
            }

            if (string.IsNullOrEmpty(state.ProviderId))
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.ProviderIsUndefined());
            }

            IProvider provider = dependencies?.GetProvider(state.ProviderId);
            if (provider == null)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.ProviderUnknown(state.ProviderId));
            }

            return await IsValidAsync(state, provider).ConfigureAwait(false);
        }

        /// <summary>
        ///  Validates <see cref="ILibraryInstallationState"/>
        /// </summary>
        /// <param name="state">The <see cref="ILibraryInstallationState"/> to validate.</param>
        /// <param name="provider">The <see cref="IProvider"/> used to validate <see cref="ILibraryInstallationState"/></param>
        /// <returns><see cref="OperationResult{LibraryInstallationGoalState}"/> with the result of the validation</returns>
        public static async Task<OperationResult<LibraryInstallationGoalState>> IsValidAsync(this ILibraryInstallationState state, IProvider provider)
        {
            if (state == null)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.UnknownError());
            }

            if (provider == null)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.ProviderUnknown(string.Empty));
            }

            if (string.IsNullOrEmpty(state.Name))
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.LibraryIdIsUndefined());
            }

            ILibraryCatalog catalog = provider.GetCatalog();
            try
            {
                await catalog.GetLibraryAsync(state.Name, state.Version, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.UnableToResolveSource(state.Name, state.Version, provider.Id));
            }

            if (string.IsNullOrEmpty(state.DestinationPath))
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.PathIsUndefined());
            }

            if (state.DestinationPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return OperationResult<LibraryInstallationGoalState>.FromError(PredefinedErrors.DestinationPathHasInvalidCharacters(state.DestinationPath));
            }

            OperationResult<LibraryInstallationGoalState> goalStateResult = await provider.GetInstallationGoalStateAsync(state, CancellationToken.None);

            return goalStateResult;
        }

        /// <summary>
        /// Returns files from <paramref name="files"/> that are not part of the <paramref name="library"/>
        /// </summary>
        /// <param name="library"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public static IReadOnlyList<string> GetInvalidFiles(this ILibrary library, IReadOnlyList<string> files)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            var invalidFiles = new List<string>();

            if (files == null || !files.Any())
            {
                return invalidFiles;
            }

            foreach(string file in files)
            {
                if (!library.Files.ContainsKey(file))
                {
                    invalidFiles.Add(file);
                }
            }

            return invalidFiles;
        }

        /// <summary>
        /// Gets JSON via a get request to the provided URL and converts it to a JObject
        /// </summary>
        /// <param name="webRequestHandler"></param>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<JObject> GetJsonObjectViaGetAsync(this IWebRequestHandler webRequestHandler, string url, CancellationToken cancellationToken)
        {
            _ = webRequestHandler ?? throw new ArgumentNullException(nameof(webRequestHandler));
            JObject result = null;

            using (Stream stream = await webRequestHandler.GetStreamAsync(url, cancellationToken))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonText = await reader.ReadToEndAsync();
                    result = await Task.Factory.StartNew(() => ((JObject)JsonConvert.DeserializeObject(jsonText)),
                                                         cancellationToken,
                                                         TaskCreationOptions.None,
                                                         TaskScheduler.Default);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets string value of JObject given member name. Assumes the value is a string, not another object or array.
        /// </summary>
        /// <param name="jObject">Object containing member with the desired value</param>
        /// <param name="propertyName">Member name for the value to return</param>
        /// <param name="defaultValue">Value to use if the object doesn't contain a property with the specified name</param>
        /// <returns></returns>
        public static string GetJObjectMemberStringValue(this JObject jObject, string propertyName, string defaultValue = "")
        {
            _ = jObject ?? throw new ArgumentNullException(nameof(jObject));
            string propertyValue = defaultValue;

            JValue jValue = jObject[propertyName] as JValue;
            if (jValue != null)
            {
                propertyValue = jValue.Value as string ?? defaultValue;
            }

            return propertyValue;
        }

    }
}
