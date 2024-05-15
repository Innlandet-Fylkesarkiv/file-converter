using ConversionTools.Converters;
using FileConverter.HelperClasses;
using FileConverter.Managers;
using iText.Kernel.Pdf;
using Org.BouncyCastle.Asn1.Cms;
using System.Collections.Concurrent;
using SF = FileConverter.Siegfried;


namespace FileConverter.Converters.Tests
{

    [TestClass()]
    public class LibreOfficeTests
    {
        LibreOfficeConverter libreOffice = new LibreOfficeConverter();

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(executableDirectory);
            Directory.SetCurrentDirectory("../../../");
            string newDirectory = Directory.GetCurrentDirectory();
            string currentDirectory = Directory.GetCurrentDirectory();
        }
        [TestMethod()]
        public void TestGetListOfSupportedConversions()
        {
            // Arrange
            var expectedConversions = GetExpectedConversions();

            // Act
            var actualConversions = libreOffice.GetListOfSupportedConvesions();

            // Assert
            Helper.AreDictionariesEquivalent(expectedConversions, actualConversions);
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
            string currentDirectory= Directory.GetCurrentDirectory();
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
        string parentDirectory;
        Siegfried.Siegfried siegfried;
        public EmailConverterTests()
        {
            emailConverter = new EmailConverter();
            Directory.SetCurrentDirectory("../../../");
            parentDirectory = Directory.GetCurrentDirectory();
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
            Helper.AreDictionariesEquivalent(expectedConversions, actualConversions);
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
            string folderWithAttachments = "src\\testFiles";
            GlobalVariables.ParsedOptions.Input = "testFiles";
            GlobalVariables.ParsedOptions.Output = "output";
            Console.WriteLine(parentDirectory);
            List<FileInfo2>? attachmentFiles = new List<FileInfo2?>();
            var key = new KeyValuePair<string, string>("fmt/412", "fmt/456");
            var value = new List<string> { "fmt/412", "fmt/212", "fmt/313" };
            ConversionManager cm = ConversionManager.Instance;
            cm.ConversionMap = new ConcurrentDictionary<KeyValuePair<string, string>, List<string>>();
            cm.ConversionMap.TryAdd(key, value);
            string targetDirectory = "";
            if (Directory.Exists(parentDirectory))
            {
                // Combine with folderWithAttachments to get the target directory
                targetDirectory = Path.Combine(parentDirectory, folderWithAttachments);
                if (Directory.Exists(targetDirectory))
                {
                    // Get the list of attachement files so they can later be added to the working set
                    attachmentFiles = await SF.Siegfried.Instance.IdentifyFilesIndividually(targetDirectory)!;
                }
            }
            else
            {
                Assert.Fail("Can not find testFiles directory");
            }

            // Act
            await emailConverter.AddAttachementFilesToWorkingSet(targetDirectory);

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
            }
            else
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
        private static IText7 iText7;
        private Helper helper = new Helper();
        
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(executableDirectory);
            Directory.SetCurrentDirectory("../../../");
            string newDirectory = Directory.GetCurrentDirectory();
            iText7 = new IText7();
            string currentDirectory = Directory.GetCurrentDirectory();
        }

        [TestMethod]
        public void TestGetListOfSupportedConversions()
        {
            string currentDirecroty = Directory.GetCurrentDirectory();
            // Arrange
            var expectedConversions = new Dictionary<string, List<string>>();

            // Add image conversions
            foreach (var imagePronom in helper.ImagePronoms)
            {
                expectedConversions.Add(imagePronom, helper.PDFPronoms);
            }

            // Add HTML conversions
            foreach (var htmlPronom in helper.HTMLPronoms)
            {
                expectedConversions.Add(htmlPronom, helper.PDFPronoms);
            }

            // Add PDF conversions
            foreach (var pdfPronom in helper.PDFPronoms)
            {
                expectedConversions.Add(pdfPronom, helper.PDFPronoms);
            }

            // Act
            var actualConversions = iText7.GetListOfSupportedConvesions();

            // Assert
            Helper.AreDictionariesEquivalent(expectedConversions, actualConversions);
        }

