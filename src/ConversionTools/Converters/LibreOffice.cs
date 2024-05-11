using System.Diagnostics;
using FileConverter.Managers;
using FileConverter.HelperClasses;


/// <summary>
/// Libreoffice supports the following conversions for both Linux and Windows:
/// 
/// - Word (DOC, DOCX, DOCM, DOTX) to PDF
/// - PowerPoint (PPT, PPTX, PPTM, POTX) to PDF
/// - Excel (XLS, XLSX, XLSM, XLTX) to PDF
/// - CSV to PDF
/// - RTF to PDF
/// - OpenDocument (ODT, ODS, ODP) to PDF
/// 
/// - Format it converts to:
/// PDF - fmt/276 (Windows), fmt/20 (Linux)  
/// ODT - fmt/1756
/// 
/// 
/// </summary>
/// 
namespace ConversionTools.Converters
{
    public class LibreOfficeConverter : Converter
	{
		readonly Logger log = Logger.Instance;
		private static readonly object locker = new object();
		readonly OperatingSystem currentOS;
		bool iTextFound = false;
		readonly string sofficePathLinux = "/usr/lib/libreoffice/program/soffice";
		readonly string sofficePathWindows = "C:\\Program Files\\LibreOffice\\program\\soffice.exe";

		/// <summary>
		/// Constructor setting important properties for the class.
		/// </summary>
		public LibreOfficeConverter()
		{
			Name = "Libreoffice";
			SetNameAndVersion();
			SupportedConversions = GetListOfSupportedConvesions();
			SupportedOperatingSystems = GetSupportedOS();
			currentOS = Environment.OSVersion;
			DependenciesExists = currentOS.Platform == PlatformID.Unix ? CheckPathVariableLinux("soffice")
																	: CheckPathVariableWindows("soffice.exe");
			BlockingConversions = GetListOfBlockingConversions();
		}

		/// <summary>
		/// Gets the version of LibreOffice
		/// </summary>
		public override void GetVersion()
		{
			Version = "Unable to fetch version"; // Default version in case retrieval fails

			try
			{
				string sofficePath = Environment.OSVersion.Platform == PlatformID.Unix ? sofficePathLinux : sofficePathWindows;
				if (!string.IsNullOrEmpty(sofficePath))
				{
					FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(sofficePath);
					if (fileVersionInfo != null)
					{
						Version = fileVersionInfo.ProductVersion ?? "Version not found";
					}
				}
				else
				{
					Logger.Instance.SetUpRunTimeLogMessage("LibreOffice path is empty or invalid", true);
				}
			}
			catch (Exception ex)
			{
				Logger.Instance.SetUpRunTimeLogMessage("Error getting LibreOffice version: " + ex.Message, true);
			}
		}

		/// <summary>
		/// Gets the supported operating system for the converter
		/// </summary>
		/// <returns>Returns a list of string with the suported operating systems</returns>
		public override List<string> GetSupportedOS()
		{
			var supportedOS = new List<string>();
			supportedOS.Add(PlatformID.Win32NT.ToString());
			supportedOS.Add(PlatformID.Unix.ToString());
			return supportedOS;
		}

		/// <summary>
		/// Convert a file to a new format
		/// </summary>
		/// <param name="file">The file to convert</param>
		/// <param name="pronom">The file format to convert to</param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async public override Task ConvertFile(FileToConvert file, string pronom)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			// Get correct folders and properties required for conversion
			string inputFolder = GlobalVariables.ParsedOptions.Input;
			string outputFolder = GlobalVariables.ParsedOptions.Output;

			string outputDir = Directory.GetParent(file.FilePath.Replace(inputFolder, outputFolder))?.ToString() ?? "";
			string inputDirectory = Directory.GetParent(file.FilePath)?.ToString() ?? "";
			string inputFilePath = Path.Combine(inputDirectory, Path.GetFileName(file.FilePath));
			string executableName = currentOS.Platform == PlatformID.Unix ? "soffice" : "soffice.exe";

