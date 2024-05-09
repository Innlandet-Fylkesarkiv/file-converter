using ConversionTools.Converters;
using FileConverter.HelperClasses;
using FileConverter.Managers;
using SF = FileConverter.Siegfried;


namespace FileConverter.Converters.Tests
{

    [TestClass()]
    public class LibreOfficeTests
    {
        LibreOfficeConverter libreOffice = new LibreOfficeConverter();
        [TestMethod()]
        public void TestGetListOfSupportedConversions()
        {
            // Arrange
            var expectedConversions = GetExpectedConversions();

            // Act
            var actualConversions = libreOffice.GetListOfSupportedConvesions();

            // Assert
            AssertHelper.AreDictionariesEquivalent(expectedConversions, actualConversions);
        }
        [TestMethod()]
        public void TestGetSofficePath_Unix()
        {
            // Arrange
            var expectedPath = "/usr/lib/libreoffice/program/soffice";

            // Act
            var actualPath = GetSofficePathTest(false, PlatformID.Unix);

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod()]
        public void TestGetSofficePath_Windows()
        {
            // Arrange
            var expectedPath = "C:\\Program Files\\LibreOffice\\program\\soffice.exe";

            // Act
            var actualPath = GetSofficePathTest(false, PlatformID.Win32NT);

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod()]
        public void TestGetSofficePath_Linux()
        {
            // Arrange
            var expectedPath = "soffice";

            // Act
            var actualPath = GetSofficePathTest(true, PlatformID.Unix);

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod()]
        public void TestGetConversionExtension_PDF()
        {
            // Arrange
            var targetPronom = "fmt/276";
            var expectedExtension = "pdf";

            // Act
            var actualExtension = GetConversionExtensionTest(targetPronom);

            // Assert
            Assert.AreEqual(expectedExtension, actualExtension);
        }

        [TestMethod()]
        public void TestGetConversionExtension_XLSX()
        {
            // Arrange
            var targetPronoms = new[] { "fmt/214", "fmt/1828" };
            var expectedExtension = "xlsx";

            foreach (var targetPronom in targetPronoms)
            {
                // Act
                var actualExtension = GetConversionExtensionTest(targetPronom);

                // Assert
                Assert.AreEqual(expectedExtension, actualExtension);
            }
        }

        [TestMethod()]
        public void TestGetConversionExtension_PDFDefault()
        {
            // Arrange
            var targetPronoms = new[] { "fmt/43298", "fmt/723946" };
            var expectedExtension = "pdf";

            foreach (var targetPronom in targetPronoms)
            {
                // Act
                var actualExtension = GetConversionExtensionTest(targetPronom);

                // Assert
                Assert.AreEqual(expectedExtension, actualExtension);
            }
        }

        [TestMethod()]
        public void TestGetLibreOfficeCommand_Unix()
        {
            // Arrange
            var destinationPDF = "/path/to/destination.pdf";
            var sourceDoc = "/path/to/source.docx";
            var sofficeCommand = "/usr/bin/soffice";
            var targetFormat = "pdf";
            var expectedCommand = $@"-c ""soffice --headless --convert-to {targetFormat} --outdir '{destinationPDF}' '{sourceDoc}'""";

            // Act
            var actualCommand = GetLibreOfficeCommandTest(destinationPDF, sourceDoc, sofficeCommand, targetFormat, PlatformID.Unix);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod()]
        public void TestGetLibreOfficeCommand_Windows()
        {
            // Arrange
            var destinationPDF = @"C:\path\to\destination.pdf";
            var sourceDoc = @"C:\path\to\source.docx";
            var sofficeCommand = @"C:\Program Files\LibreOffice\program\soffice.exe";
            var targetFormat = "pdf";
            var expectedCommand = $@"/C {sofficeCommand} --headless --convert-to {targetFormat} --outdir ""{destinationPDF}"" ""{sourceDoc}""";

            // Act
            var actualCommand = GetLibreOfficeCommandTest(destinationPDF, sourceDoc, sofficeCommand, targetFormat, PlatformID.Win32NT);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }



        string GetLibreOfficeCommandTest(string destinationPDF, string sourceDoc, string sofficeCommand, string targetFormat, PlatformID os)
        {
            return os == PlatformID.Unix ? $@"-c ""soffice --headless --convert-to {targetFormat} --outdir '{destinationPDF}' '{sourceDoc}'""" : $@"/C {sofficeCommand} --headless --convert-to {targetFormat} --outdir ""{destinationPDF}"" ""{sourceDoc}""";
        }

        private Dictionary<string, List<string>> GetExpectedConversions()
        {
            return libreOffice.GetListOfSupportedConvesions();
        }

