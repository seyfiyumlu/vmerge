using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace vMerge.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var name = "";
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge",
                name + ".qvmset");

            if (!File.Exists(path))
                return;

            try
            {

                    SerializableDictionary<string, object> serializedSettings;
                    Serializer.XmlDeserialize(path, out serializedSettings);
                    SerializedSettings = serializedSettings;
                
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}