			bool sofficePathWindowsExists = CheckPathVariableWindows(executableName);
			bool sofficePathLinuxExists = CheckPathVariableLinux(executableName);

			string targetFormat = GetConversionExtension(pronom);

			// Depending on operating system run Libreoffice with different executable path
			if (executableName == "soffice.exe")
			{
				// Libreoffice will not allow several threads running and half of the documents will not
				// be converted without the lock
				lock (locker)
				{
					RunOfficeToPdfConversion(inputFilePath, outputDir, pronom, sofficePathWindowsExists, targetFormat, file);
				}
			}
			else if (executableName == "soffice")
			{
				lock (locker)
				{
					RunOfficeToPdfConversion(inputFilePath, outputDir, pronom, sofficePathLinuxExists, targetFormat, file);
				}
			}
			else
			{
				log.SetUpRunTimeLogMessage("LibreOffice Operating system not supported for office conversion", true, file.FilePath);
			}
		}
        /// <summary>
        /// Reference list stating supported conversions containing key value pairs with string input pronom and string output pronom
        /// </summary>
        /// <returns>List of all supported conversions</returns>
        public override Dictionary<string, List<string>> GetListOfSupportedConvesions()
        {
            var supportedConversions = new Dictionary<string, List<string>>();

            CheckAndRemovePDFAPronoms();

            AddConversionsForMultipleFormats(supportedConversions);

            return supportedConversions;
        }

		/// <summary>
		/// Removes PDF/A PRONOMs from the list if iText7 is not found
		/// </summary>
        private void CheckAndRemovePDFAPronoms()
        {
            var converters = AddConverters.Instance.GetConverters();
            iTextFound = converters.Any(converter => converter.Name == new IText7().Name);

            if (!iTextFound)
            {
                foreach (string pronom in PDFAPronoms.ToList())
                {
                    PDFPronoms.Remove(pronom);
                }
            }
        }

		/// <summary>
		/// Sets the supported conversions for multiple formats
		/// </summary>
		/// <param name="supportedConversions"> The dictionary where the supported conversions are saved to </param>
        private static void AddConversionsForMultipleFormats(Dictionary<string, List<string>> supportedConversions)
        {
            List<string> emptyList = [];
			// EXCEL 
            AddConversions(XLSXPronoms, PDFPronoms, ODSPronoms, emptyList, supportedConversions);
            AddConversions(XLSPronoms,  PDFPronoms, XLSXPronoms, ODSPronoms, supportedConversions);
            AddConversions(XLSMPronoms, PDFPronoms, XLSXPronoms, ODSPronoms, supportedConversions);
            AddConversions(XLTXPronoms, PDFPronoms, XLSXPronoms, ODSPronoms, supportedConversions);
            // WORD
            AddConversions(DOCXPronoms, PDFPronoms, ODTPronoms, emptyList, supportedConversions);
			AddConversions(DOCPronoms,  PDFPronoms, DOCXPronoms, ODTPronoms, supportedConversions);
            AddConversions(DOCMPronoms, PDFPronoms, DOCXPronoms, ODTPronoms, supportedConversions);
            AddConversions(DOTXPronoms, PDFPronoms, DOCXPronoms, ODTPronoms, supportedConversions);
            // PowerPoint
            AddConversions(PPTXPronoms, PDFPronoms, ODPPronoms, emptyList, supportedConversions);
            AddConversions(PPTPronoms,  PDFPronoms, PPTXPronoms, ODPPronoms, supportedConversions);
            AddConversions(PPTMPronoms, PDFPronoms, PPTXPronoms, ODPPronoms, supportedConversions);
            AddConversions(POTXPronoms, PDFPronoms, PPTXPronoms, ODPPronoms, supportedConversions);
            // Open Document 
            AddConversions(ODPPronoms, PDFPronoms, PPTXPronoms, emptyList, supportedConversions);
            AddConversions(ODTPronoms, PDFPronoms, DOCXPronoms, emptyList, supportedConversions);
            AddConversions(ODSPronoms, PDFPronoms, XLSXPronoms, emptyList, supportedConversions);
            // RTF
            AddConversions(RTFPronoms, PDFPronoms, DOCXPronoms, ODTPronoms, supportedConversions);
			// CSV
			AddConversions(CSVPronoms, PDFPronoms, emptyList, emptyList, supportedConversions);
        }

