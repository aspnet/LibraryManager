// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    internal class TableDataSource : ITableDataSource
    {
        private static TableDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private static readonly Dictionary<string, TableEntriesSnapshot> _snapshots = new Dictionary<string, TableEntriesSnapshot>();

        [Import]
        public ITableManagerProvider TableManagerProvider { get; set; }

        private TableDataSource()
        {
            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
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
            get { return _instance ?? (_instance = new TableDataSource()); }
        }

        public bool HasErrors
        {
            get { return _snapshots.Count > 0; }
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
                    manager.UpdateSink(_snapshots.Values);
                }
            }
        }

        public void AddErrors(IEnumerable<DisplayError> result, string projectName, string fileName)
        {
            var snapshot = new TableEntriesSnapshot(result, projectName, fileName);
            _snapshots[fileName] = snapshot;

            UpdateAllSinks();
        }

        public void CleanErrors(params string[] urls)
        {
            foreach (string url in urls)
            {
                if (_snapshots.ContainsKey(url))
                {
                    _snapshots[url].Dispose();
                    _snapshots.Remove(url);
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
            foreach (string url in _snapshots.Keys)
            {
                TableEntriesSnapshot snapshot = _snapshots[url];
                snapshot?.Dispose();
            }

            _snapshots.Clear();

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
