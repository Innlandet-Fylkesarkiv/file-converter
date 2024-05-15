﻿using FileConverter.HelperClasses;
using FileConverter.Managers;
using FileConverter.Siegfried;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using SF = FileConverter.Siegfried;


namespace FileConverter.HelperClasses.Tests
{
    [TestClass()]
    public class FileInfo2Tests
    {

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(executableDirectory);
            Directory.SetCurrentDirectory("../../../");
            string newDirectory = Directory.GetCurrentDirectory();
            string currentDirectory = Directory.GetCurrentDirectory();
        }
        [TestMethod]
        public async Task RenameFile_ShouldRenameFile()
        {
            // Arrange
            var originalFileName = "original.txt";
            var newFileName = "renamed.txt";
            FileInfo2 fileInfo;
            SF.Siegfried sf = SF.Siegfried.Instance;
            string currentDirectory = Directory.GetCurrentDirectory();
            string originalFilePath = Path.Combine(currentDirectory, "src", "testFiles", originalFileName);
            string newFilePath = Path.Combine(currentDirectory, "src", "testFiles", newFileName);
            string testFilesDirectory = Path.Combine(currentDirectory, "src", "testFiles");
            File.Create(originalFilePath).Close(); // Create a dummy 
            List<FileInfo2>? siegfriedFiles = new List<FileInfo2?>();
            siegfriedFiles = await sf.IdentifyFilesIndividually(testFilesDirectory);

            if (siegfriedFiles != null)
            {

                foreach (FileInfo2 file in siegfriedFiles)
                {
                    if (file.OriginalFilePath.Equals(originalFilePath))
                    {
                        fileInfo = new FileInfo2(originalFilePath, file);
                        //act
                        fileInfo.RenameFile(newFileName);
                        // Assert
                        Directory.SetCurrentDirectory("../");
                        Assert.IsTrue(File.Exists(newFilePath)); // Check if file exists with new name
                        Assert.IsFalse(File.Exists(originalFilePath)); // Check if file with old name does not exist
                        Assert.AreEqual(newFilePath, fileInfo.FilePath); // Check if FilePath property is updated
                        Assert.AreEqual(newFilePath, fileInfo.OriginalFilePath); // Check if OriginalFilePath property is updated
                    }
                }
            }
            else
            {
                Assert.Fail("Siegfried file was not identifed correctly!");
            }

            // Cleanup
            File.Delete(newFileName); // Delete the renamed file
        }

        [TestMethod]
        public void TestFileInfo2ConstructorWithPathAndFileInfo()
        {
            // Arrange
            var siegfriedFile = new SiegfriedFile
            {
                filesize = 100,
                filename = "test.txt",
                hash = "hash",
                matches = new SiegfriedMatches[] { new SiegfriedMatches { id = "id", format = "format", mime = "mime" } }
            };
            FileInfo2 originalFileInfo = new FileInfo2(siegfriedFile, "test");
            FileInfo2 fileInfoToCopy = new FileInfo2("test.txt", originalFileInfo);

            // Assert
            Assert.AreEqual(originalFileInfo.FilePath, fileInfoToCopy.FilePath);
            Assert.AreEqual(originalFileInfo.OriginalFilePath, fileInfoToCopy.OriginalFilePath);
            Assert.AreEqual(originalFileInfo.OriginalPronom, fileInfoToCopy.OriginalPronom);
            Assert.AreEqual(originalFileInfo.OriginalChecksum, fileInfoToCopy.OriginalChecksum);
            Assert.AreEqual(originalFileInfo.OriginalFormatName, fileInfoToCopy.OriginalFormatName);
            Assert.AreEqual(originalFileInfo.OriginalMime, fileInfoToCopy.OriginalMime);
            Assert.AreEqual(originalFileInfo.OriginalSize, fileInfoToCopy.OriginalSize);
            Assert.AreEqual(originalFileInfo.Parent, fileInfoToCopy.Parent);
        }

