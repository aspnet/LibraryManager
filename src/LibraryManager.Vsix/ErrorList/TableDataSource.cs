// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.Web.LibraryManager.Vsix.ErrorList
{
    internal class TableDataSource : ITableDataSource
    {
        private static TableDataSource CachedInstance;
        private static readonly Dictionary<string, TableEntriesSnapshot> Snapshots = new Dictionary<string, TableEntriesSnapshot>();
        private readonly List<SinkManager> _managers = new List<SinkManager>();

        [Import]
        public ITableManagerProvider TableManagerProvider { get; set; }

        private TableDataSource()
        {
            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(compositionService);
            compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

            ITableManager manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            manager.AddSource(this, /*StandardTableColumnDefinitions.DetailsExpander,*/ StandardTableColumnDefinitions.ErrorCategory,
                                    StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                    StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                    StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                    StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);
        }

        public static TableDataSource Instance
        {
            get { return CachedInstance ?? (CachedInstance = new TableDataSource()); }
        }

        public bool HasErrors
        {
            get { return Snapshots.Count > 0; }
        }

        #region ITableDataSource members
        public string SourceTypeIdentifier
        {
            get { return StandardTableDataSources.ErrorTableDataSource; }
        }

        public string Identifier
        {
            get { return PackageGuids.guidPackageString; }
        }

        public string DisplayName
        {
            get { return Vsix.Name; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            lock (_managers)
            {
                foreach (SinkManager manager in _managers)
                {
                    manager.UpdateSink(Snapshots.Values);
                }
            }
        }

        public void AddErrors(IEnumerable<DisplayError> result, string projectName, string fileName)
        {
            var snapshot = new TableEntriesSnapshot(result, projectName, fileName);
            Snapshots[fileName] = snapshot;

            UpdateAllSinks();
        }

        public void CleanErrors(params string[] urls)
        {
            foreach (string url in urls)
            {
                if (Snapshots.ContainsKey(url))
                {
                    Snapshots[url].Dispose();
                    Snapshots.Remove(url);
                }
            }

            lock (_managers)
            {
                foreach (SinkManager manager in _managers)
                {
                    manager.RemoveSnapshots(urls);
                }
            }

            UpdateAllSinks();
        }

        public void CleanAllErrors()
        {
            foreach (string url in Snapshots.Keys)
            {
                TableEntriesSnapshot snapshot = Snapshots[url];
                snapshot?.Dispose();
            }

            Snapshots.Clear();

            lock (_managers)
            {
                foreach (SinkManager manager in _managers)
                {
                    manager.Clear();
                }
            }

            UpdateAllSinks();
        }
    }
}
