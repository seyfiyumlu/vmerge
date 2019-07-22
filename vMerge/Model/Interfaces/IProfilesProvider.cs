using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface IProfilesProvider
    {
        SerializableDictionary<string, ProfileSettings> GetForUri(String uri);
        bool TryGetValue(string uri, out SerializableDictionary<string, ProfileSettings> result);
        void Set(string v, SerializableDictionary<string, ProfileSettings> result);
        IEnumerable<ProfileSettings> GetAllProfiles();

        void SaveAsJson(String fileName);

        void LoadAsJson(String fileName);
    }
}
