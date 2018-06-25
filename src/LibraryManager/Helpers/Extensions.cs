// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// <param name="dependencies"></param>
        /// <returns>
        ///   <c>true</c> if the specified state is valid; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<ILibraryOperationResult> IsValidAsync(this ILibraryInstallationState state, IDependencies dependencies)
        {
            var errors = new List<IError>();

            if (state != null)
            {
                if (string.IsNullOrEmpty(state.ProviderId))
                {
                    errors.Add(PredefinedErrors.ProviderIsUndefined());
                }
                else
                {
                    IProvider provider = dependencies.GetProvider(state.ProviderId);
                    if (provider == null)
                    {
                        errors.Add(PredefinedErrors.ProviderUnknown(state.ProviderId));
                    }
                    else
                    {
                        ILibraryOperationResult result = await IsValidAsync(state, provider);
                        errors.AddRange(result.Errors);
                    }
                }
            }
            else
            {
                errors.Add(PredefinedErrors.ManifestMalformed());
            }

            if (errors.Any())
            {
                return new LibraryOperationResult(errors.ToArray());
            }

            return LibraryOperationResult.FromSuccess(state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static async Task<ILibraryOperationResult> IsValidAsync(this ILibraryInstallationState state, IProvider provider)
        {
            var errors = new List<IError>();

            if (state != null && provider != null)
            {
                if (string.IsNullOrEmpty(state.LibraryId))
                {
                    errors.Add(PredefinedErrors.LibraryIdIsUndefined());
                }
                else
                {
                    ILibraryCatalog catalog = provider.GetCatalog();
                    try
                    {
                        await catalog.GetLibraryAsync(state.LibraryId, CancellationToken.None);
                    }
                    catch
                    {
                        errors.Add(PredefinedErrors.UnableToResolveSource(state.LibraryId, provider.Id));
                    }
                }

                if (string.IsNullOrEmpty(state.DestinationPath))
                {
                    errors.Add(PredefinedErrors.PathIsUndefined());
                }
                else if (state.DestinationPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    errors.Add(PredefinedErrors.DestinationPathHasInvalidCharacters(state.DestinationPath));
                }
            }

            if (errors.Any())
            {
                return new LibraryOperationResult(errors.ToArray());
            }

            return LibraryOperationResult.FromSuccess(state);
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
            JObject result = null;

            using (Stream stream = await WebRequestHandler.Instance.GetStreamAsync(url, cancellationToken))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonText = await reader.ReadToEndAsync();
                    result = await Task.Factory.StartNew(() => ((JObject)JsonConvert.DeserializeObject(jsonText)), cancellationToken);
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
