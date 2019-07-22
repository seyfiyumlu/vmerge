using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using alexbegh.vMerge.Model.Implementation;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model;

namespace vMergeUnitTests2
{
    [TestClass]
    public class ProfilesProviderTests
    {
        private string _file;
        private string _profileName = "myProfileName";
        private string _testUri = "http://www.haufe.de/";

        [TestInitialize]
        public void Setup()
        {
            _file = Path.GetTempFileName();
            Repository.Initialize(new TfsConnectionInfoProviderMock(), new TfsUIInteractionProviderMock(), new VMergeUIProviderMock());
        }

        [TestCleanup]
        public void TearDown()
        {
            File.Delete(_file);
        }

        [TestMethod]
        public void TestProfilesProvider()
        {
            var provider = new ProfilesProvider();

            var profile = new SerializableDictionary<string, ProfileSettings>();
            profile.Add("ABC", new ProfileSettings()
            {
                Name = "myName",
                TeamProject = "myProject"
            });
            provider.Set("dummyUrl", profile);

            provider.SaveAsJson(_file);

            var loadedProfiler = new ProfilesProvider();
            loadedProfiler.LoadAsJson(_file);

            CollectionAssert.AreEqual(provider.GetAllProfiles().ToList(), loadedProfiler.GetAllProfiles().ToList());
        }

        // 1. Save Implementieren
        // 2. UnitTest TestProfilesProvider sollte grün sein
        // 3. Settings Klasse Load & Save zusätzliches file was von ProfilesProvider load & Save benutzt wird
        
    }
}
