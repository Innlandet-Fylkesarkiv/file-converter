using iText.Kernel.Pdf;
using iText.IO.Image;
using iText.Pdfa;
using iText.Html2pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas;
using iText.Commons.Actions;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using System.Diagnostics;

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
public class iText7 : Converter
{
	private static readonly object pdfalock = new object();     //PDF-A uses a .icc file when converting, which can not be accessed by multiple threads at the same time

    public iText7()
	{
		Name = "iText7";
        SetNameAndVersion();
		SupportedConversions = GetListOfSupportedConvesions();
        SupportedOperatingSystems = GetSupportedOS();
        BlockingConversions = GetListOfBlockingConversions();

        DependeciesExists = true;   // Bundled with program 
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
        foreach(string pronom in ImagePronoms.Concat(HTMLPronoms).Concat(PDFPronoms))
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
    async public override Task ConvertFile(FileToConvert file)
    {
		try
		{
			PdfVersion? pdfVersion = null;
			PdfAConformanceLevel? conformanceLevel = null;
			if (PronomToPdfVersion.ContainsKey(file.TargetPronom))
			{
				pdfVersion = PronomToPdfVersion[file.TargetPronom];
			}
			if (PronomToPdfAConformanceLevel.ContainsKey(file.TargetPronom))
			{
				conformanceLevel = PronomToPdfAConformanceLevel[file.TargetPronom];
			}
           
			if (HTMLPronoms.Contains(file.CurrentPronom))
			{
				convertFromHTMLToPDF(file, pdfVersion ?? PdfVersion.PDF_2_0, conformanceLevel);
			}
			else if (PDFPronoms.Contains(file.CurrentPronom))
			{
				convertFromPDFToPDF(file);
			}
			else if (ImagePronoms.Contains(file.CurrentPronom))
			{
				convertFromImageToPDF(file, pdfVersion ?? PdfVersion.PDF_2_0, conformanceLevel);
			}
        }
        catch(Exception e)
		{
		    Logger.Instance.SetUpRunTimeLogMessage("Error converting file with iText7. Error message: " + e.Message, true, filename: file.FilePath);
		}
    }

    /// <summary>
    /// Get the filepath of the ICC file needed for PDF-A conversion
    /// </summary>
    /// <returns>A string with the full path</returns>
    string GetICCFilePath()
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
                    PdfDocumentInfo info = pdfDocument.GetDocumentInfo();

                    var imageData = ImageDataFactory.Create(filestream,false);
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
				    PdfDocumentInfo info = pdfDocument.GetDocumentInfo();
				    using(var htmlSource = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				    {
					    HtmlConverter.ConvertToPdf(htmlSource, pdfDocument); //TODO: System.UriFormatException: 'Invalid URI: The URI is empty'
					    document.Close();
				    }
				    pdfDocument.Close();
				    pdfWriter.Close();
				    pdfWriter.Dispose();
			    }

                if (conformanceLevel != null)
                {
                    convertFromPDFToPDFA(new FileToConvert(output,file.Id, file.Route.First()), conformanceLevel);
                }
                converted = CheckConversionStatus(output, file.TargetPronom, file);
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
    /// Convert from any pdf file to pdf-A version 1A-3B
    /// </summary>
    /// <param name="file">The file to convert</param>
    /// <param name="conformanceLevel">The type of PDF-A to convert to</param>
    void convertFromPDFToPDFA(FileToConvert file, PdfAConformanceLevel conformanceLevel)
    {
        try
        {
            string tmpFilename = Path.Combine(Path.GetDirectoryName(file.FilePath) ?? "", Path.GetFileNameWithoutExtension(file.FilePath) + "_PDFA.pdf");
            string filename = Path.Combine(file.FilePath);
            int count = 0;
            bool converted = false;
            PdfOutputIntent outputIntent;

            //RemoveInterpolation(filename);
           
            do
            {
                // Initialize PdfOutputIntent outside the loop
                lock (pdfalock)
                {
                    using (FileStream iccFileStream = new FileStream(GetICCFilePath(), FileMode.Open))
                    {
                        outputIntent = new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccFileStream);
                    }
                }
                
                using (PdfWriter writer = new PdfWriter(tmpFilename)) // Create PdfWriter instance
                using (PdfADocument pdfADocument = new PdfADocument(writer, conformanceLevel, outputIntent))    // Associate PdfADocument with PdfWriter
                using (PdfReader reader = new PdfReader(filename))
                {
                    PdfDocument pdfDocument = new PdfDocument(reader);
                    pdfADocument.SetTagged();

                    for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                    {
                        PdfPage sourcePage = pdfDocument.GetPage(pageNum);
                        var ps = sourcePage.GetPageSize();
                        PdfPage page = pdfADocument.AddNewPage(new PageSize(sourcePage.GetPageSize()));
                        PdfFormXObject pageCopy = sourcePage.CopyAsFormXObject(pdfADocument);

                        PdfCanvas canvas = new PdfCanvas(page);
                        canvas.AddXObject(pageCopy);
                    }
                }
                converted = CheckConversionStatus(tmpFilename, file.Route.First());
            } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
            if (!converted)
            {
                file.Failed = true;
            }
            else
            {
                File.Delete(filename);
                File.Move(tmpFilename, filename);
                ReplaceFileInList(filename, file);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error converting PDF to PDF-A. File is not converted: " + e.Message, true, filename: file.FilePath);
        }
    }

    /// <summary>
    /// Remove interpolation 
    /// </summary>
    /// <param name="filename">Name of the file</param>
    void RemoveInterpolation(string filename)
    {
        using (PdfReader reader = new PdfReader(filename))
        {
            PdfDocument pdfDocument = new PdfDocument(reader);
            for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
            {
                PdfPage page = pdfDocument.GetPage(pageNum);
                RemoveInterpolationFromResources(page.GetPdfObject());
            }

            // Save the modified document
            pdfDocument.Close();
        }
    }

    /// <summary>
    /// Remove interpolation from resources
    /// </summary>
    /// <param name="resource">The specific PDF document</param>
    void RemoveInterpolationFromResources(PdfObject resource)
    {
        if (resource is PdfDictionary resources)
        {
            PdfDictionary xobjs = resources.GetAsDictionary(PdfName.XObject);

            if (xobjs != null)
            {
                foreach (PdfName name in xobjs.KeySet())
                {
                    PdfStream xobjStream = xobjs.GetAsStream(name);

                    if (PdfName.Form.Equals(xobjStream.GetAsName(PdfName.Subtype)))
                    {
                        // XObject forms have their own nested resources
                        PdfDictionary nestedResources = xobjStream.GetAsDictionary(PdfName.Resources);
                        RemoveInterpolationFromResources(nestedResources);
                    }
                    else
                    {
                        // Remove the interpolate flag
                        xobjStream.Remove(PdfName.Interpolate);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert from any pdf format to another pdf format. This includes PDF to PDF-A, but not PDF-A to PDF.
    /// </summary>
    /// <param name="file">The file to convert</param>
    public void convertFromPDFToPDF(FileToConvert file)
    {
        try
        {
            PdfVersion? pdfVersion;
            if(PronomToPdfVersion.ContainsKey(file.TargetPronom))
            {
                pdfVersion = PronomToPdfVersion[file.TargetPronom];
            }
            else
            {
                Logger.Instance.SetUpRunTimeLogMessage("PDF pronom not found in dictionary. Using default PDF version 2.0", true, file.TargetPronom, file.FilePath);
                pdfVersion = PdfVersion.PDF_2_0; //Default PDF version
            }
            if(PronomToPdfAConformanceLevel.ContainsKey(file.TargetPronom))
            {
                convertFromPDFToPDFA(file, PronomToPdfAConformanceLevel[file.TargetPronom]);
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
                        var ps = sourcePage.GetPageSize();
                        var landscape = ps.GetWidth() > ps.GetHeight();
                        if (landscape)
                        {
                            //Console.WriteLine("Landscape");
                        }

                        PdfPage page = pdfDocument.AddNewPage(new iText.Kernel.Geom.PageSize(sourcePage.GetPageSize()));
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
    public override void CombineFiles(List<FileInfo> files, string pronom)
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
            string outputDirectory = Path.GetDirectoryName(files.First().FilePath) ?? GlobalVariables.parsedOptions.Output;
            string filename = Path.Combine(outputDirectory, baseName + "_" + formattedDateTime);

            List<Task> tasks = new List<Task>();
            List<FileInfo> group = new List<FileInfo>();
            long groupSize = 0;
            int groupCount = 1;
            foreach (var file in files)
            {
                string outputFileName = $@"{filename}_{groupCount}.pdf";
                file.NewFileName = outputFileName;
                group.Add(file);
                groupSize += file.OriginalSize;
                if (groupSize > GlobalVariables.maxFileSize)
                {
                    Task.Run(() => MergeFilesToPDF(group, outputFileName, pronom).Wait()); 
                    group.Clear();
                    groupSize = 0;
                    groupCount++;
                }
            }
            if(group.Count > 0)
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
    Task MergeFilesToPDF(List<FileInfo> files, string outputFileName, string pronom)
     {
        try
        {
            PdfVersion? pdfVersion = PronomToPdfVersion[pronom];
            if (pdfVersion == null)
            {
                pdfVersion = PdfVersion.PDF_2_0; //Default PDF version
            }

            PdfAConformanceLevel? conformanceLevel = null;
            if (PronomToPdfAConformanceLevel.ContainsKey(pronom))
            {
                conformanceLevel = PronomToPdfAConformanceLevel[pronom];
            }

            using (var pdfWriter = new PdfWriter(outputFileName, new WriterProperties().SetPdfVersion(pdfVersion)))
            using (var pdfDocument = new PdfDocument(pdfWriter))
            using (var document = new iText.Layout.Document(pdfDocument))
            {
                pdfDocument.SetTagged();                                
                PdfDocumentInfo info = pdfDocument.GetDocumentInfo();   // Set the document's metadata
                foreach (var file in files)
                {
                    string filename = Path.Combine(file.FilePath);
                    var filestream = File.ReadAllBytes(filename);
                    var imageData = ImageDataFactory.Create(filestream, false);
                    iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData); //TODO: System.UriFormatException: 'Invalid URI: The format of the URI could not be determined.'
                                                                                                    //"output\\MergeFiles\\Norwegian_Flag\\Norwegian_flag (3339).jpg"
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

            FileToConvert ftc = new FileToConvert(outputFileName, new Guid(), pronom);

            if (conformanceLevel != null)
            {
                convertFromPDFToPDFA(ftc, conformanceLevel);
            }

            var result = Siegfried.Instance.IdentifyFile(outputFileName, false);
            if (result != null)
            {
                FileInfo newFileInfo = new FileInfo(result);
                newFileInfo.Id = new Guid();
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

    List<string> ImagePronoms = [
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
    List<string> HTMLPronoms = [
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
    List<string> PDFPronoms = [
        "fmt/95",       // PDF/A 1A
        "fmt/354",      // PDF/A 1B
        "fmt/476",      // PDF/A 2A
        "fmt/477",      // PDF/A 2B
        "fmt/478",      // PDF/A 2U
        "fmt/479",      // PDF/A 3A
        "fmt/480",      // PDF/A 3B
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

    List<string> PDFAPronoms = [
         "fmt/95",       // PDF/A 1A
        "fmt/354",      // PDF/A 1B
        "fmt/476",      // PDF/A 2A
        "fmt/477",      // PDF/A 2B
        "fmt/478",      // PDF/A 2U
        "fmt/479",      // PDF/A 3A
        "fmt/480",      // PDF/A 3B
    ];

    /// <summary>
    /// Maps a string pronom to the corresponding iText7 class PdfVersion
    /// NOTE: All PDF-A PRONOMS should be mapped to PDF 2.0
    /// </summary>
    Dictionary<String, PdfVersion> PronomToPdfVersion = new Dictionary<string, PdfVersion>()
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
        {"fmt/95", PdfVersion.PDF_2_0 },
        {"fmt/354", PdfVersion.PDF_2_0 },
        {"fmt/476", PdfVersion.PDF_2_0 },
        {"fmt/477", PdfVersion.PDF_2_0 },
        {"fmt/478", PdfVersion.PDF_2_0 },
        {"fmt/479", PdfVersion.PDF_2_0 },
        {"fmt/480", PdfVersion.PDF_2_0 }
    };

    /// <summary>
    /// Maps a string pronom to the corresponding iText7 class PdfAConformanceLevel
    /// </summary>
    public Dictionary<String, PdfAConformanceLevel> PronomToPdfAConformanceLevel = new Dictionary<string, PdfAConformanceLevel>()
    {
        {"fmt/95", PdfAConformanceLevel.PDF_A_1A },
        {"fmt/354", PdfAConformanceLevel.PDF_A_1B },
        {"fmt/476", PdfAConformanceLevel.PDF_A_2A },
        {"fmt/477", PdfAConformanceLevel.PDF_A_2B },
        {"fmt/478", PdfAConformanceLevel.PDF_A_2U },
        {"fmt/479", PdfAConformanceLevel.PDF_A_3A },
        {"fmt/480", PdfAConformanceLevel.PDF_A_3B }
    };
}