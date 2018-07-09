// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class Telemetry
    {
        private const string _namespace = Constants.TelemetryNamespace;

        public static void TrackUserTask(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");

            var task = new UserTaskEvent(_namespace + actualName, result);

            foreach (KeyValuePair<string, object> property in properties)
            {
                task.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(task);
        }

        public static void TrackOperation(string name, TelemetryResult result = TelemetryResult.None, params KeyValuePair<string, object>[] properties)
        {
            string actualName = name.Replace(" ", "_");
            var task = new OperationEvent(_namespace + actualName, result);

            foreach (KeyValuePair<string, object> property in properties)
            {
                task.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(task);
        }

        public static void TrackException(string name, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(name) || exception == null)
                return;

            string actualName = name.Replace(" ", "_");
            TelemetryService.DefaultSession.PostFault(_namespace + actualName, exception.Message, exception);
        }

        internal static void LogEventsSummary(IEnumerable<ILibraryOperationResult> results, OperationType operation, TimeSpan elapsedTime)
        {
            Dictionary<string, object> telResult = new Dictionary<string, object>();
            double elapsedTimeRounded = Math.Round(elapsedTime.TotalSeconds, 2);
            string elapsedTimeStr = elapsedTimeRounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
            List<string> generalErrorCodes = GetErrorCodes(results.Where(r => r.InstallationState == null && r.Errors.Any()));
            IEnumerable<string> providers = results.Select(r => r.InstallationState?.ProviderId.ToLower()).Distinct();

            telResult.Add("LibrariesCount", results.Count());
            telResult.Add($"{operation}_time", elapsedTimeStr);

            if (generalErrorCodes.Count() > 0)
            {
                telResult.Add("ErrorCodes", string.Join(":", generalErrorCodes));
            }

            foreach (string provider in providers)
            {
                List<ILibraryOperationResult> successfulProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> failedProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> cancelledProviderResults = new List<ILibraryOperationResult>();
                List<ILibraryOperationResult> uptodateProviderResults = new List<ILibraryOperationResult>();

                foreach (ILibraryOperationResult result in results)
                {
                    if (result.InstallationState != null &&
                        result.InstallationState.ProviderId.Equals(provider, StringComparison.OrdinalIgnoreCase))
                    {
                        if (result.Success && !result.UpToDate)
                        {
                            successfulProviderResults.Add(result);
                        }

                        if (result.Errors.Any())
                        {
                            failedProviderResults.Add(result);
                        }

                        if (result.UpToDate)
                        {
                            uptodateProviderResults.Add(result);
                        }

                        if (result.Cancelled)
                        {
                            cancelledProviderResults.Add(result);
                        }
                    }
                }

                if (successfulProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Success"] = successfulProviderResults.Count;
                    telResult[$"LibrariesIDs_{provider}_Success"] = string.Join(":", GetHashedLibrariesNames(successfulProviderResults);
                    telResult[$"{provider}_LibrariesFilesCount"] = GetProviderLibraryFilesCount(successfulProviderResults);
                }

                if (failedProviderResults.Count > 0)
                {
                    List<string> providerErrorCodes = GetErrorCodes(failedProviderResults);

                    telResult[$"LibrariesCount_{provider}_Failure"] = failedProviderResults.Count;
                    telResult[$"LibrariesIDs_{provider}_Failure"] = string.Join(":", GetHashedLibrariesNames(failedProviderResults));
                    telResult.Add($"ErrorCodes_{provider}", string.Join(":", providerErrorCodes));
                }

                if (cancelledProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Cancelled"] = cancelledProviderResults.Count;
                    telResult[$"LibrariesIDs_{provider}_Cancelled"] = string.Join(":", GetHashedLibrariesNames(cancelledProviderResults));
                }

                if (uptodateProviderResults.Count > 0)
                {
                    telResult[$"LibrariesCount_{provider}_Uptodate"] = uptodateProviderResults.Count;
                    telResult[$"LibrariesIDs_{provider}_Uptodate"] = string.Join(":", GetHashedLibrariesNames(uptodateProviderResults));
                }
            }

            TrackUserTask($@"{operation}_Operation", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }

        private static int GetProviderLibraryFilesCount(IEnumerable<ILibraryOperationResult> successfulProviderResults)
        {
            int count = 0;

            foreach (ILibraryOperationResult result in successfulProviderResults)
            {
                if (result.InstallationState != null && result.InstallationState.Files != null)
                {
                    count += result.InstallationState.Files.Count();
                }
            }

            return count;
        }

        internal static void LogErrors(string eventName, IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorCodes = GetErrorCodes(results.Where(r => r.Errors.Any()));
            TrackUserTask(eventName, TelemetryResult.Failure, new KeyValuePair<string, object>("Errorcode", string.Join(":", errorCodes)));
        }

        private static List<string> GetErrorCodes(IEnumerable<ILibraryOperationResult> results)
        {
            List<string> errorCodes = new List<string>();

            foreach (ILibraryOperationResult result in results)
            {
                errorCodes.AddRange(result.Errors.Select(e => e.Code));
            }

            return errorCodes;
        }

        private static List<string> GetHashedLibrariesNames(IEnumerable<ILibraryOperationResult> results)
        {
            List<string> hashedLibraryNames= new List<string>();

            using (MD5 md5Hash = MD5.Create())
            {
                foreach (ILibraryOperationResult result in results)
                {
                    if (result.InstallationState != null)
                    {
                        hashedLibraryNames.Add(GetMd5Hash(md5Hash, result.InstallationState.Name));
                    }
                }
            }

            return hashedLibraryNames;
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
