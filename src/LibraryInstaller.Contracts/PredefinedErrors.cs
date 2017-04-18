// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Web.LibraryInstaller.Contracts
{
    /// <summary>
    /// A list of predefined errors any <see cref="IProvider"/> can use.
    /// </summary>
    /// <remarks>
    /// It is recommended to use these errors where applicable to create a
    /// uniform experience across providers.
    /// </remarks>
    public static class PredefinedErrors
    {
        /// <summary>
        /// Represents an unhandled exception that occured in the provider.
        /// </summary>
        /// <remarks>
        /// An <see cref="IProvider.InstallAsync"/> should never throw and this error
        /// should be used as when catching generic exeptions.
        /// </remarks>
        /// <returns>The error code LIB000</returns>
        public static IError UnknownException()
            => new Error("LIB000", Text.ErrorUnknownException);

        /// <summary>
        /// The specified provider is unknown to the host.
        /// </summary>
        /// <param name="providerId">The unique ID of the <see cref="IProvider"/>.</param>
        /// <returns>The error code LIB001</returns>
        public static IError ProviderUnknown(string providerId)
            => new Error("LIB001", string.Format(Text.ErrorProviderUnknown, providerId));

        /// <summary>
        /// The <see cref="IProvider"/> is unable to resolve the source.
        /// </summary>
        /// <param name="libraryId">The ID of the library that could not be resolved.</param>
        /// <param name="providerId">The ID of the <see cref="IProvider"/> that could not resolve the resource.</param>
        /// <returns>The error code LIB002</returns>
        public static IError UnableToResolveSource(string libraryId, string providerId)
            => new Error("LIB002", string.Format(Text.ErrorUnableToResolveSource, libraryId, providerId));

        /// <summary>
        /// The <see cref="IProvider"/> failed to write a file in the <see cref="ILibraryInstallationState.Files"/> array.
        /// </summary>
        /// <param name="file">The file name that failed to be written to disk.</param>
        /// <returns>The error code LIB003</returns>
        public static IError CouldNotWriteFile(string file)
            => new Error("LIB003", string.Format(Text.ErrorCouldNotWriteFile, file));

        /// <summary>
        /// The manifest JSON file is malformed.
        /// </summary>
        /// <returns>The error code LIB004</returns>
        public static IError ManifestMalformed()
           => new Error("LIB004", string.Format(Text.ErrorManifestMalformed));

        /// <summary>
        /// The relative path is undefined.
        /// </summary>
        /// <returns>The error code LIB005</returns>
        public static IError PathIsUndefined()
           => new Error("LIB005", string.Format(Text.ErrorPathIsUndefined));

        /// <summary>
        /// The library id is undefined.
        /// </summary>
        /// <returns>The error code LIB006</returns>
        public static IError LibraryIdIsUndefined()
           => new Error("LIB006", string.Format(Text.ErrorLibraryIdIsUndefined));

        /// <summary>
        /// The provider is undefined.
        /// </summary>
        /// <returns>The error code LIB007</returns>
        public static IError ProviderIsUndefined()
           => new Error("LIB007", string.Format(Text.ErrorProviderIsUndefined));
    }
}
