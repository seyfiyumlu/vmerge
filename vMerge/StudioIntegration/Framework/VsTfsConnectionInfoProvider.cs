using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using System;
using alexbegh.Utility.Helpers.Logging;
using System.Diagnostics;

namespace alexbegh.vMerge.StudioIntegration.Framework
{
    class VsTfsConnectionInfoProvider : ITfsConnectionInfoProvider
    {
        #region Constructor
        internal VsTfsConnectionInfoProvider()
        {
            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var obj = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");
                if (obj == null)
                {
                    SimpleLogger.Log(SimpleLogLevel.Error,
                        "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt is null");
                }
                else
                {
                    TeamFoundationServerExt tfse =
                        obj as TeamFoundationServerExt;
                    if (tfse != null)
                    {
                        tfse.ProjectContextChanged += VsTfsProjectContextChanged;
                    }
                    else
                    {
                        SimpleLogger.Log(SimpleLogLevel.Error,
                            "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt is " +
                            obj.GetType().Namespace + "." + obj.GetType().Name + " from " +
                            obj.GetType().AssemblyQualifiedName);
                        try
                        {
                            ((TeamFoundationServerExt)obj)
                                .ProjectContextChanged += VsTfsProjectContextChanged;
                        }
                        catch (Exception ex)
                        {
                            SimpleLogger.Log(ex, false, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
        }
        #endregion

        #region Private Operations
        private void VsTfsProjectContextChanged(object sender, EventArgs e)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "Project context changed");
            _loadFailed = false;
            _projectNullReloadDone = false;
            _uriNullReloadDone = false;
            _reloadCounter = 0;
            
            Repository.Instance.TfsBridgeProvider.Clear();
            ReloadProjectContextProperties();
        }

        private void ReloadProjectContextProperties()
        {
            _reloadCounter++;
            SimpleLogger.Log(SimpleLogLevel.Info, "ReloadProjectContextProperties ["+ _reloadCounter+"]");
            if (_loadFailed || _reloadCounter >= 10)
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "ReloadProjectContextProperties canceled\r\n"+ (new StackTrace(true)).ToString());
                return;
            }
            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                Object obj = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");
                if (obj == null) SimpleLogger.Log(SimpleLogLevel.Error, "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt not found");
                Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt tfse = obj as TeamFoundationServerExt;

                if (tfse != null)
                {
                    if (tfse.ActiveProjectContext != null
                            && tfse.ActiveProjectContext.DomainUri != null
                            && !String.IsNullOrEmpty(tfse.ActiveProjectContext.ProjectName))
                    {
                        LoadUriAndProject(tfse);
                    }
                    else
                    {
                        WriteUnableToLoadLogEntry(tfse);
                    }
                }
                else
                {
                    SimpleLogger.Log(SimpleLogLevel.Error, "Unable to load Project Context Properties");
                    if (obj != null) SimpleLogger.Log(SimpleLogLevel.Info, "was: " + obj.GetType().Namespace + "." + obj.GetType().Name);
                    _uri = null;
                    RaisePropertyChanged("Uri");
                    _project = null;
                    RaisePropertyChanged("Project");
                }
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                SimpleLogger.Log(ex, false);
                _uri = null;
                RaisePropertyChanged("Uri");
                _project = null;
                RaisePropertyChanged("Project");
            }
        }

        private void LoadUriAndProject(TeamFoundationServerExt tfse)
        {
            var newUri = new Uri(tfse.ActiveProjectContext.DomainUri);

            SetUri(newUri);
            if (Repository.Instance.TfsBridgeProvider != null &&
                Repository.Instance.TfsBridgeProvider.VersionControlServer != null)
            {
                var newProject = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetTeamProject(tfse.ActiveProjectContext.ProjectName);
                SetProject(newProject);
                Repository.Instance.TfsBridgeProvider.ActiveTeamProject = newProject;
            }
            else
            {
                WriteUnableToLoadLogEntry(tfse);
            }
        }

        private static void WriteUnableToLoadLogEntry(TeamFoundationServerExt tfse)
        {
            var message = "Unable to load Project Context Properties";
            if (tfse.ActiveProjectContext != null && tfse.ActiveProjectContext.ProjectName != null)
            {
                message += " on Project '" + tfse.ActiveProjectContext.ProjectName + "'.";
            }
            SimpleLogger.Log(SimpleLogLevel.Error, message);
            if (tfse.ActiveProjectContext == null) SimpleLogger.Log(SimpleLogLevel.Error, "ActiveProjectContext null");
            if (tfse.ActiveProjectContext != null && tfse.ActiveProjectContext.DomainUri == null) SimpleLogger.Log(SimpleLogLevel.Error, "ActiveProjectContext.DomainUri null");
            if (tfse.ActiveProjectContext != null && tfse.ActiveProjectContext.ProjectName == null) SimpleLogger.Log(SimpleLogLevel.Error, "ProjectName null");
            if (Repository.Instance.TfsBridgeProvider == null) SimpleLogger.Log(SimpleLogLevel.Error, "TfsBridgeProvider null");
            if (Repository.Instance.TfsBridgeProvider != null &&
                Repository.Instance.TfsBridgeProvider.VersionControlServer == null) SimpleLogger.Log(SimpleLogLevel.Error, "TfsBridgeProvider.VersionControlServer null");
        }

        private void SetUri(Uri newUri)
        {


            _uri = newUri;
            SimpleLogger.Log(SimpleLogLevel.Info, String.Format("Set active URI: {0}", _uri == null ? "<null>" : _uri.ToString()));
            if (IsNewUri(newUri)) RaisePropertyChanged("Uri");            
        }

        private void SetProject(TeamProject newProject)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, String.Format("Set active Project: {0}", newProject == null ? "<null>" : newProject.Name));
            _project = newProject;
            if (IsNewProject(newProject)) RaisePropertyChanged("Project");
        }

        private bool IsNewUri(Uri newUri){
            if (newUri == null && _uri == null) return false;
            if (newUri != null && _uri == null) return false;
            if (newUri == null && _uri != null) return false;
            return !newUri.Equals(_uri);
        }
        private bool IsNewProject(TeamProject newProject)
        {
            if (newProject == null && _project == null) return false;
            if (newProject != null && _project == null) return false;
            if (newProject == null && _project != null) return false;
            return !newProject.Equals(_project);
        }
        #endregion

        #region ITfsConnectionInfoProvider Exposed Properties
        private Uri _uri;
        public Uri Uri
        {
            get {
                if (_uri == null &&!_uriNullReloadDone)
                {
                    _uriNullReloadDone = true;
                    ReloadProjectContextProperties();
                    
                }
                return _uri;
            }
            set { if (_uri != value) { SimpleLogger.Log(SimpleLogLevel.Info, "Set active URI: {0}", value == null ? "<null>" : value.ToString()); _uri = value; RaisePropertyChanged("Uri"); } }
        }

        private TeamProject _project;
        private bool _loadFailed;
        private bool _uriNullReloadDone;
        private bool _projectNullReloadDone;
        private int _reloadCounter = 0;

        public TeamProject Project
        {
            get {
                if (_project == null && !_projectNullReloadDone)
                {
                    _projectNullReloadDone = true;
                    ReloadProjectContextProperties();
                    
                }
                return _project;
            }
            set { if (_project != value) { SimpleLogger.Log(SimpleLogLevel.Info, "Active project: {0}", value == null ? "<null>" : value.Name); _project = value; RaisePropertyChanged("Project"); } }
        }
        #endregion

        #region INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