        /// <summary>
        /// Adds the conversions to the supported conversions list
        /// </summary>
        /// <param name="sourceFormats"> the format that is upporting the conversions to the targets </param>
        /// <param name="targetFormats1"> target format 1 </param>
        /// <param name="targetFormats2"> target format 2 (send empty list here if the sourceFormat only supports one conversion) </param>
        /// <param name="targetFormats3">target format 3 (send empty list here if the sourceFormat only supports two conversions) </param>
        /// <param name="supportedConversions"> The dictionary where the supported conversions are saved to </param>
        static private void AddConversions(List<string> sourceFormats, List<string> targetFormats1, List<string> targetFormats2, List<string> targetFormats3, Dictionary<string, List<string>> supportedConversions)
        {
            foreach (string sourceFormat in sourceFormats)
            {
                if (!supportedConversions.TryGetValue(sourceFormat, out var pronomList))
                {
                    pronomList = new List<string>();
                    supportedConversions[sourceFormat] = pronomList;
                }

                if (targetFormats1.Count > 0)
                {
                    pronomList.AddRange(targetFormats1);
                }
                if (targetFormats2.Count > 0)
                {
                    pronomList.AddRange(targetFormats2);
                }
                if (targetFormats3.Count > 0)
                {
                    pronomList.AddRange(targetFormats3);
                }
            }
        }

        /// <summary>
        /// Get a dictionary of all conversions that blocks multithreading
        /// </summary>
        /// <returns> the list </returns>
        public override Dictionary<string, List<string>> GetListOfBlockingConversions()
		{
			// LibreOffice blocks all conversions
			return SupportedConversions;
		}

        /// <summary>
        /// Converts and office file to PDF
        /// </summary>
        /// <param name="sourceDoc"> office file </param>
        /// <param name="destinationPdfFolder"> the folder of where the PDF ends up </param>
        /// <param name="pronom"> target PRONOM for the office file </param>
        /// <param name="sofficePath"> </param>
		/// <param name="extention"> The extention after conversion (ex. pdf)</param>
		/// <param name="file"> The file to be converted </param>
        void RunOfficeToPdfConversion(string sourceDoc, string destinationPdfFolder, string pronom,
										  bool sofficePath, string extention, FileToConvert file)
		{
			try
			{
				bool converted = false;
				int count = 0;
				string newFileName = Path.Combine(destinationPdfFolder, Path.GetFileNameWithoutExtension(sourceDoc) + "." + extention);
				do
				{
					using (Process process = new Process())
					{
						// Set the correct properties for the process thta will run libreoffice
						process.StartInfo.FileName = GetPlatformExecutionFile();
						process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

						string sofficeCommand = GetSofficePath(sofficePath);
						string arguments = GetLibreOfficeCommand(destinationPdfFolder, sourceDoc, sofficeCommand, extention);
						process.StartInfo.Arguments = arguments;
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.RedirectStandardError = true;
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;

						process.Start();

						// Get output and potential error
						string standardOutput = process.StandardOutput.ReadToEnd();
						string standardError = process.StandardError.ReadToEnd();

						process.WaitForExit();
						int exitCode = process.ExitCode;

						if (exitCode != 0)      // Something went wrong, warn the user
						{
							Console.WriteLine($"\n Filepath: {sourceDoc} :  Exit Code: {exitCode}\n");
							Console.WriteLine("Standard Output:\n" + standardOutput);
							Console.WriteLine("Standard Error:\n" + standardError);
						}
					}

					// Set the new filename and check if the document was converted correctly
					file.FilePath = newFileName;
					string? currPronom = GetPronom(newFileName);
					//Convert to another PDF format if LibreOffice's standard output format is not the desired one
					if (currPronom != null && currPronom != pronom && PDFPronoms.Contains(pronom) && iTextFound)
					{
						file.Route.Add(pronom);
						pronom = currPronom;    //Override target pronom since the final PDF conversion will be done in iText7
					}
					converted = CheckConversionStatus(newFileName, pronom);
				} while (!converted && ++count < GlobalVariables.MAX_RETRIES);
				if (converted)
				{
					// Delete copy in ouputfolder if converted successfully
					DeleteOriginalFileFromOutputDirectory(sourceDoc);
					ReplaceFileInList(newFileName, file);
				}
				else
				{
					file.Failed = true;
				}
			}
			catch (Exception e)
			{
				Logger.Instance.SetUpRunTimeLogMessage("LibreOffice Error converting file to PDF. File is not converted: " + e.Message, true, filename: sourceDoc);
			}
		}

