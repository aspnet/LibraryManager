// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager.Vsix.ErrorList
{
    internal class SinkManager : IDisposable
    {
        private readonly ITableDataSink _sink;
        private readonly TableDataSource _errorList;
        private readonly List<TableEntriesSnapshot> _snapshots = new List<TableEntriesSnapshot>();

        internal SinkManager(TableDataSource errorList, ITableDataSink sink)
        {
            _sink = sink;
            _errorList = errorList;

            errorList.AddSinkManager(this);
        }

        internal void Clear()
        {
            _sink.RemoveAllSnapshots();
        }

        internal void UpdateSink(IEnumerable<TableEntriesSnapshot> snapshots)
        {
            foreach (TableEntriesSnapshot snapshot in snapshots)
            {
                TableEntriesSnapshot existing = _snapshots.Find(s => s.Url == snapshot.Url);

                if (existing != null)
                {
                    _snapshots.Remove(existing);
                    _sink.ReplaceSnapshot(existing, snapshot);
                }
                else
                {
                    _sink.AddSnapshot(snapshot);
                }

                _snapshots.Add(snapshot);
            }
        }

        internal void RemoveSnapshots(IEnumerable<string> urls)
        {
            foreach (string url in urls)
            {
                TableEntriesSnapshot existing = _snapshots.Find(s => s.Url == url);

                if (existing != null)
                {
                    _snapshots.Remove(existing);
                    _sink.RemoveSnapshot(existing);
                }
            }
        }

        public void Dispose()
        {
            // Called when the person who subscribed to the data source disposes of the cookie (== this object) they were given.
            _errorList.RemoveSinkManager(this);
        }
    }
}