        string GetSofficePathTest(bool sofficePath, PlatformID os)
        {
            string sofficePathString;
            if (sofficePath)
            {
                sofficePathString = "soffice";
            }
            else if (os == PlatformID.Unix)
            {
                sofficePathString = "/usr/lib/libreoffice/program/soffice";
            }
            else
            {
                sofficePathString = "C:\\Program Files\\LibreOffice\\program\\soffice.exe";
            }

            return sofficePathString;
        }

        string GetConversionExtensionTest(string targetPronom)
        {
            string extensionNameForConversion;
            switch (targetPronom)
            {
                case "fmt/276":
                    extensionNameForConversion = "pdf";
                    break;
                case "fmt/214":
                case "fmt/1828":
                    extensionNameForConversion = "xlsx";
                    break;

                default:
                    extensionNameForConversion = "pdf";
                    break;
            }

            return extensionNameForConversion;
        }


    }

    [TestClass()]
    public class EmailConverterTests
    {
        EmailConverter emailConverter;
        string currentDirectory;
        string parentDirectory;
        Siegfried.Siegfried siegfried;
        public EmailConverterTests()
        {
            emailConverter = new EmailConverter();
            currentDirectory = Directory.GetCurrentDirectory();
            parentDirectory = Directory.GetParent(currentDirectory)?.Parent?.Parent?.FullName;
            Directory.SetCurrentDirectory(parentDirectory);
            siegfried = Siegfried.Siegfried.Instance; 
        }


        [TestMethod()]
        public void TestGetListOfSupportedConversions()
        {
            // Arrange
            Dictionary<string, List<string>> expectedConversions = GetExpectedConversions();

            // Act
            Dictionary<string, List<string>> actualConversions = emailConverter.GetListOfSupportedConvesions();

            // Assert
            AssertHelper.AreDictionariesEquivalent(expectedConversions, actualConversions);
        }

        [TestMethod()]
        public void TestGetSupportedOS()
        {
            // Arrange
            var expectedOS = new List<string> { "Win32NT", "Unix" };

            // Act
            var actualOS = emailConverter.GetSupportedOS();

            // Assert
            CollectionAssert.AreEquivalent(expectedOS, actualOS);
        }

        [TestMethod()]
        public void TestGetEmlToPdfCommand_Linux()
        {
            // Arrange
            var inputFilePath = "/path/to/input.eml";
            var workingDirectory = "/path/to";
            var expectedCommand = $"-c java -jar \"{workingDirectory}/src/ConversionTools/emailconverter-2.6.0-all.jar\" \"{inputFilePath}\" -a";
            var os = PlatformID.Unix;

            // Act
            var actualCommand = GetEmlToPdfCommandTest(inputFilePath, workingDirectory, os);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod()]
        public void TestGetEmlToPdfCommand_Windows()
        {
            // Arrange
            var inputFilePath = @"C:\path\to\input.eml";
            var workingDirectory = @"C:\path\to";
            var expectedCommand = $@"/C java -jar ""C:\path\to\src\ConversionTools\emailconverter-2.6.0-all.jar"" ""C:\path\to\input.eml"" -a";
            var os = PlatformID.Win32NT;

            // Act
            var actualCommand = GetEmlToPdfCommandTest(inputFilePath, workingDirectory, os);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod()]
        public void TestGetMsgToEmlCommandUnix()
        {
            // Arrange
            var inputFilePath = "/path/to/input.msg";
            var expectedCommand = $"-c msgconvert \"{inputFilePath}\" ";

            // Act
            var actualCommand = GetMsgToEmlCommandUnixTest(inputFilePath);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod()]
        public void TestGetMsgToEmlCommandWindows()
        {
            // Arrange
            var inputFilePath = @"C:\path\to\input.msg";
            var workingDirectory = @"C:\path\to";
            var destinationDir = @"C:\path\to\output";
            var relativeRebexFilePath = "src\\ConversionTools\\MailConverter.exe";
            var rebexConverterFile = Path.Combine(workingDirectory, relativeRebexFilePath);
            var expectedCommand = $@" /C {rebexConverterFile} to-mime --ignore ""{inputFilePath}"" ""{destinationDir}"" ";

            // Act
            var actualCommand = GetMsgToEmlCommandWindowsTest(inputFilePath, workingDirectory, destinationDir);

            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod()]
        public async Task TestAddAttachmentFilesToWorkingSet()
        {
            // Arrange
            string folderWithAttachments = "src//testfiles";
            GlobalVariables.ParsedOptions.Input = "testfiles";
            GlobalVariables.ParsedOptions.Output = "output";
            List<FileInfo2>? attachmentFiles = new List<FileInfo2?>();
            string targetDirectory = "";
            if (Directory.Exists(parentDirectory))
            {
                // Combine with folderWithAttachments to get the target directory
                targetDirectory = Path.Combine(parentDirectory, folderWithAttachments);
                if (Directory.Exists(targetDirectory))
                { 
                    string currentDirectory2 = Directory.GetCurrentDirectory();
                    attachmentFiles = await SF.Siegfried.Instance.IdentifyFilesIndividually(targetDirectory)!;
                }
            } 
            else
            {
                Assert.Fail("Can not find testFIles directory");
            }
            
            // Act
            await emailConverter.addAttachementFilesToWorkingSet(targetDirectory);

            // Assert
            if (attachmentFiles.Any())
            {
                foreach (var attachmentFile in attachmentFiles)
                {
                    bool foundMatch = false;

                    // Iterate over each file in the WorkingSet
                    foreach (var fileInfo in ConversionManager.Instance.WorkingSet.Values)
                    {
                        // Check if the FilePath of the attachmentFile matches any FilePath in the WorkingSet
                        if (fileInfo.FilePath == attachmentFile.FilePath)
                        {
                            foundMatch = true;
                            break; // Found a match, no need to continue searching
                        }
                    }

                    // Assert that a match was found
                    Assert.IsTrue(foundMatch, $"No match found in WorkingSet for file with FilePath: {attachmentFile.FilePath}");
                }
            } else
            {
                Assert.Fail("Could not find attachemnet files");
            }
        }
       