        /// <summary>
        /// Gets the path to the soffice executable unless it is added to the systems PATH
        /// then it just returns "soffice" which the name of the executable
        /// </summary>
        /// <param name="sofficePath"> Bool - Indicating if is in the PATH of the environment</param>
        /// <returns> see summary </returns>
        private string GetSofficePath(bool sofficePath)
		{
			string sofficePathString;
			if (sofficePath)
			{
				sofficePathString = "soffice";
			}
			else if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				sofficePathString = sofficePathLinux;
			}
			else
			{
				sofficePathString = sofficePathWindows;
			}

			return sofficePathString;
		}

		/// <summary>
		/// Gets the command for running Libreoffice with the correct arguments.
		/// </summary>
		/// <param name="destinationPDF"> Output folder for the PDF</param>
		/// <param name="sourceDoc"> Path to the original document</param>
		/// <param name="sofficeCommand"> Either path to the executable or just 'soffice'</param>
		/// <param name="targetFormat"> The format to convert to</param>
		/// <returns> see summary </returns>
		static string GetLibreOfficeCommand(string destinationPDF, string sourceDoc, string sofficeCommand, string targetFormat)
		{
			return Environment.OSVersion.Platform == PlatformID.Unix ? $@"-c ""soffice --headless --convert-to {targetFormat} --outdir '{destinationPDF}' '{sourceDoc}'""" : $@"/C {sofficeCommand} --headless --convert-to {targetFormat} --outdir ""{destinationPDF}"" ""{sourceDoc}""";
		}

		/// <summary>
		/// Gets the correct extension for PRONOMs supported by LibreOffice
		/// </summary>
		/// <param name="targetPronom"> the PRONOM </param>
		/// <returns> see summary </returns>
		static string GetConversionExtension(string targetPronom)
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
				case "fmt/1755":
				case "fmt/137":
				case "fmt/294":
				case "fmt/295":
					extensionNameForConversion = "ods";
					break;
				case "x-fmt/3":
				case "fmt/1756":
				case "fmt/136":
				case "fmt/290":
				case "fmt/291":
					extensionNameForConversion = "odt";
					break;
				case "fmt/1827":
				case "fmt/412":
					extensionNameForConversion = "docx";
					break;
				case "fmt/215":
				case "fmt/1829":
				case "fmt/494":
					extensionNameForConversion = "pptx";
					break;
				case "fmt/293":
				case "fmt/292":
				case "fmt/138":
				case "fmt/1754":
					extensionNameForConversion = "odp";
					break;
				default:
					extensionNameForConversion = "pdf";
					break;
			}

			return extensionNameForConversion;
		}

		static List<string> PDFPronoms = [
			"fmt/95",       // PDF/A 1A
			"fmt/354",      // PDF/A 1B
			"fmt/476",      // PDF/A 2A
			"fmt/477",      // PDF/A 2B
			"fmt/478",      // PDF/A 2U
			"fmt/479",      // PDF/A 3A
			"fmt/480",      // PDF/A 3B
			"fmt/481",      // PDF/A 3U
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

		static List<string> PDFAPronoms = [
			"fmt/95",       // PDF/A 1A
			"fmt/354",      // PDF/A 1B
			"fmt/476",      // PDF/A 2A
			"fmt/477",      // PDF/A 2B
			"fmt/478",      // PDF/A 2U
			"fmt/479",      // PDF/A 3A
			"fmt/480",      // PDF/A 3B
			"fmt/481",      // PDF/A 3U
		];

		static List<string> DOCPronoms =
		[
			// DOC
			"x-fmt/329",
			"fmt/609",
			"fmt/39",
			"x-fmt/274",
			"x-fmt/275",
			"x-fmt/276",
			"fmt/1688",
			"fmt/37",
			"fmt/38",
			"fmt/1282",
			"fmt/1283",
			"x-fmt/131",
			"x-fmt/42",
			"x-fmt/43",
			"fmt/40",
			"x-fmt/44",
			"x-fmt/393",
			"x-fmt/394",
			"fmt/892",
		];
		static List<string> DOCXPronoms =
		[
			// DOCX
			//"fmt/473",		This is the code for Office Owner File
			"fmt/1827",
			"fmt/412",
		];

		static List<string> DOCMPronoms =
		[
			"fmt/523", // DOCM
		];
		static List<string> DOTXPronoms =
		[
			 "fmt/597", // DOTX
		];

		static List<string> XLSPronoms =
		[
			//XLS
			"fmt/55",
			"fmt/56",
			"fmt/57",
			"fmt/61",
			"fmt/62",
			"fmt/59",
		];
		static List<string> XLTXPronoms =
		[
			"fmt/598", // XLTX
		];
		static List<string> XLSMPronoms =
		[
			"fmt/445", // XLSM
		];
		static List<string> XLSXPronoms =
		[
			//XLSX
			"fmt/214",
			"fmt/1828",
		];
		static List<string> CSVPronoms =
		[
		   "x-fmt/18", //CSV
			"fmt/800",
		];
		static List<string> PPTPronoms =
		[
			// PPT
			"fmt/1537",
			"fmt/1866",
			"fmt/181",
			"fmt/1867",
			"fmt/179",
			"fmt/1747",
			"fmt/1748",
			"x-fmt/88",
			"fmt/125",
			"fmt/126",
		];

		static List<string> PPTXPronoms =
		[
			// PPTX
			"fmt/215",
			"fmt/1829",
			"fmt/494",
		];
		static List<string> PPTMPronoms =
		[
			// PPTM
			"fmt/487",
		];
		static List<string> POTXPronoms =
		[
			//POTX
			"fmt/631",
		];

		static List<string> ODPPronoms =
		[
			// ODP
			"fmt/293",
			"fmt/292",
			"fmt/138",
			"fmt/1754",
		];

		static List<string> ODTPronoms =
		[
			// ODT
			"x-fmt/3",
			"fmt/1756",
			"fmt/136",
			"fmt/290",
			"fmt/291",
		];
		static List<string> ODSPronoms =
		[
			// ODS
			"fmt/1755",
			"fmt/137",
			"fmt/294",
			"fmt/295",
		];

		static List<string> RTFPronoms =
		[
			"fmt/969",
			"fmt/45",
			"fmt/50",
			"fmt/52",
			"fmt/53",
			"fmt/355",
		];

		// Unused lists of all formats of a type, can uncomment and add this later,
		// for example if you want to support conversion for doc to all PowerPoint Formats.
		// static List<string> AllPowerPointFormats = PPTMPronoms.Concat(PPTXPronoms).Concat(PPTPronoms).Concat(POTXPronoms).ToList();

		//static List<string> AllWordFormats = DOCPronoms.Concat(DOCXPronoms).Concat(DOCMPronoms).Concat(DOTXPronoms).ToList();

		//static List<string> AllExcelFormats = XLSPronoms.Concat(XLSXPronoms).Concat(XLSMPronoms).Concat(XLTXPronoms).ToList();

		//static List<string> AllOpenDocumentFormats = ODTPronoms.Concat(ODSPronoms).Concat(ODPPronoms).ToList();
	}
}