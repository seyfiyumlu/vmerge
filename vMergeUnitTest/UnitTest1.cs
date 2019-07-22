using alexbegh.Utility.SerializationHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace vMergeUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private string _file;

        [TestInitialize]
        public void Setup()
        {
            _file = Path.GetTempFileName();
        }

        [TestCleanup]
        public void TearDown()
        {
            File.Delete(_file);
        }


        [TestMethod]
        public void TestMethod1()
        {
            var test = "abc";
            Serializer.JsonSerialize(test, _file);

            Assert.IsTrue(File.Exists(_file), "Test "+ _file+" exists after serialize");

            var testRead = string.Empty;
            Serializer.JSonDeserialize(_file, out testRead);

            Assert.AreEqual(test, testRead, "Compare after serialize/desrialize");
        }
    }
}