        [TestMethod]
        public void TestGetListOfBlockingConversions()
        {
            // Arrange. No Conversion are blocking in IText7
            var expectedBlockingConversions = new Dictionary<string, List<string>>();

            // Act
            var actualBlockingConversions = iText7.GetListOfBlockingConversions();

            // Assert
            Helper.AreDictionariesEquivalent(expectedBlockingConversions, actualBlockingConversions);
        }
        [TestMethod]
        public void TestGetSupportedOS()
        {
            // Arrange
            var expectedOS = new List<string> { PlatformID.Win32NT.ToString(), PlatformID.Unix.ToString() };

            // Act
            var actualOS = iText7.GetSupportedOS();

            // Assert
            CollectionAssert.AreEquivalent(expectedOS, actualOS);
        }
        [TestMethod]
        public void TestGetPDFVersion()
        {
            // Arrange
            string knownPronom = "fmt/95";
            PdfVersion expectedVersion = PdfVersion.PDF_1_4;

            // Act
            PdfVersion actualVersion = GetPDFVersion(knownPronom);

            // Assert
            Assert.AreEqual(expectedVersion, actualVersion);
        }

        /*
        [TestMethod]
        public void TestGetPdfAConformanceLevel()
        {
            // Arrange
            string knownPronom = "fmt/479";
            PdfAConformanceLevel? expectedConformanceLevel = PdfAConformanceLevel.PDF_A_3B;

            // Act
            PdfAConformanceLevel? actualConformanceLevel = itext7.GetPdfAConformanceLevel(knownPronom);

            // Assert
            Assert.AreEqual(expectedConformanceLevel, actualConformanceLevel);
        }*/
        /*
        [TestMethod]
        public void TestSetToPDFABasic()
        {
            // Arrange
            string knownPronom = "fmt/95";
            PdfAConformanceLevel expectedConformanceLevel;
            PdfVersion expectedVersion;
            string expectedPronom = "fmt/354";

            // Act
            string actualPronom = itext7.SetToPDFABasic(knownPronom, out expectedConformanceLevel, out expectedVersion);

            // Assert
            Assert.AreEqual(expectedPronom, actualPronom);
            Assert.AreEqual(PdfAConformanceLevel.PDF_A_1B, expectedConformanceLevel);
            Assert.AreEqual(PdfVersion.PDF_1_4, expectedVersion);
        }*/
        PdfVersion GetPDFVersion(string pronom)
        {
            if (helper.PronomToPdfVersion.TryGetValue(pronom, out var pdfVersion))
            {
                return pdfVersion;
            }
            else
            {
                return PdfVersion.PDF_1_7;
            }
        }
        /*
        PdfAConformanceLevel? GetPdfAConformanceLevel(string pronom)
        {
            if (    .TryGetValue(pronom, out var pdfAConformanceLevel))
            {
                return pdfAConformanceLevel;
            }
            else
            {
                return null;
            }
        }*/
        [ClassCleanup]
        public static void ClassCleanup()
        {
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "ICCFiles");

