// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Web.LibraryManager.Vsix.Shared
{
    // EventArgs helper class that can hold various event arg information
    internal class ParamEventArgs : EventArgs
    {
        public object[] Args { get; private set; }

        public ParamEventArgs(params object[] args)
        {
            Args = args;
        }
    }

    internal class DefaultSolutionEvents : IVsSolutionEvents, IDisposable
    {
        public event EventHandler<ParamEventArgs> SolutionClosing;
        public event EventHandler<ParamEventArgs> ProjectUnloading;
        public event EventHandler<ParamEventArgs> ProjectClosing;

        private IVsSolution _solution;
        private uint _solutionEventsCookie;

        public DefaultSolutionEvents()
            : this(Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution)
        {
        }

        public DefaultSolutionEvents(IVsSolution solution)
        {
            _solution = solution;
            _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            ProjectClosing?.Invoke(this, new ParamEventArgs(pHierarchy, fRemoved));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            SolutionClosing?.Invoke(this, new ParamEventArgs(pUnkReserved));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            ProjectUnloading?.Invoke(this, new ParamEventArgs(pRealHierarchy, pStubHierarchy));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            if ((_solution != null) && (_solutionEventsCookie != 0))
            {
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
            }
        }
    }
}
