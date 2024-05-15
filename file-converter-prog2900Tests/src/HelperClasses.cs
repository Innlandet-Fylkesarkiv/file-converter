using FileConverter.HelperClasses;
using FileConverter.Managers;
using FileConverter.Siegfried;
using SF = FileConverter.Siegfried;


namespace FileConverter.HelperClasses.Tests
{
    [TestClass()]
    public class HelperClassesTests
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
            string originalFilePath = Path.Combine(currentDirectory, "src\\testFiles\\" + originalFileName);
            string newFilePath = Path.Combine(currentDirectory, "src\\testFiles\\" +newFileName);
            string testFiles = Path.Combine(currentDirectory, "src\\testFiles\\");
            File.Create(originalFilePath).Close(); // Create a dummy 
            List<FileInfo2>? siegfriedFiles = new List<FileInfo2?>();
            siegfriedFiles = await sf.IdentifyFilesIndividually(testFiles);

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
    }
}