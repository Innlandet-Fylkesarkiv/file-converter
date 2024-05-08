using iText.Kernel.Pdf;
using iText.IO.Image;
using iText.Pdfa;
using iText.Html2pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas;
using iText.Commons.Actions;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using FileConverter.Managers;
using FileConverter.HelperClasses;
using FileConverter.Siegfried;
using System.Collections.Immutable;

/// <summary>
/// iText7 is a subclass of the Converter class.                                                     <br></br>
///                                                                                                  <br></br>
/// iText7 supports the following conversions:                                                       <br></br>
/// - Image (jpg, png, gif, tiff, bmp) to PDF 1.0-2.0                                                <br></br>
/// - Image (jpg, png, gif, tiff, bmp) to PDF-A 1A-3B                                                <br></br>
/// - HTML to PDF 1.0-2.0                                                                            <br></br>
/// - PDF 1.0-2.0 to PDF-A 1A-3B                                                                     <br></br>                                                                          
///                                                                                                  <br></br>
/// iText7 can also combine the following file formats into one PDF (1.0-2.0) or PDF-A (1A-3B):      <br></br>
/// - Image (jpg, png, gif, tiff, bmp)                                                               <br></br>
///                                                                                                  <br></br>
/// </summary>
/// 
namespace ConversionTools.Converters
{
    public class IText7 : Converter
    {
        private static readonly object pdfalock = new object();     //PDF-A uses a .icc file when converting, which can not be accessed by multiple threads at the same time

        public IText7()
        {
            Name = "iText7";
            SetNameAndVersion();
            SupportedConversions = GetListOfSupportedConvesions();
            SupportedOperatingSystems = GetSupportedOS();
            BlockingConversions = GetListOfBlockingConversions();

            DependenciesExists = true;   // Bundled with program 
                                         //Acknowledge AGPL usage warning
            EventManager.AcknowledgeAgplUsageDisableWarningMessage();
        }

        /// <summary>
        /// Get the version of the iText7 library
        /// </summary>
        public override void GetVersion()
        {
            Version = typeof(PdfWriter).Assembly.GetName().Version?.ToString() ?? "";
        }

        /// <summary>
        /// Reference list stating supported conversions containing key value pairs with string input pronom and string output pronom
        /// </summary>
        /// <returns>List of all conversions</returns>
        public override Dictionary<string, List<string>> GetListOfSupportedConvesions()
        {
            var supportedConversions = new Dictionary<string, List<string>>();
            foreach (string imagePronom in ImagePronoms)
            {
                supportedConversions.Add(imagePronom, PDFPronoms);
            }

            foreach (string htmlPronom in HTMLPronoms)
            {
                supportedConversions.Add(htmlPronom, PDFPronoms);
            }

            foreach (string pdfPronom in PDFPronoms)
            {
                supportedConversions.Add(pdfPronom, PDFPronoms);
            }

            return supportedConversions;
        }

        /// <summary>
        /// Get list of blocking conversions.
        /// </summary>
        /// <returns> A dictionary containing all conversions processes that are blocking</returns>
        public override Dictionary<string, List<string>> GetListOfBlockingConversions()
        {
            var blockingConversions = new Dictionary<string, List<string>>();
            foreach (string pronom in ImagePronoms.Concat(HTMLPronoms).Concat(PDFPronoms))
            {
                blockingConversions.Add(pronom, PDFAPronoms);
            }
            return blockingConversions;
        }

        /// <summary>
        /// Get the supported os for the iText7 library
        /// </summary>
        /// <returns>A list of supported OS</returns>
        public override List<string> GetSupportedOS()
        {
            var supportedOS = new List<string>();
            supportedOS.Add(PlatformID.Win32NT.ToString());
            supportedOS.Add(PlatformID.Unix.ToString());
            //Add more supported OS here
            return supportedOS;
        }

        /// <summary>
        /// Convert a file to a new format
        /// </summary>
        /// <param name="file">The file to be converted</param>
        /// <param name="pronom">The file format to convert to</param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async public override Task ConvertFile(FileToConvert file, string pronom)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                PdfVersion pdfVersion = GetPDFVersion(pronom);
                PdfAConformanceLevel? conformanceLevel = GetPdfAConformanceLevel(pronom);

