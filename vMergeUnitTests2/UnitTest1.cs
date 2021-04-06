using alexbegh.Utility.SerializationHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using alexbegh.Utility;
using alexbegh.vMerge.Model.Implementation;
using alexbegh.vMerge.Model;
using vMergeUnitTests2;
using System;
using System.Linq;

namespace vMergeUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private string _testName = "UnitTests";
        private string _file;
        private string _profileName = "myProfileName";
        private string _testUri = "http://www.haufe.de/";

        [TestInitialize]
        public void Setup()
        {
            _file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge" + _testName + ".qvmset");
            Repository.Initialize(new TfsConnectionInfoProviderMock(), new TfsUIInteractionProviderMock(), new VMergeUIProviderMock());
        }

        [TestCleanup]
        public void TearDown()
        {
            File.Delete(_file);
        }


        //[TestMethod]
        //public void TestMethod1()
        //{
        //    var test = "abc";
        //    Serializer.JsonSerialize(test, _file);

        //    Assert.IsTrue(File.Exists(_file), "Test " + _file + " exists after serialize");

        //    var testRead = string.Empty;
        //    Serializer.JSonDeserialize(_file, out testRead);

        //    Assert.AreEqual(test, testRead, "Compare after serialize/deserialize");
        //}

        //[TestMethod]
        //public void TestMethod1()
        //{
        //    var test = new Settings();
        //    var testDict = new SerializableDictionary<string, object>();
        //    test.SerializedSettings = testDict;

        //    Serializer.JsonSerialize(testDict, _file);

        //    Assert.IsTrue(File.Exists(_file), "Test " + _file + " exists after serialize");

        //    var testRead = string.Empty;
        //    Serializer.JSonDeserialize(_file, out testRead);

        //    Assert.AreEqual(test, testRead, "Compare after serialize/deserialize");
        //}


        

        [TestMethod]
        public void TestSaveProfile()
        {           
            var test = Repository.Instance.Settings;            

            Assert.IsTrue(Repository.Instance.ProfileProvider.SaveProfileAs(new System.Uri(_testUri), _profileName, false));
            AssertProfileIsInSettings(_profileName, _testUri);

            //Serialisierung
            test.SaveSettings(_testName);

            Assert.IsTrue(File.ReadAllText(_file).Contains(_profileName), "Saved Profile not found in json file");

            //Reload
            test.LoadSettings(_testName);
            AssertProfileIsInSettings(_profileName, _testUri);
        }

        [TestMethod]
        public void TestSaveProfileWhileProfilesInDict()
        {
            
            
            var test = Repository.Instance.Settings;
            var testDict = new SerializableDictionary<string, object>();
            var profiles = new SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>();

            InitTest(test, testDict, profiles);

            AddProfileAndTestObject(_profileName, _testUri, testDict, profiles);
            AssertProfileIsInSettings(_profileName, _testUri);


            //Serialisierung
            SaveAndTestFile(_profileName, test);

            //Reload
            test.LoadSettings(_testName);

            AssertProfileIsInSettings(_profileName, _testUri);
            //TestReloadedDictonaries(_profileName, _testUri, test, out testDict, out profiles);
        }

        private static void TestReloadedDictonaries(string profileName, string testUri, alexbegh.vMerge.Model.Interfaces.ISettings test, out SerializableDictionary<string, object> testDict, out SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> profiles)
        {
            testDict = ((Settings)test).SerializedSettings[alexbegh.vMerge.Constants.Settings.ProfileKey] as SerializableDictionary<string, object>;
            Assert.IsNotNull(testDict, "testDict wrong type");
            Assert.IsTrue(testDict.ContainsKey(alexbegh.vMerge.Constants.Settings.ProfileKey), "Profiles not in Settings dict");

            profiles = testDict[alexbegh.vMerge.Constants.Settings.ProfileKey] as SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>;
            Assert.IsNotNull(profiles, "profiles wrong type");
            Assert.IsTrue(profiles.ContainsKey(testUri), "Uri " + testUri + " not in profiles");
            Assert.IsTrue(profiles[testUri].ContainsKey(profileName), "Profile " + profileName + " not in profiles");
        }

        private void SaveAndTestFile(string profileName, alexbegh.vMerge.Model.Interfaces.ISettings test)
        {
            test.SaveSettings(_testName);

            Assert.IsTrue(File.Exists(_file), "File " + _file + " not found");
            Assert.IsTrue(File.ReadAllText(_file).Contains(profileName), "Saved Profile not found in json file");
        }

        private static void AssertProfileIsInSettings(string profileName, string testUri)
        {
            Repository.Instance.ProfileProvider.ReloadFromSettings();
            var loadProfiles = Repository.Instance.ProfileProvider.GetAllProfilesForProject(new System.Uri(testUri));
            Assert.IsTrue(loadProfiles.Any(), "Profiles are empty");
            Assert.IsTrue(loadProfiles.Any(p => p.Name.Equals(profileName)), "Profiles are empty");
        }

        private static void AddProfileAndTestObject(string profileName, string testUri, SerializableDictionary<string, object> testDict, SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> profiles)
        {
            Assert.IsTrue(Repository.Instance.ProfileProvider.SaveProfileAs(new System.Uri(testUri), profileName, false));
            /*Assert.IsTrue(testDict.ContainsKey(alexbegh.vMerge.Constants.Settings.ProfileKey), "Profiles not in Settings dict");
            Assert.IsTrue(profiles.ContainsKey(testUri), "Uri " + testUri + " not in profiles");
            Assert.IsTrue(profiles[testUri].ContainsKey(profileName), "Profile " + profileName + " not in profiles");*/
        }

        private static void InitTest(alexbegh.vMerge.Model.Interfaces.ISettings test, SerializableDictionary<string, object> testDict, SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> profiles)
        {
            testDict.Add(alexbegh.vMerge.Constants.Settings.ProfileKey, profiles);
            ((Settings)test).SerializedSettings = testDict;
        }




        //[TestMethod]
        //public void TestMethod2()
        //{
        //    var test = new Settings();
        //    var testDict = new SerializableDictionary<string, object>();
        //    var testInnerDict = new SerializableDictionary<string, object>();
        //    testDict.Add("abc", testInnerDict);
        //    testInnerDict.Add("innerAbc", "1234");
        //    //test.SerializedSettings = testDict;
        //    //test.SaveSettings(_file);
        //    //test.LoadSettings(_file);
        //    //Assert.AreEqual(testDict, test.SerializedSettings);

        //    Serializer.JsonSerialize(testDict, _file);
        //}

        //C:\Users\RoederT\AppData\Roaming\qbus.vMerge.settings.qvmset
        //[TestMethod]
        //public void TestMethodLoadProdData()
        //{

        //    SerializableDictionary<string, object> testDict = null;

        //    Serializer.JSonDeserialize(@"C:\Users\RoederT\AppData\Roaming\qbus.vMerge.settings.qvmset", out testDict);

        //    Assert.IsNotNull(testDict);
        //}

    }
}
