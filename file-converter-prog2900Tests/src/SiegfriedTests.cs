using System.Text.Json;

namespace FileConverter.Siegfried.Tests
{
    /// <summary>
    /// Contains unit tests for the Siegfried class
    /// </summary>
    [TestClass()]
    public class SiegfriedTests
    {
        Siegfried siegfried = Siegfried.Instance;

        /// <summary>
        /// Tests the ParseJSONOutput method to ensure it parses JSON output correctly
        /// </summary>
        [TestMethod()]
        public void ParseJSONOutputTest()
        {
            // Arrange
            var json = @"{
        ""siegfried"": ""1.7.0"",
        ""scandate"": ""2024-04-25"",
        ""files"": [
            {
                ""filename"": ""test.txt"",
                ""filesize"": 1024,
                ""modified"": ""2024-04-25"",
                ""errors"": """",
                ""matches"": []
            }
        ]
        }";

            // Act
            var siegfriedJSON = Siegfried.ParseJSONOutput(json, readFromFile: false);

            // Assert
            Assert.IsNotNull(siegfriedJSON);
            Assert.AreEqual("1.7.0", siegfriedJSON.siegfriedVersion);
            Assert.AreEqual("2024-04-25", siegfriedJSON.scandate);
            Assert.AreEqual(1, siegfriedJSON.files.Length);
            Assert.AreEqual("test.txt", siegfriedJSON.files[0].filename);
            Assert.AreEqual(1024, siegfriedJSON.files[0].filesize);
        }

        /// <summary>
        /// Tests the GroupPaths method to ensure it groups file paths correctly
        /// </summary>
        [TestMethod()]
        public void GroupPathsTest()
        {
            // Arrange
            var paths = new List<string>
        {
            "path1/file1.txt",
            "path2/file2.txt",
            "path3/file3.txt",
            "path4/file4.txt",
            "path5/file5.txt"
        };

            // Act
            var filePathGroups = siegfried.GroupPaths(paths);

            // Assert
            Assert.AreEqual(1, filePathGroups.Count);
            CollectionAssert.AreEqual(new[] { "path1/file1.txt", "path2/file2.txt", "path3/file3.txt", "path4/file4.txt", "path5/file5.txt" }, filePathGroups[0]);
        }

        /// <summary>
        /// Tests the ParseSiegfriedFile method to ensure it parses a JSON file element correctly
        /// </summary>
        [TestMethod()]
        public void ParseSiegfriedFileTest()
        {
            // Arrange
            var fileElement = JsonDocument.Parse(@"{
        ""filename"": ""test.txt"",
        ""filesize"": 1024,
        ""modified"": ""2024-04-25"",
        ""errors"": """",
        ""matches"": [
            {""ns"": ""ns1"", ""id"": ""1"", ""format"": ""format1"", ""version"": ""1.0"", ""mime"": ""mime1"", ""class"": ""class1"", ""basis"": ""basis1"", ""warning"": ""warning1""},
            {""ns"": ""ns2"", ""id"": ""2"", ""format"": ""format2"", ""version"": ""2.0"", ""mime"": ""mime2"", ""class"": ""class2"", ""basis"": ""basis2"", ""warning"": ""warning2""}
        ]
    }").RootElement;

            // Act
            var siegfriedFile = Siegfried.ParseSiegfriedFile(fileElement);

            // Assert
            Assert.AreEqual("test.txt", siegfriedFile.filename);
            Assert.AreEqual(1024, siegfriedFile.filesize);
            Assert.AreEqual("2024-04-25", siegfriedFile.modified);
            Assert.AreEqual("", siegfriedFile.errors);
            Assert.AreEqual(2, siegfriedFile.matches.Length);
            Assert.AreEqual("ns1", siegfriedFile.matches[0].ns);
            Assert.AreEqual("1", siegfriedFile.matches[0].id);
            Assert.AreEqual("format1", siegfriedFile.matches[0].format);
            Assert.AreEqual("1.0", siegfriedFile.matches[0].version);
            Assert.AreEqual("mime1", siegfriedFile.matches[0].mime);
            Assert.AreEqual("class1", siegfriedFile.matches[0].class_);
            Assert.AreEqual("basis1", siegfriedFile.matches[0].basis);
            Assert.AreEqual("warning1", siegfriedFile.matches[0].warning);
            Assert.AreEqual("ns2", siegfriedFile.matches[1].ns);
            Assert.AreEqual("2", siegfriedFile.matches[1].id);
            Assert.AreEqual("format2", siegfriedFile.matches[1].format);
            Assert.AreEqual("2.0", siegfriedFile.matches[1].version);
            Assert.AreEqual("mime2", siegfriedFile.matches[1].mime);
            Assert.AreEqual("class2", siegfriedFile.matches[1].class_);
            Assert.AreEqual("basis2", siegfriedFile.matches[1].basis);
            Assert.AreEqual("warning2", siegfriedFile.matches[1].warning);
        }

        /// <summary>
        /// Tests the ParseSiegfriedMatches method to ensure it parses a JSON match element correctly
        /// </summary>
        [TestMethod()]
        public void ParseSiegfriedMatchesTest()
        {
            // Arrange
            var matchElement = JsonDocument.Parse(@"{
        ""ns"": ""ns1"",
        ""id"": ""1"",
        ""format"": ""format1"",
        ""version"": ""1.0"",
        ""mime"": ""mime1"",
        ""class"": ""class1"",
        ""basis"": ""basis1"",
        ""warning"": ""warning1""
    }").RootElement;

            // Act
            var siegfriedMatches = Siegfried.ParseSiegfriedMatches(matchElement);

            // Assert
            Assert.AreEqual("ns1", siegfriedMatches.ns);
            Assert.AreEqual("1", siegfriedMatches.id);
            Assert.AreEqual("format1", siegfriedMatches.format);
            Assert.AreEqual("1.0", siegfriedMatches.version);
            Assert.AreEqual("mime1", siegfriedMatches.mime);
            Assert.AreEqual("class1", siegfriedMatches.class_);
            Assert.AreEqual("basis1", siegfriedMatches.basis);
            Assert.AreEqual("warning1", siegfriedMatches.warning);
        }
    }
}