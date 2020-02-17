// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.LibraryManager.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Helpers
{
    internal class LibraryStateTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<List<LibraryInstallationState>>(reader).Cast<ILibraryInstallationState>().ToList();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IEnumerable<LibraryInstallationState> list = (value as IEnumerable<ILibraryInstallationState>)
                                                         .Select(i => LibraryInstallationState.FromInterface(i));

            serializer.Serialize(writer, list);
        }
    }
}
