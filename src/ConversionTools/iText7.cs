﻿using iText.Kernel.Pdf;
using iText.IO.Image;
using iText.Pdfa;
using iText.Html2pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using System.Drawing;
using System.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;

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

	private static readonly object padlock = new object();

    public iText7()
	{
		Name = "iText7";
		Version = "8.0.2";
		SupportedConversions = getListOfSupportedConvesions();
        SupportedOperatingSystems = getSupportedOS();
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
        "fmt/95",
        "fmt/354",
        "fmt/476",
        "fmt/477",
        "fmt/478",
        "fmt/479",
        "fmt/480",
        "fmt/14",
        "fmt/15",
        "fmt/16",
        "fmt/17",
        "fmt/18",
        "fmt/19",
        "fmt/20",
        "fmt/276",
        "fmt/1129"
    ];

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

    Dictionary<String, PdfAConformanceLevel> PronomToPdfAConformanceLevel = new Dictionary<string, PdfAConformanceLevel>()
    {
        {"fmt/95", PdfAConformanceLevel.PDF_A_1A },
        {"fmt/354", PdfAConformanceLevel.PDF_A_1B },
        {"fmt/476", PdfAConformanceLevel.PDF_A_2A },
        {"fmt/477", PdfAConformanceLevel.PDF_A_2B },
        {"fmt/478", PdfAConformanceLevel.PDF_A_2U },
        {"fmt/479", PdfAConformanceLevel.PDF_A_3A },
        {"fmt/480", PdfAConformanceLevel.PDF_A_3B }
    };

    /// <summary>
    /// Reference list stating supported conversions containing key value pairs with string input pronom and string output pronom
    /// </summary>
    /// <returns>List of all conversions</returns>
    public override Dictionary<string, List<string>> getListOfSupportedConvesions()
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

    public override List<string> getSupportedOS()
    {
        var supportedOS = new List<string>();
        supportedOS.Add(PlatformID.Win32NT.ToString());
        //Add more supported OS here
        return supportedOS;
    }

    /// <summary>
    /// Convert a file to a new format
    /// </summary>
    /// <param name="fileinfo">The file to be converted</param>
    /// <param name="pronom">The file format to convert to</param>
    public override void ConvertFile(FileToConvert file, string pronom)
    {
		try
		{
			PdfVersion? pdfVersion = null;
			PdfAConformanceLevel? conformanceLevel = null;
			if (PronomToPdfVersion.ContainsKey(pronom))
			{
				pdfVersion = PronomToPdfVersion[pronom];
			}
			if (PronomToPdfAConformanceLevel.ContainsKey(pronom))
			{
				conformanceLevel = PronomToPdfAConformanceLevel[pronom];
			}
			string extension = Path.GetExtension(file.FilePath).ToLower();
			if (extension == ".html" || extension == ".htm")
			{
				convertFromHTMLToPDF(file, pdfVersion ?? PdfVersion.PDF_1_2, conformanceLevel != null, pronom, conformanceLevel);
			}
			else if (extension == ".pdf")
			{
				if (conformanceLevel != null)
				{
					convertFromPDFToPDFA(file, conformanceLevel, pronom);
				}
			}
			else if (extension == ".jpg" || extension == ".png" || extension == ".gif" || extension == ".tiff" || extension == ".bmp")
			{
				convertFromImageToPDF(file, pdfVersion ?? PdfVersion.PDF_2_0, pronom, conformanceLevel);
			}
        }
        catch(Exception e)
		{
		    Logger.Instance.SetUpRunTimeLogMessage("Error converting file with iText7. Error message: " + e.Message, true, filename: file.FilePath);
            throw;
		}
    }

	/// <summary>
	/// Convert from any image file to pdf version 1.0-2.0
	/// </summary>
	/// <param name="filePath">The file being converted</param>
	/// <param name="pdfVersion">What pdf version it is being converted to</param>
	/// <param name="conformanceLevel"></param>
	/// <param name="pronom">The file format to convert to</param>
	void convertFromImageToPDF(FileToConvert file, PdfVersion pdfVersion, string pronom, PdfAConformanceLevel? conformanceLevel = null) {
	
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
			        iText.Layout.Element.Image image = new iText.Layout.Element.Image(ImageDataFactory.Create(filename));
			        document.Add(image);
		        }
		        if (conformanceLevel != null)
		        {
			        convertFromPDFToPDFA(new FileToConvert(output, file.Id), conformanceLevel, file.FilePath);
		        }
			
			    converted = CheckConversionStatus(output, pronom, file);
			} while (!converted && ++count < 3);
			if (!converted)
			{
				throw new Exception("File was not converted");
			}
		}
		catch (Exception e)
		{
			Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF. File is not converted: " + e.Message, true, filename: file.FilePath);
			throw;
		}
		
	}

	/// <summary>
	/// Convert from any html file to pdf 1.0-2.0
	/// </summary>
	/// <param name="filePath">Name of the file to be converted</param>
	/// <param name="pdfVersion">Specific pdf version to be converted to</param>
	void convertFromHTMLToPDF(FileToConvert file, PdfVersion pdfVersion, bool pdfA, string pronom, PdfAConformanceLevel? conformanceLevel = null)
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
					    HtmlConverter.ConvertToPdf(htmlSource, pdfDocument);
					    document.Close();
				    }
				    pdfDocument.Close();
				    pdfWriter.Close();
				    pdfWriter.Dispose();
			    }

                if (conformanceLevel != null)
                {
                    convertFromPDFToPDFA(new FileToConvert(output,file.Id), conformanceLevel, filename);
                }
                converted = CheckConversionStatus(output, pronom, file);
            } while (!converted && ++count < 3);
            if (!converted)
            {
                throw new Exception("File was not converted");
            }
            else
            {
                deleteOriginalFileFromOutputDirectory(file.FilePath);
            }

        }
		catch (Exception e)
		{
			Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF. File is not converted: " + e.Message, true, filename: file.FilePath);
            throw;
		}

	}

    /// <summary>
    /// Convert from any pdf file to pdf-A version 1A-3B
    /// </summary>
    /// <param name="filePath">The filename to convert</param>
    /// <param name="conformanceLevel">The type of PDF-A to convert to</param>
    /// <param name="originalFile">Original file that should be deleted</param>
    void convertFromPDFToPDFA(FileToConvert file, PdfAConformanceLevel conformanceLevel, string pronom, string? originalFile = null)
    {
        try
        {
            string newFileName = Path.Combine(Path.GetDirectoryName(file.FilePath) ?? "", Path.GetFileNameWithoutExtension(file.FilePath) + "_PDFA.pdf");
            string filename = Path.Combine(file.FilePath);
            int count = 0;
            bool converted = false;
            do
            {

                PdfOutputIntent outputIntent;
                lock (padlock)
                {
                    using (FileStream iccFileStream = new FileStream("src/ConversionTools/sRGB2014.icc", FileMode.Open))
                    {
                        outputIntent = new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccFileStream);
                    }
                }

                using (PdfWriter writer = new PdfWriter(newFileName)) // Create PdfWriter instance
                using (PdfADocument pdfADocument = new PdfADocument(writer, conformanceLevel, outputIntent)) // Associate PdfADocument with PdfWriter
                using (PdfReader reader = new PdfReader(filename))
                {
                    
                    PdfDocument pdfDocument = new PdfDocument(reader);
                    
			        pdfADocument.SetTagged();

                    for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                    {
                        PdfPage sourcePage = pdfDocument.GetPage(pageNum);
                        var ps = sourcePage.GetPageSize();
                        var landscape = ps.GetWidth() > ps.GetHeight();
                        if (landscape)
                        {
                            Console.WriteLine("Landscape");
                        }

                        PdfPage page = pdfADocument.AddNewPage(new iText.Kernel.Geom.PageSize(sourcePage.GetPageSize()));
                        PdfFormXObject pageCopy = sourcePage.CopyAsFormXObject(pdfADocument);

                        PdfCanvas canvas = new PdfCanvas(page);
                        canvas.AddXObject(pageCopy);
                        
                    }

                    // Close the PDF documents
                    pdfDocument.Close();
                }
                converted = CheckConversionStatus(newFileName, pronom, file);
            } while (!converted && ++count < 3);
            if (!converted)
            {
                throw new Exception("File was not converted");
            } else
            {
                File.Delete(filename);
                File.Move(newFileName, filename);
                replaceFileInList(newFileName, file);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF-A. File is not converted: " + e.Message, true, filename: file.FilePath);
            throw;
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
            string baseName = Path.GetDirectoryName(files.First().FilePath).Split('\\').Last() ?? "combined";
            string outputDirectory = Path.GetDirectoryName(files.First().FilePath) ?? GlobalVariables.parsedOptions.Output;
            string filename = Path.Combine(outputDirectory, baseName + "_" + formattedDateTime);

            List<Task> tasks = new List<Task>();
            List<FileInfo> group = new List<FileInfo>();
            long groupSize = 0;
            int groupCount = 1;
            foreach (var file in files)
            {
                group.Add(file);
                groupSize += file.OriginalSize;
                if (groupSize > GlobalVariables.maxFileSize)
                {
                    string outputFileName = $@"{filename}_{groupCount}.pdf";
                    tasks.Add(Task.Run(() => MergeFilesToPDF(group, outputFileName, pronom)));
                    group.Clear();
                    groupSize = 0;
                    groupCount++;
                }
            }
            if(group.Count > 0)
            {
                string outputFileName = $@"{filename}_{groupCount}.pdf";
                tasks.Add(Task.Run(() => MergeFilesToPDF(group, outputFileName, pronom)));
            }
            //Wait for all files 
            tasks.ForEach(t => t.Wait());
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error combining files with iText7. Error message: " + e.Message, true, pronom, Path.GetDirectoryName(files.First().FilePath) ?? files.First().FilePath);
        }
    }

    /// <summary>
    /// Merge several image files into one pdf
    /// </summary>
    /// <param name="files"></param>
    /// <param name="outputFileName"></param>
    /// <param name="pronom"></param>
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
                    iText.Layout.Element.Image image = new iText.Layout.Element.Image(ImageDataFactory.Create(filename));
                    document.Add(image);
                }
            }
            
            foreach (var file in files)
            {
                string filename = Path.Combine(file.FilePath);
                deleteOriginalFileFromOutputDirectory(filename);
                file.IsMerged = true;
            }

            FileToConvert ftc = new FileToConvert(outputFileName, new Guid());

            if (conformanceLevel != null)
            {
                convertFromPDFToPDFA(ftc, conformanceLevel, pronom);
            }

            var result = Siegfried.Instance.IdentifyFile(outputFileName, false);
            if (result != null)
            {
                FileInfo fi = new FileInfo(result);
                fi.IsMerged = true;
                fi.ShouldMerge = true;
                fi.AddConversionTool(Name);
                FileManager.Instance.Files.TryAdd(fi.Id, fi);
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
}