        [TestMethod]
        public void TestFileInfo2ConstructorWithSiegfriedFile()
        {
            // Arrange
            var siegfriedFile = new SiegfriedFile
            {
                filesize = 100,
                filename = "test.txt",
                hash = "hash",
                matches = new SiegfriedMatches[] { new SiegfriedMatches { id = "id", format = "format", mime = "mime" } }
            };

            // Act
            FileInfo2 fileInfo = new FileInfo2(siegfriedFile, "test");

            // Assert
            Assert.AreEqual(siegfriedFile.filesize, fileInfo.OriginalSize);
            Assert.AreEqual(siegfriedFile.filename, fileInfo.OriginalFilePath);
            Assert.AreEqual(siegfriedFile.matches[0].id, fileInfo.OriginalPronom);
            Assert.AreEqual(siegfriedFile.hash, fileInfo.OriginalChecksum);
            Assert.AreEqual(siegfriedFile.matches[0].format, fileInfo.OriginalFormatName);
            Assert.AreEqual(siegfriedFile.matches[0].mime, fileInfo.OriginalMime);
        }

        [TestMethod]
        public void TestFileInfo2ConstructorWithFileToConvert()
        {
            // Arrange
            string filePath = "test.txt";
            var fileToConvert = new FileToConvert(filePath, Guid.NewGuid(), "targetPronom")
            {
                CurrentPronom = "currentPronom",
                Route = new List<string> { "route1", "route2" },
                IsModified = true,
                addedDuringRun = true,
            };

            // Act
            FileInfo2 fileInfo = new FileInfo2(fileToConvert);

            // Assert
            Assert.AreEqual(fileToConvert.FilePath, fileInfo.FilePath);
            Assert.AreEqual(fileInfo.OriginalFilePath, Path.GetFileName(fileToConvert.FilePath));
        }

        [TestMethod]
        public void TestUpdateSelfWithNonNullFileInfo()
        {
            // Arrange
            var originalSiegfriedFile = new SiegfriedFile
            {
                filename = "test.txt",
                filesize = 12345,
                matches = new[] { new SiegfriedMatches { id = "pronom123", format = "FormatName", mime = "text/plain" } }
            };

            var newSiegfriedFile = new SiegfriedFile
            {
                filename = "test2.txt",
                filesize = 54321,
                matches = new[] { new SiegfriedMatches { id = "pronom456", format = "NewFormatName", mime = "text/csv" } }
            };

            // Act
            var originalFileInfo = new FileInfo2(originalSiegfriedFile, "test");
            var newFileInfo = new FileInfo2(newSiegfriedFile, "test");

            // Act
            originalFileInfo.UpdateSelf(newFileInfo);

            // Assert
            Assert.AreEqual(newFileInfo.OriginalPronom, originalFileInfo.NewPronom);
            Assert.AreEqual(newFileInfo.OriginalFormatName, originalFileInfo.NewFormatName);
            Assert.AreEqual(newFileInfo.OriginalMime, originalFileInfo.NewMime);
            Assert.AreEqual(newFileInfo.OriginalSize, originalFileInfo.NewSize);
            Assert.AreEqual(newFileInfo.OriginalChecksum, originalFileInfo.NewChecksum);
        }
    }


    [TestClass()]
    public class LoggerTests
    {
        private readonly Logger logger = Logger.Instance;
        [TestMethod()]
        public void TestSetRequesterAndConverter()
        {
            // Arrange
            var expectedRequesterAndConverter = GetExpectedRequesterAndConverter();
            Debug.WriteLine("Expected Requester and Converter: " + expectedRequesterAndConverter);
            // Act
            GlobalVariables.ParsedOptions.AcceptAll = true;
            Logger.SetRequesterAndConverter();
            // Assert
            Assert.AreEqual(expectedRequesterAndConverter, Logger.JsonRoot.Requester);
            Assert.AreEqual(expectedRequesterAndConverter, Logger.JsonRoot.Converter);
            Debug.WriteLine("Actual Requester: " + Logger.JsonRoot.Requester);
            Debug.WriteLine("Actual Converter: " + Logger.JsonRoot.Converter);
        }

        private static string GetExpectedRequesterAndConverter()
        {
            string user = Environment.UserName;
            if (OperatingSystem.IsWindows())
            {
                user = UserPrincipal.Current.DisplayName;
            }
            return user;
        }
    }
}