            // Delete all files within the folder
            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                File.Delete(filePath);
            }

            // Delete the folder itself
            Directory.Delete(folderPath);
        }
    }

    [TestClass()]
    public class GhostScriptTests
    {

    }

    public class Helper
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
        public readonly List<string> ImagePronoms = [
          "fmt/3",
            "fmt/4",
            "fmt/11",
            "fmt/12",
            "fmt/13",
            "fmt/935",
            "fmt/41",
            "fmt/42",
            "fmt/43",
            "fmt/44",
            "x-fmt/398",
            "x-fmt/390",
            "x-fmt/391",
            "fmt/645",
            "fmt/1507",
            "fmt/112",
            "fmt/367",
            "fmt/1917",
            "x-fmt/399",
            "x-fmt/388",
            "x-fmt/387",
            "fmt/155",
            "fmt/353",
            "fmt/154",
            "fmt/153",
            "fmt/156",
            "x-fmt/270",
            "fmt/115",
            "fmt/118",
            "fmt/119",
            "fmt/114",
            "fmt/116",
            "fmt/117"
       ];
        public readonly List<string> HTMLPronoms = [
            "fmt/103",
            "fmt/96",
            "fmt/97",
            "fmt/98",
            "fmt/99",
            "fmt/100",
            "fmt/471",
            "fmt/1132",
            "fmt/102",
            "fmt/583"
        ];
        public readonly List<string> PDFPronoms = [
            "fmt/95",       // PDF/A 1A
            "fmt/354",      // PDF/A 1B
            "fmt/476",      // PDF/A 2A
            "fmt/477",      // PDF/A 2B
            "fmt/478",      // PDF/A 2U
            "fmt/479",      // PDF/A 3A
            "fmt/480",      // PDF/A 3B
            "fmt/481",      // PDF/A 3U
                            //"fmt/1910",     // PDF/A 4
                            //"fmt/1911",     // PDF/A 4E
                            //"fmt/1912",     // PDF/A 4F
            "fmt/14",       // PDF 1.0
            "fmt/15",       // PDF 1.1
            "fmt/16",       // PDF 1.2
            "fmt/17",       // PDF 1.3
            "fmt/18",       // PDF 1.4
            "fmt/19",       // PDF 1.5
            "fmt/20",       // PDF 1.6
            "fmt/276",      // PDF 1.7
            "fmt/1129"      // PDF 2.0
        ];
        public readonly List<string> PDFAPronoms = [
             "fmt/95",       // PDF/A 1A
            "fmt/354",      // PDF/A 1B
            "fmt/476",      // PDF/A 2A
            "fmt/477",      // PDF/A 2B
            "fmt/478",      // PDF/A 2U
            "fmt/479",      // PDF/A 3A
            "fmt/480",      // PDF/A 3B
            "fmt/481",      // PDF/A 3U
                            //"fmt/1910",     // PDF/A 4
                            //"fmt/1911",     // PDF/A 4E
                            //"fmt/1912",     // PDF/A 4F
        ];
        public Dictionary<String, PdfVersion> PronomToPdfVersion = new Dictionary<string, PdfVersion>()
        {
            {"fmt/14", PdfVersion.PDF_1_0},
            {"fmt/15", PdfVersion.PDF_1_1},
            {"fmt/16", PdfVersion.PDF_1_2},
            {"fmt/17", PdfVersion.PDF_1_3},
            {"fmt/18", PdfVersion.PDF_1_4},
            {"fmt/19", PdfVersion.PDF_1_5},
            {"fmt/20", PdfVersion.PDF_1_6},
            {"fmt/276", PdfVersion.PDF_1_7},
            {"fmt/1129", PdfVersion.PDF_2_0},
            {"fmt/95", PdfVersion.PDF_1_4 },    //PDF/A 1A
            {"fmt/354", PdfVersion.PDF_1_4 },   //PDF/A 1B
            {"fmt/476", PdfVersion.PDF_1_7 },   //PDF/A 2A
            {"fmt/477", PdfVersion.PDF_1_7 },   //PDF/A 2B
            {"fmt/478", PdfVersion.PDF_1_7 },   //PDF/A 2U
            {"fmt/479", PdfVersion.PDF_1_7 },   //PDF/A 3A
            {"fmt/480", PdfVersion.PDF_1_7 },   //PDF/A 3B
            {"fmt/481", PdfVersion.PDF_1_7 },   //PDF/A 3U
            {"fmt/1910", PdfVersion.PDF_2_0 },  //PDF/A 4
            {"fmt/1911", PdfVersion.PDF_2_0 },  //PDF/A 4E
            {"fmt/1912", PdfVersion.PDF_2_0 }   //PDF/A 4F
        };
    }
}

