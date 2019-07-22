using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using alexbegh.vMerge.Model.Interfaces;
using DevExpress.Xpo;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;

namespace vMergeUnitTests2
{

    public class TfsConnectionInfoProviderMock : ITfsConnectionInfoProvider
    {
        public Uri Uri { get { return null; } }

        public Microsoft.TeamFoundation.VersionControl.Client.TeamProject Project { get { return null; } }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TfsUIInteractionProviderMock : ITfsUIInteractionProvider
    {
        public string BrowseForTfsFolder(string startFrom)
        {
            return null;
        }

        public bool ResolveConflictsInternally(ITfsTemporaryWorkspace workspace)
        {
            return true;
        }

        public void ResolveConflictsPerTF(string rootPath)
        {

        }

        public void ShowChangeset(int id)
        {

        }

        public bool ShowDifferencesPerTF(string rootPath, string sourcePath, string targetPath)
        {
            return true;
        }

        public void ShowWorkItem(int id)
        {

        }

        public void TrackChangeset(int id)
        {

        }

        public void TrackWorkItem(int id)
        {

        }
    }

    public class VMergeUIProviderMock : IVMergeUIProvider
    {
        public event EventHandler MergeWindowVisibilityChanged;

        public void FocusChangesetWindow()
        {

        }

        public void FocusMergeWindow()
        {

        }

        public void FocusWorkItemWindow()
        {

        }

        public bool IsMergeWindowVisible()
        {
            return false;
        }
    }

}