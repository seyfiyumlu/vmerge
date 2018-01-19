using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using System;
using alexbegh.Utility.Helpers.Logging;

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
                    Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt tfse =
                        obj as Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt;
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
                            ((Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt) obj)
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
            ReloadProjectContextProperties();
            Repository.Instance.TfsBridgeProvider.Clear();
        }

        private void ReloadProjectContextProperties()
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ReloadProjectContextProperties");
            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                TeamFoundationServerExt tfse = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt") as TeamFoundationServerExt;

                if (tfse != null &&
                    tfse.ActiveProjectContext != null
                    && tfse.ActiveProjectContext.DomainUri!=null
                    && !String.IsNullOrEmpty(tfse.ActiveProjectContext.ProjectName))
                {
                    Uri = new Uri(tfse.ActiveProjectContext.DomainUri);
                    Project = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetTeamProject(tfse.ActiveProjectContext.ProjectName);
                }
                else
                {
                    SimpleLogger.Log(SimpleLogLevel.Error, "Unable to load Project Context Properties");
                    Uri = null;
                    Project = null;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
                Uri = null;
                Project = null;
            }
        }
        #endregion

        #region ITfsConnectionInfoProvider Exposed Properties
        private Uri _uri;
        public Uri Uri
        {
            get { if (_uri == null) ReloadProjectContextProperties(); return _uri; }
            set { if (_uri != value) { SimpleLogger.Log(SimpleLogLevel.Info, "Active URI: {0}", value == null ? "<null>" : value.ToString()); _uri = value; RaisePropertyChanged("Uri"); } }
        }

        private TeamProject _project;
        public TeamProject Project
        {
            get { if (_project == null) ReloadProjectContextProperties(); return _project; }
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
