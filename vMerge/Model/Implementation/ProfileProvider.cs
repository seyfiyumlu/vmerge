using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;

namespace alexbegh.vMerge.Model.Implementation
{
    public class ProfileProvider : IProfileProvider
    {
        private IProfileSettings _activeProfile;

        public ProfileProvider()
        {
            Repository.Instance.TfsBridgeProvider.AfterCompleteBranchListLoaded +=
                (o, a) =>
                {
                    var defaultProfile = GetDefaultProfile();
                    if (DefaultProfileChanged != null && defaultProfile != null)
                        DefaultProfileChanged(this, new DefaultProfileChangedEventArgs(defaultProfile));
                };
        }

        private IProfilesProvider _profiles;
        private IProfilesProvider Profiles
        {
            get
            {
                if (_profiles == null)
                {
                    _profiles = Repository.Instance.Settings.ProfilesSettings;                    
                }

                return _profiles;
            }
        }

        public void ReloadFromSettings()
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ProfileProvider.ReloadFromSettings ");
            _profiles = Repository.Instance.Settings.ProfilesSettings;            
            _activeProfile = null;
        }

        public void SetProfileDirty(IProfileSettings profileSettings)
        {
            if (profileSettings != null && profileSettings.Name == "__Default" && DefaultProfileChanged != null)
                DefaultProfileChanged(this, new DefaultProfileChangedEventArgs(profileSettings));
            if (profileSettings != null && profileSettings.Name == "__Default" && ProfilesChanged != null)
                ProfilesChanged(this, new DefaultProfileChangedEventArgs(profileSettings));

            Repository.Instance.Settings.SetSettings(Constants.Settings.ProfileKey, Profiles);
        }

        public IProfileSettings GetDefaultProfile(Uri teamProjectUri = null)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ProfileProvider.GetDefaultProfile");
            if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
                return null;

            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles.Set(teamProjectUri.ToString(), result);
            }
            ProfileSettings settings = null;
            if (result.TryGetValue("__Default", out settings) == false)
            {
                var teamProjectName = GetTeamProjectName(teamProjectUri);
                settings = new ProfileSettings(teamProjectUri.ToString(), teamProjectName, "__Default", SetProfileDirty);
                result["__Default"] = settings;
            }
            return settings;
        }

        private String GetTeamProjectName(Uri teamProjectUri)
        {
            if (teamProjectUri.ToString().Equals("http://www.haufe.de/"))
            {
                return "test_unittestProjectName";
            }
            return Repository.Instance.TfsBridgeProvider.VersionControlServer.GetAllTeamProjects(false).Where(tp => tp.ArtifactUri.Equals(teamProjectUri)).FirstOrDefault().Name;
        }

        public IEnumerable<IProfileSettings> GetAllProfilesForProject(Uri teamProjectUri = null)
        {
            if ((teamProjectUri != null && !teamProjectUri.ToString().Equals("http://www.haufe.de/"))
                && Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
            {
                return Enumerable.Empty<IProfileSettings>();
            }

            if (teamProjectUri == null)
            {
                try
                {
                    teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;
                } catch
                {
                    SimpleLogger.Log(SimpleLogLevel.Warn, "GetAllProfilesForProject cannot load current ArtifactUri - maybe not connected to TFS");
                    return Enumerable.Empty<IProfileSettings>();
                }
            }

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                return Enumerable.Empty<IProfileSettings>();
            }

            return result.Values.Where(value => value.Name != "__Default");
        }

        public IEnumerable<IProfileSettings> GetAllProfiles()
        {
            return Profiles.GetAllProfiles().Where(item => item.Name != "__Default");
        }

        public bool SaveProfileAs(Uri teamProjectUri, string profileName, bool overwrite)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "Save Profile: " + profileName);
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "Create new Profile SerializableDictionary");
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles.Set(teamProjectUri.ToString(), result);
            }

            if (result.ContainsKey(profileName) && !overwrite)
                return false;

            var teamProjectName = GetTeamProjectName(teamProjectUri);//Repository.Instance.TfsBridgeProvider.VersionControlServer.GetAllTeamProjects(false).Where(tp => tp.ArtifactUri.Equals(teamProjectUri)).FirstOrDefault();
            var settings = new ProfileSettings(teamProjectUri.ToString(), teamProjectName, profileName, SetProfileDirty);

            var defaultProfile = GetDefaultProfile(teamProjectUri);
            if (defaultProfile != null) (defaultProfile as ProfileSettings).CopyTo(settings);
            result[profileName] = settings;

            if (ActiveProjectProfileListChanged != null &&
                !teamProjectUri.ToString().Equals("http://www.haufe.de/")
                && teamProjectUri == Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri)
            {
                ActiveProjectProfileListChanged(this, EventArgs.Empty);
            }
            if (ProfilesChanged != null)
                ProfilesChanged(this, EventArgs.Empty);
            _activeProfile = result[profileName];
            SimpleLogger.Log(SimpleLogLevel.Info, "Save Profile finished: " + profileName);
            return true;
        }

        public bool DeleteProfile(IProfileSettings profile)
        {
            return DeleteProfile(new Uri(profile.TeamProject), profile.Name);
        }

        public bool DeleteProfile(Uri teamProjectUri, string profileName)
        {
            if (_activeProfile != null && _activeProfile.TeamProject == teamProjectUri.ToString() && _activeProfile.Name == profileName)
            {
                _activeProfile = null;
            }
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles.Set(teamProjectUri.ToString(), result);
            }

            if (!result.ContainsKey(profileName))
                return false;

            result.Remove(profileName);

            if (ActiveProjectProfileListChanged != null && teamProjectUri == Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri)
                ActiveProjectProfileListChanged(this, EventArgs.Empty);
            if (ProfilesChanged != null)
                ProfilesChanged(this, EventArgs.Empty);

            SetProfileDirty(null);
            return true;
        }

        public bool LoadProfile(Uri teamProjectUri, string profileName)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "LoadProfile: " + profileName);
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles.Set(teamProjectUri.ToString(), result);
            }

            if (!result.ContainsKey(profileName))
                return false;

            result[profileName].CopyTo(GetDefaultProfile(teamProjectUri));
            SetProfileDirty(GetDefaultProfile(teamProjectUri));
            _activeProfile = result[profileName];
            return true;
        }

        public bool GetActiveProfile(out IProfileSettings mostRecentSettings, out bool alreadyModified)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "GetActiveProfile ");
            var defaultProfile = GetDefaultProfile();
            mostRecentSettings = _activeProfile;
            alreadyModified = false;
            if (defaultProfile == null)
                return false;

            if (mostRecentSettings == null)
            {
                alreadyModified = true;
            }
            else
            {
                alreadyModified = !mostRecentSettings.Equals(defaultProfile);
            }
            return true;
        }

        public event EventHandler<DefaultProfileChangedEventArgs> DefaultProfileChanged;

        public event EventHandler ProfilesChanged;

        public event EventHandler ActiveProjectProfileListChanged;

    }
}
