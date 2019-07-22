using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;

namespace alexbegh.vMerge.Model.Implementation
{
    public class ProfilesProvider : IProfilesProvider
    {
        private SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> _content = new SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>();

        public SerializableDictionary<string, ProfileSettings> GetForUri(String uri)
        {
            if (_content.ContainsKey(uri)) return _content[uri];
            return null;
        }

        public bool TryGetValue(string uri, out SerializableDictionary<string, ProfileSettings> result)
        {
            try
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider.TryGetValue for: " + uri);
                result = GetForUri(uri);
                return result != null;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, false, false);
                result = null;
                return false;
            }
        }

        public void Set(string uri, SerializableDictionary<string, ProfileSettings> result)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider.set for: " + uri);
            _content.Add(uri, result);
        }

        public IEnumerable<ProfileSettings> GetAllProfiles()
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider.GetAllProfiles");
            return _content.Values.SelectMany(k => k.Values);
        }

        public void SaveAsJson(String fileName)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider SaveAsJson.");
            var pathBak = fileName + ".bkp";

            if (_content == null)
                return;

            if (File.Exists(fileName))
                File.Copy(fileName, pathBak, true);

            SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider JsonSerialze.");
            SimpleLogger.Log(SimpleLogLevel.Info, "Serialize to: " + fileName);
            Serializer.JsonSerialize(_content, fileName);

        }

        public void LoadAsJson(String fileName)
        {
            try
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "ProfilesProvider load");

                SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> tempSerializedSettings;
                //Serializer.JSonDeserialize(fileName, out tempSerializedSettings);
                Serializer.JSonDeserialize(fileName, out tempSerializedSettings);

                _content = tempSerializedSettings;
            }
            catch (FileNotFoundException)
            {
                var message = "Failed to load Settings from '" + fileName + "': File not found";
                SimpleLogger.Log(SimpleLogLevel.Warn, message);
            }
            catch (Exception ex)
            {
                var message = "Failed to load Settings from '" + fileName + "': " + ex.Message;
                SimpleLogger.Log(SimpleLogLevel.Error, message);
                throw new Exception(message, ex);
            }
        }
    }
}