                if (HTMLPronoms.Contains(file.CurrentPronom))
                {
                    convertFromHTMLToPDF(file, pdfVersion, conformanceLevel);
                }
                else if (PDFPronoms.Contains(file.CurrentPronom))
                {
                    convertFromPDFToPDF(file);
                }
                else if (ImagePronoms.Contains(file.CurrentPronom))
                {
                    convertFromImageToPDF(file, pdfVersion, conformanceLevel);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error converting file with iText7. Error message: " + e.Message, true, filename: file.FilePath);
            }
        }

        /// <summary>
        /// Get the filepath of the ICC file needed for PDF-A conversion
        /// </summary>
        /// <returns>A string with the full path</returns>
        static string GetICCFilePath()
        {
            string fileName = "sRGB2014.icc";
            string path = "";
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                path = files[0];
            }
            else
            {
                Logger.Instance.SetUpRunTimeLogMessage("ICC file not found: " + fileName, true);
            }
            return path;
        }

        /// <summary>
        /// Convert from any image file to pdf version 1.0-2.0
        /// </summary>
        /// <param name="file">The file being converted</param>
        /// <param name="pdfVersion">What pdf version it is being converted to</param>
        /// <param name="conformanceLevel"></param>
        void convertFromImageToPDF(FileToConvert file, PdfVersion pdfVersion, PdfAConformanceLevel? conformanceLevel = null)
        {
            string dir = Path.GetDirectoryName(file.FilePath)?.ToString() ?? "";
            string filePathWithoutExtension = Path.Combine(dir, Path.GetFileNameWithoutExtension(file.FilePath));
            string output = Path.Combine(filePathWithoutExtension + ".pdf");
            string filename = Path.Combine(file.FilePath);
            var filestream = File.ReadAllBytes(filename);
            try
            {
                int count = 0;
                bool converted = false;
                do
                {
                    using (var pdfWriter = new PdfWriter(output, new WriterProperties().SetPdfVersion(pdfVersion)))
                    using (var pdfDocument = new PdfDocument(pdfWriter))
                    using (var document = new iText.Layout.Document(pdfDocument))
                    {
                        pdfDocument.SetTagged();

                        var imageData = ImageDataFactory.Create(filestream, false);
                        iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData);
                        document.Add(image);
                    }
                    if (conformanceLevel != null)
                    {
                        convertFromPDFToPDFA(new FileToConvert(output, file.Id, file.Route.First()), conformanceLevel);
                    }

                    converted = CheckConversionStatus(output, file.TargetPronom, file);
                } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error converting Image to PDF. File is not converted: " + e.Message, true, filename: file.FilePath);
            }
        }

        /// <summary>
        /// Convert from any html file to pdf 1.0-2.0
        /// </summary>
        /// <param name="file">Name of the file to be converted</param>
        /// <param name="pdfVersion">Specific pdf version to be converted to</param>
        /// <param name="conformanceLevel">The type of PDF-A to convert to</param>
        void convertFromHTMLToPDF(FileToConvert file, PdfVersion pdfVersion, PdfAConformanceLevel? conformanceLevel = null)
        {
            string dir = Path.GetDirectoryName(file.FilePath)?.ToString() ?? "";
            string filePathWithoutExtension = Path.Combine(dir, Path.GetFileNameWithoutExtension(file.FilePath));
            string output = Path.Combine(filePathWithoutExtension + ".pdf");
            string filename = Path.Combine(file.FilePath);
            try
            {
                int count = 0;
                bool converted = false;
                do
                {
                    using (var pdfWriter = new PdfWriter(output, new WriterProperties().SetPdfVersion(pdfVersion)))
                    using (var pdfDocument = new PdfDocument(pdfWriter))
                    using (var document = new iText.Layout.Document(pdfDocument))
                    {
                        pdfDocument.SetTagged();

                        using (var htmlSource = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            //Create a URI for the base path
                            Uri uri = new Uri(Path.GetFullPath(filename), UriKind.RelativeOrAbsolute);
                            HtmlConverter.ConvertToPdf(htmlSource, pdfDocument, new ConverterProperties().SetBaseUri(uri.ToString()));
                        }
                        document.Close();
                        pdfDocument.Close();
                        pdfWriter.Close();
                        pdfWriter.Dispose();
                    }

                    if (conformanceLevel != null)
                    {
                        convertFromPDFToPDFA(new FileToConvert(output, file.Id, file.Route.First()), conformanceLevel);
                    }
                    converted = CheckConversionStatus(output, file.Route.First(), file);
                } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
                if (!converted)
                {
                    file.Failed = true;
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error converting HTML to PDF. File is not converted: " + e.Message, true, filename: file.FilePath);
            }
        }

        /// <summary>
        /// Convert from any pdf file to pdf-A version 1A-3U
        /// </summary>
        /// <param name="file">The file to convert</param>
        /// <param name="conformanceLevel">The type of PDF-A to convert to</param>
        void convertFromPDFToPDFA(FileToConvert file, PdfAConformanceLevel conformanceLevel)
        {
            try
            {
                string pdfaFileName = Path.Combine(Path.GetDirectoryName(file.FilePath) ?? "", Path.GetFileNameWithoutExtension(file.FilePath) + "_PDFA.pdf");
                string filename = Path.Combine(file.FilePath);
                int count = 0;
                bool converted = false;
                string pronom = file.Route.First();
                PdfOutputIntent outputIntent;

                string tmpFileName = RemoveInterpolation(filename);
                // Initialize PdfOutputIntent object
                lock (pdfalock)
                {
                    using (FileStream iccFileStream = new FileStream(GetICCFilePath(), FileMode.Open))
                    {
                        outputIntent = new PdfOutputIntent("Custom", "", "https://www.color.org", "sRGB IEC61966-2.1", iccFileStream);
                    }
                }
                do
                {
                    PdfVersion pdfVersion = GetPDFVersion(pronom);
                    //If the conversion failed with PDF/A Accessible, try PDF/A Basic/F depending on the pronom
                    if (PDFAAPronoms.Contains(pronom) && count > 0)
                    {
                        var oldConformance = conformanceLevel.ToString();
                        pronom = SetToPDFABasic(pronom, out conformanceLevel, out pdfVersion);
                        Logger.Instance.SetUpRunTimeLogMessage("PDF to PDF/A Accessible (" + oldConformance + ") conversion failed. Attempting " + conformanceLevel.ToString(), true, filename: file.FilePath);
                    }

                    using (PdfWriter writer = new PdfWriter(pdfaFileName, new WriterProperties().SetPdfVersion(pdfVersion))) // Create PdfWriter instance
                    using (PdfADocument pdfADocument = new PdfADocument(writer, conformanceLevel, outputIntent))    // Associate PdfADocument with PdfWriter
                    using (PdfReader reader = new PdfReader(tmpFileName))
                    {
                        PdfDocument pdfDocument = new PdfDocument(reader);
                        pdfADocument.SetTagged();

                        for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                        {
                            PdfPage sourcePage = pdfDocument.GetPage(pageNum);
                            PdfPage page = pdfADocument.AddNewPage(new PageSize(sourcePage.GetPageSize()));
                            PdfFormXObject pageCopy = sourcePage.CopyAsFormXObject(pdfADocument);

                            PdfCanvas canvas = new PdfCanvas(page);
                            canvas.AddXObject(pageCopy);
                        }
                    }
                    converted = CheckConversionStatus(pdfaFileName, pronom);
                } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
                if (!converted)
                {
                    file.Failed = true;
                    File.Delete(pdfaFileName);
                    File.Delete(tmpFileName);
                }
                else
                {
                    File.Delete(tmpFileName);
                    File.Delete(filename);
                    File.Move(pdfaFileName, filename);
                    ReplaceFileInList(filename, file);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error converting PDF to PDF-A. File is not converted: " + e.Message, true, filename: file.FilePath);
            }
        }

        static public string RemoveInterpolation(string filename)
        {
            int dotindex = filename.LastIndexOf('.');
            if (dotindex == -1)
            {
                dotindex = filename.Length - 3;
            }
            List<int> editedPages = new List<int>();
            string name = filename.Substring(0, dotindex);
            string newfilename = String.Format("{0}_{1}.pdf", name, "TEMP");
            try
            {
                // Open the input PDF file.
                PdfReader reader = new PdfReader(filename);
                PdfWriter writer = new PdfWriter(newfilename);
                PdfDocument pdfDoc = new PdfDocument(reader, writer);

                // Iterate through each page of the input PDF.
                for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
                {
                    PdfPage page = pdfDoc.GetPage(pageNum);
                    var obj = page.GetPdfObject();
                    var resources = obj.GetAsDictionary(PdfName.Resources);
                    PdfDictionary asDictionary = resources.GetAsDictionary(PdfName.XObject);
                    if (asDictionary == null)
                    {
                        continue;
                    }
                    foreach (PdfObject item in asDictionary.Values())
                    {
                        if (item.IsStream())
                        {
                            PdfStream pdfStream = (PdfStream)item;
                            if (pdfStream.ContainsKey(PdfName.Interpolate))
                            {
                                pdfStream.Remove(PdfName.Interpolate);
                                editedPages.Add(pageNum);
                            }
                        }
                    }
                }
                // Close the PDF document.
                pdfDoc.Close();
            }
            catch (Exception e)
            {
                // Handle any exceptions during processing.
                Console.WriteLine($"Unable to process file: {filename}. Exception: {e}");
            }
            if(editedPages.Count > 0)
            { 
                Logger.Instance.SetUpRunTimeLogMessage("Interpolation removed from PDF to comply with PDF/A standards on page(s) " + editedPages.Distinct().ToList(), true, filename: filename);
            }
            return newfilename;
        }

        /// <summary>
        /// Convert from any pdf format to another pdf format. This includes PDF to PDF-A, but not PDF-A to PDF.
        /// </summary>
        /// <param name="file">The file to convert</param>
        public void convertFromPDFToPDF(FileToConvert file)
        {
            try
            {
                PdfVersion pdfVersion = GetPDFVersion(file.Route.First());
                PdfAConformanceLevel? conformanceLevel = GetPdfAConformanceLevel(file.Route.First());
                if (conformanceLevel != null)
                {
                    convertFromPDFToPDFA(file, conformanceLevel);
                    return;
                }

                string tmpFilename = Path.Combine(Path.GetDirectoryName(file.FilePath) ?? "", Path.GetFileNameWithoutExtension(file.FilePath) + "_TEMP.pdf");
                string filename = Path.Combine(file.FilePath);
                int count = 0;
                bool converted = false;
                do
                {
                    using (PdfWriter writer = new PdfWriter(tmpFilename, new WriterProperties().SetPdfVersion(pdfVersion))) // Create PdfWriter instance
                    using (PdfDocument pdfDocument = new PdfDocument(writer))
                    using (PdfReader reader = new PdfReader(filename))
                    {
                        PdfDocument sourceDoc = new PdfDocument(reader);
                        pdfDocument.SetTagged();
                        for (int pageNum = 1; pageNum <= sourceDoc.GetNumberOfPages(); pageNum++)
                        {
                            PdfPage sourcePage = sourceDoc.GetPage(pageNum);
                            PdfPage page = pdfDocument.AddNewPage(new PageSize(sourcePage.GetPageSize()));
                            PdfFormXObject pageCopy = sourcePage.CopyAsFormXObject(pdfDocument);
                            PdfCanvas canvas = new PdfCanvas(page);
                            canvas.AddXObject(pageCopy);
                        }
                    }
                    converted = CheckConversionStatus(tmpFilename, file.TargetPronom);
                } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
                if (!converted)
                {
                    file.Failed = true;
                }
                else
                {
                    //Remove old file and replace with new
                    File.Delete(filename);
                    File.Move(tmpFilename, filename);
                    //Rename reference in filemanager
                    ReplaceFileInList(filename, file);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error converting PDF to PDF/A or PDF. File is not converted: " + e.Message, true, filename: file.FilePath);
            }
        }

        /// <summary>
        /// Merge files into one PDF
        /// </summary>
        /// <param name="files">List of files that should be nerged</param>
        /// <param name="pronom">The file format to convert to</param>
        public override void CombineFiles(List<FileInfo2> files, string pronom)
        {
            if (files == null || files.Count == 0)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Files sent to iText7 to be combined, but no files found.", true);
                return;
            }
            try
            {
                //Set base output file name to YYYY-MM-DD
                DateTime currentDateTime = DateTime.Now;
                string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd");
                string baseName = Path.GetDirectoryName(files.First().FilePath)!.Split('\\').Last() ?? "combined";
                string outputDirectory = Path.GetDirectoryName(files.First().FilePath) ?? GlobalVariables.ParsedOptions.Output;
                string filename = Path.Combine(outputDirectory, baseName + "_" + formattedDateTime);

                List<FileInfo2> group = new List<FileInfo2>();
                List<string> sentPaths = new List<string>();
                long groupSize = 0;
                int groupCount = 1;
                foreach (var file in files)
                {
                    string outputFileName = $@"{filename}_{groupCount}.pdf";
                    file.NewFileName = outputFileName;
                    if (sentPaths.Contains(file.FilePath))
                    {
                        continue;
                    }
                    group.Add(file);
                    sentPaths.Add(file.FilePath);
                    groupSize += file.OriginalSize;
                    if (groupSize > GlobalVariables.MaxFileSize)
                    {
                        Task.Run(() => MergeFilesToPDF(group, outputFileName, pronom).Wait());
                        group.Clear();
                        groupSize = 0;
                        groupCount++;
                    }
                }
                if (group.Count > 0)
                {
                    string outputFileName = $@"{filename}_{groupCount}.pdf";
                    Task.Run(() => MergeFilesToPDF(group, outputFileName, pronom).Wait());
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error combining files with iText7. Error message: " + e.Message, true, files.First().OriginalPronom, Path.GetDirectoryName(files.First().FilePath) ?? files.First().FilePath);
            }
        }

        /// <summary>
        /// Merge several image files into one pdf
        /// </summary>
        /// <param name="files"></param>
        /// <param name="outputFileName"></param>
        Task MergeFilesToPDF(List<FileInfo2> files, string outputFileName, string pronom)
        {
            try
            {
                PdfVersion pdfVersion = GetPDFVersion(pronom);

                PdfAConformanceLevel? conformanceLevel = GetPdfAConformanceLevel(pronom);

                using (var pdfWriter = new PdfWriter(outputFileName, new WriterProperties().SetPdfVersion(pdfVersion)))
                using (var pdfDocument = new PdfDocument(pdfWriter))
                using (var document = new iText.Layout.Document(pdfDocument))
                {
                    pdfDocument.SetTagged();
                    foreach (var file in files)
                    {
                        bool isWrapped = file.FilePath.StartsWith('"') && file.FilePath.EndsWith('"');
                        string filename = isWrapped ? file.FilePath : String.Format("\"{0}\"",file.FilePath);
                        if (!File.Exists(filename))
                        {
                            Logger.Instance.SetUpRunTimeLogMessage("MergeFiles - File not found: " + file.FilePath, true);
                            continue;
                        }
                        var filestream = File.ReadAllBytes(filename);
                        var imageData = ImageDataFactory.Create(filestream, false);
                        iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData);
                        document.Add(image);
                    }
                }

                foreach (var file in files)
                {
                    string filename = Path.Combine(file.FilePath);
                    DeleteOriginalFileFromOutputDirectory(filename);
                    file.IsMerged = true;
                    file.NewFileName = outputFileName;
                }

                FileToConvert ftc = new FileToConvert(outputFileName, Guid.NewGuid(), pronom);

                if (conformanceLevel != null)
                {
                    convertFromPDFToPDFA(ftc, conformanceLevel);
                }

                var result = Siegfried.Instance.IdentifyFile(outputFileName, false);
                if (result != null)
                {
                    FileInfo2 newFileInfo = new FileInfo2(result);
                    newFileInfo.Id = Guid.NewGuid();
                    newFileInfo.IsMerged = pronom == result.matches[0].id;
                    newFileInfo.ShouldMerge = true;
                    newFileInfo.AddConversionTool(NameAndVersion);
                    FileManager.Instance.Files.TryAdd(newFileInfo.Id, newFileInfo);
                }
                else
                {
                    Logger.Instance.SetUpRunTimeLogMessage("iText7 CombineFiles: Result could not be identified: " + outputFileName, true);
                }
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("Error combining files to PDF. Files are not combined: " + e.Message, true, pronom, outputFileName);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Returns the PDFVersion based on the pronom
        /// </summary>
        /// <param name="pronom">PRONOM PUID that will be checked</param>
        /// <returns>PdfVersion associated with PRONOM if found in map, default is PDF_1_7</returns>
        static PdfVersion GetPDFVersion(string pronom)
        {
            if (PronomToPdfVersion.TryGetValue(pronom, out var pdfVersion))
            {
                return pdfVersion;
            }
            else
            {
                return PdfVersion.PDF_1_7;
            }
        }

        /// <summary>
        /// Returns the PDF-A conformance level based on the pronom
        /// </summary>
        /// <param name="pronom">PRONOM PUID that will be checked</param>
        /// <returns>PdfAConformanceLevel based on input pronom if found in map, otherwise null</returns>
        static PdfAConformanceLevel? GetPdfAConformanceLevel(string pronom)
        {
            if (PronomToPdfAConformanceLevel.TryGetValue(pronom, out var pdfAConformanceLevel))
            {
                return pdfAConformanceLevel;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the conformance level and PdfVersion to PDF/A Basic for the given PDF/A version (ie. PDF/A 2A -> PDF/A 2B)
        /// </summary>
        /// <param name="pronom">Pronom for previous PDF/A version</param>
        /// <param name="conformanceLevel">PDF/A conformance level that will be set in the method</param>
        /// <param name="pdfVersion">PDF version that will be set in the method</param>
        /// <returns>The new PRONOM PUID</returns>
        static string SetToPDFABasic(string pronom, out PdfAConformanceLevel conformanceLevel, out PdfVersion pdfVersion)
        {
            switch (pronom)
            {
                case "fmt/95":
                case "fmt/354":
                    conformanceLevel = PdfAConformanceLevel.PDF_A_1B;
                    pronom = "fmt/354";
                    pdfVersion = PdfVersion.PDF_1_4;
                    break;
                case "fmt/479":
                case "fmt/480":
                case "fmt/481":
                    conformanceLevel = PdfAConformanceLevel.PDF_A_3B;
                    pronom = "fmt/480";
                    pdfVersion = PdfVersion.PDF_1_7;
                    break;
                case "fmt/1910":
                case "fmt/1911":
                case "fmt/1912":
                    conformanceLevel = PdfAConformanceLevel.PDF_A_4F;
                    pronom = "fmt/1912";
                    pdfVersion = PdfVersion.PDF_2_0;
                    break;
                default:
                    conformanceLevel = PdfAConformanceLevel.PDF_A_2B;
                    pronom = "fmt/477";
                    pdfVersion = PdfVersion.PDF_1_7;
                    break;
            }
            return pronom;
        }

        readonly List<string> ImagePronoms = [
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
        readonly List<string> HTMLPronoms = [
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
        readonly List<string> PDFPronoms = [
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

        readonly List<string> PDFAPronoms = [
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

        readonly List<string> PDFAAPronoms = [
            "fmt/95",       // PDF/A 1A
            "fmt/476",      // PDF/A 2A
            "fmt/479",      // PDF/A 3A
        ];

        /// <summary>
        /// Maps a string pronom to the corresponding iText7 class PdfVersion
        /// NOTE: All PDF-A PRONOMS should be mapped to PDF 2.0
        /// </summary>
        static Dictionary<String, PdfVersion> PronomToPdfVersion = new Dictionary<string, PdfVersion>()
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

        /// <summary>
        /// Maps a string pronom to the corresponding iText7 class PdfAConformanceLevel
        /// </summary>
        static public readonly ImmutableDictionary<string, PdfAConformanceLevel> PronomToPdfAConformanceLevel = ImmutableDictionary<string, PdfAConformanceLevel>.Empty
            .Add("fmt/95", PdfAConformanceLevel.PDF_A_1A)
            .Add("fmt/354", PdfAConformanceLevel.PDF_A_1B)
            .Add("fmt/476", PdfAConformanceLevel.PDF_A_2A)
            .Add("fmt/477", PdfAConformanceLevel.PDF_A_2B)
            .Add("fmt/478", PdfAConformanceLevel.PDF_A_2U)
            .Add("fmt/479", PdfAConformanceLevel.PDF_A_3A)
            .Add("fmt/480", PdfAConformanceLevel.PDF_A_3B)
            .Add("fmt/481", PdfAConformanceLevel.PDF_A_3U)
            .Add("fmt/1910", PdfAConformanceLevel.PDF_A_4)
            .Add("fmt/1911", PdfAConformanceLevel.PDF_A_4E)
            .Add("fmt/1912", PdfAConformanceLevel.PDF_A_4F);
        
        /*static readonly public Dictionary<String, PdfAConformanceLevel> PronomToPdfAConformanceLevel = new Dictionary<string, PdfAConformanceLevel>()
        {
            {"fmt/95",  PdfAConformanceLevel.PDF_A_1A },
            {"fmt/354", PdfAConformanceLevel.PDF_A_1B },
            {"fmt/476", PdfAConformanceLevel.PDF_A_2A },
            {"fmt/477", PdfAConformanceLevel.PDF_A_2B },
            {"fmt/478", PdfAConformanceLevel.PDF_A_2U },
            {"fmt/479", PdfAConformanceLevel.PDF_A_3A },
            {"fmt/480", PdfAConformanceLevel.PDF_A_3B },
            {"fmt/481", PdfAConformanceLevel.PDF_A_3U },
            {"fmt/1910", PdfAConformanceLevel.PDF_A_4 },
            {"fmt/1911", PdfAConformanceLevel.PDF_A_4E},
            {"fmt/1912", PdfAConformanceLevel.PDF_A_4F}
        };*/
    }
}