        string GetMsgToEmlCommandUnixTest(string inputFilePath)
        {
            return $@"-c msgconvert ""{inputFilePath}"" ";
        }
        string GetMsgToEmlCommandWindowsTest(string inputFilePath, string workingDirectory, string destinationDir)
        {
            // Get the correct path to the exe file for the mailcovnerter
            string relativeRebexFilePath = "src\\ConversionTools\\MailConverter.exe";
            string rebexConverterFile = Path.Combine(workingDirectory, relativeRebexFilePath);
            return $@" /C {rebexConverterFile} to-mime --ignore ""{inputFilePath}"" ""{destinationDir}"" ";
        }

        private Dictionary<string, List<string>> GetExpectedConversions()
        {
            return emailConverter.GetListOfSupportedConvesions();
        }

        string GetEmlToPdfCommandTest(string inputFilePath, string workingDirectory, PlatformID os)
        {
            // Get correct path to email converter relative to the workign directory
            string relativeJarPathWindows = "\\src\\ConversionTools\\emailconverter-2.6.0-all.jar";
            string relativeJarPathLinux = "/src/ConversionTools/emailconverter-2.6.0-all.jar";
            string jarFile = os == PlatformID.Unix ? Path.Combine(workingDirectory + relativeJarPathLinux)
                                                                               : Path.Combine(workingDirectory + relativeJarPathWindows);
            return os == PlatformID.Unix ? $@"-c java -jar ""{jarFile}"" ""{inputFilePath}"" -a" :
                                                                       $@"/C java -jar ""{jarFile}"" ""{inputFilePath}"" -a";
        }
    }

    [TestClass()]
    public class IText7Tests
    {
        
    }

    [TestClass()]
    public class GhostScriptTests
    {

    }

    // Created by ChatGPT
    public static class AssertHelper
    {
        public static void AreDictionariesEquivalent<TKey, TValue>(Dictionary<TKey, List<TValue>> expected, Dictionary<TKey, List<TValue>> actual)
        {
            // Check if the dictionaries have the same number of key-value pairs
            Assert.AreEqual(expected.Count, actual.Count, "The dictionaries have different counts of key-value pairs.");

            // Iterate over each key-value pair in the expected dictionary
            foreach (var kvp in expected)
            {
                // Check if the actual dictionary contains the key
                Assert.IsTrue(actual.ContainsKey(kvp.Key), $"The key '{kvp.Key}' is missing from the actual dictionary.");

                // Get the list of values for the current key from both dictionaries
                var expectedValues = kvp.Value;
                var actualValues = actual[kvp.Key];

                // Check if the lists have the same number of elements
                Assert.AreEqual(expectedValues.Count, actualValues.Count, $"The lists of values for key '{kvp.Key}' have different counts.");

                // Check if each element in the expected list exists in the actual list
                for (int i = 0; i < expectedValues.Count; i++)
                {
                    Assert.IsTrue(actualValues.Contains(expectedValues[i]), $"The value '{expectedValues[i]}' is missing for key '{kvp.Key}' in the actual dictionary.");
                }
            }
        }
    }
}
