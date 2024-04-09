using System.Diagnostics;
using Ghostscript.NET.Rasterizer;
using System.Drawing.Imaging;
using Ghostscript.NET;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1;

//TODO: Put all images in a folder with original name and delete original file

/// <summary>
/// GhostScript is a subclass of the Converter class.   <br></br>
/// 
/// GhostScript supports the following conversions:     <br></br>
/// - PDF to Image (png, jpg, tif, bmp)                 <br></br>
/// - PostScript to PDF                                 <br></br>
///                                                     <br></br>
/// Conversions not added:                              <br></br>
/// - Image to PDF  (see iText7)                        <br></br>
/// </summary>
public class GhostscriptConverter : Converter
{
	private static readonly object lockobject = new object();
	public GhostscriptConverter()
	{
		Name = "Ghostscript";
		GetExecutablePath();
		SetNameAndVersion();
		SupportedConversions = getListOfSupportedConvesions();
		SupportedOperatingSystems = getSupportedOS();
		
	}

    public override void GetVersion()
    {
		//TODO: Actually fetch version
		Version = "";

        string output = "";
        string error = "";


        using (Process process = new Process())
        {
            process.StartInfo.FileName = System.OperatingSystem.IsWindows() ? gsWindowsExecutable : "/bin/bash gs";
            process.StartInfo.Arguments = "-h";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            output = process.StandardOutput.ReadToEnd();
            error = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        if (error != "")
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error getting GhostScript version: " + error, true);
        }
        else
        {
            // Define a regular expression pattern to match the version number
            string pattern = @"\d+\.\d+\.\d+";

            // Create a Regex object
            Regex regex = new Regex(pattern);

            // Match the pattern against the input string
            Match match = regex.Match(output);

            // Check if a match was found
            if (match.Success)
            {
                // Extract the matched version number
                Version = match.Value;
            } else
			{
				Version = "Version not found";
			}
        }
    }

	void GetExecutablePath()
	{
		string fileName = "gswin64c.exe";
		var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, fileName, SearchOption.AllDirectories);
		if (files.Length > 0)
		{
            gsWindowsExecutable = files[0];
        }
        else
		{
            Logger.Instance.SetUpRunTimeLogMessage("Ghostscript executable not found", true);
        }

		fileName = "gsdll64.dll";
		files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, fileName, SearchOption.AllDirectories);
		if (files.Length > 0)
		{
            gsWindowsLibrary = files[0];
        }
        else
		{
            Logger.Instance.SetUpRunTimeLogMessage("Ghostscript library not found", true);
        }
	}

	public string gsWindowsExecutable = "";

    public string gsWindowsLibrary = "";

   

	/// <summary>
	/// Reference list stating supported conversions containing key value pairs with string input pronom and string output pronom
	/// </summary>
	/// <returns>List of all conversions</returns>
	public override Dictionary<string, List<string>> getListOfSupportedConvesions()
	{
		var supportedConversions = new Dictionary<string, List<string>>();
		//PDF to Image
		foreach (string pdfPronom in PDFPronoms.Concat(PDFAPronoms))
		{
			supportedConversions.Add(pdfPronom, PNGPronoms.Concat(JPGPronoms.Concat(BMPPronoms.Concat(TIFFPronoms))).ToList());
		}

		//PostScript to PDF
		foreach (string postScriptPronom in PostScriptPronoms)
		{
			supportedConversions.Add(postScriptPronom, PDFPronoms.Concat(PDFAPronoms).ToList());
		}

		return supportedConversions;
	}

	public override List<string> getSupportedOS()
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
	/// <param name="filePath">The file to be converted</param>
	/// <param name="pronom">The file format to convert to</param>
	async public override Task ConvertFile(FileToConvert file, string pronom)
	{
		string outputFileName = Path.GetFileNameWithoutExtension(file.FilePath);
		string? extension = GetExtension(pronom);
		string? sDevice = GetDevice(pronom);

		if(extension == null || sDevice == null)
		{
			Logger.Instance.SetUpRunTimeLogMessage(pronom + " is not supported by GhostScript. File is not converted.", true,filename: file.FilePath);
			return;
		}

		try
		{
            if (extension == ".pdf")
            {
                string pdfVersion = GetPDFVersion(pronom);
				lock (lockobject)
				{
					convertToPDF(file, outputFileName, sDevice, extension, pdfVersion, pronom);
				}
            }
            else
            {
				lock (lockobject)
				{
					if (OperatingSystem.IsWindows())
					{
						convertToImagesWindows(file, outputFileName, sDevice, extension, pronom);
					}
					else
					{
						convertToImagesLinux(file, outputFileName, sDevice, extension, pronom);
					}
				}
            }
        }
        catch(Exception e)
		{
			Logger.Instance.SetUpRunTimeLogMessage(pronom + " is not supported by GhostScript. File is not converted.", true, file.FilePath);
		}
	}

	/// <summary>
	/// Convert a file using GhostScript command line
	/// </summary>
	/// <param name="filePath">The file to be converted</param>
	/// <param name="outputFileName">The name of the new file</param>
	/// <param name="sDevice">What format GhostScript will convert to</param>
	/// <param name="extension">Extension type for after the conversion</param>
	void convertToImagesWindows(FileToConvert file, string outputFileName, string sDevice, string extension, string pronom)
	{
		Logger log = Logger.Instance;
		if (!System.OperatingSystem.IsWindowsVersionAtLeast(6,1)) 
		{
			log.SetUpRunTimeLogMessage("GhostScript is not supported on this version of Windows (minimum 6.1 required). File is not converted.", true, filename: file.FilePath);
			return; 
		}
		try
		{
            int count = 0;
			bool converted = true;
			List<FileInfo> files;
			var originalFileInfo = FileManager.Instance.GetFile(file.Id);
			if(originalFileInfo == null)
			{
				file.Failed = true;
				return;
			}
			do {
				files = new List<FileInfo>(); //Clear list of files

                //Create folder for images with original name
                string filename = Path.GetFileNameWithoutExtension(file.FilePath);
				string relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(),file.FilePath);
				 
                string folderPath = relPath.Substring(0, relPath.LastIndexOf('.'));
                if (Directory.Exists(folderPath))
				{
                    //Clear folder if it already exists
                    Directory.Delete(folderPath, true);
                }
				Directory.CreateDirectory(folderPath);

                using (var rasterizer = new GhostscriptRasterizer())
				{
					GhostscriptVersionInfo versionInfo = new GhostscriptVersionInfo(new Version(), gsWindowsLibrary, string.Empty, GhostscriptLicense.GPL);
					using (var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read))
					{
						rasterizer.Open(stream, versionInfo, false);
						ImageFormat? imageFormat = GetImageFormat(extension);
						if (imageFormat != null)
						{
							for (int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
							{
								string pageOutputFileName = String.Format("{0}{1}{2}_{3}{4}", folderPath, Path.DirectorySeparatorChar, outputFileName, pageNumber.ToString(), extension);
								using (var image = rasterizer.GetPage(300, pageNumber))
								{
									image.Save(pageOutputFileName, imageFormat);
								}

								var newFile = new FileInfo(pageOutputFileName, originalFileInfo);
								newFile.IsPartOfSplit = true;
								newFile.AddConversionTool(NameAndVersion);
                                newFile.UpdateSelf(new FileInfo(Siegfried.Instance.IdentifyFile(newFile.FilePath, true)));
                                files.Add(newFile);
							}
						}
					}
				}
				foreach (var newFile in files)
				{
					converted = CheckConversionStatus(newFile.FilePath, pronom);
					//It is only relevant to check if at least one file is not converted, rest will be checked at the end of conversion
					if (!converted)
					{
						break;
					}
				}
			} while (!converted && ++count < GlobalVariables.MAX_RETRIES);
            FileManager.Instance.AddFiles(files);
			if (converted)
			{
				deleteOriginalFileFromOutputDirectory(file.FilePath);
				originalFileInfo.Display = false;
				originalFileInfo.IsDeleted = true;
				originalFileInfo.UpdateSelf(files.First());	//TODO: Ask County Archive how they want the original file to be documented if it is split into different files
				originalFileInfo.IsConverted = true;
			}
			file.Failed = !converted;
        }
		catch (Exception e)
		{
			log.SetUpRunTimeLogMessage("Error when converting file with GhostScript. Error message: " + e.Message, true, filename: file.FilePath);
		}
	}

	private ImageFormat ?GetImageFormat(string extension)
	{
        if (!System.OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            Logger.Instance.SetUpRunTimeLogMessage("GhostScript is not supported on this version of Windows (6.1).", true);
            return null;
        }
        switch (extension)
		{
			case ".png":
				return ImageFormat.Png;
			case ".jpg":
				return ImageFormat.Jpeg;
			case ".tiff":
				return ImageFormat.Tiff;
			case ".bmp":
				return ImageFormat.Bmp;
			default:
				return null;
		}
	}

    /// <summary>
    /// Convert a file to images using GhostScript command line
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="outputFileName"></param>
    /// <param name="sDevice"></param>
    /// <param name="extension"></param>
    /// <param name="pronom"></param>
    void convertToImagesLinux(FileToConvert file, string outputFileName, string sDevice, string extension, string pronom)
    {
        try
        {
            int count = 0;
            bool converted = true;
			List<FileInfo> files;
            var originalFileInfo = FileManager.Instance.GetFile(file.Id);
			if (originalFileInfo == null)
			{
                file.Failed = true;
                return;
            }

			do
			{
				files = new List<FileInfo>(); //Clear list of files
				//Create folder for images with original name
				string folder = Path.GetFileNameWithoutExtension(file.FilePath);
				string folderPath = Path.Combine(GlobalVariables.parsedOptions.Output, folder);
				if (Directory.Exists(folderPath))
				{
					//Clear folder if it already exists
					Directory.Delete(folderPath, true);
				}
				Directory.CreateDirectory(folderPath);

				string command = $"gs -sDEVICE={sDevice} -o {folderPath}{outputFileName}%d{extension} {file.FilePath}";  // %d adds page number to filename, i.e outputFileName1.png outputFileName2.png

				ProcessStartInfo startInfo = new ProcessStartInfo();
				startInfo.FileName = "/bin/bash";
				startInfo.Arguments = $"-c \"{command}\"";
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;
				startInfo.CreateNoWindow = true;
				startInfo.RedirectStandardError = true;

				using (Process? process = Process.Start(startInfo))
				{
					string? output = process?.StandardOutput.ReadToEnd();
					string? error = process?.StandardError.ReadToEnd();

					process?.WaitForExit();
				}

				var newFilePaths = Directory.GetFiles(folderPath);
				foreach (var filePath in newFilePaths)
				{
					var newFile = new FileInfo(filePath, originalFileInfo);
					newFile.IsPartOfSplit = true;
					newFile.UpdateSelf(new FileInfo(Siegfried.Instance.IdentifyFile(newFile.FilePath,true)));
					newFile.AddConversionTool(NameAndVersion);
					files.Add(newFile);
                    converted = converted && CheckConversionStatus(newFile.FilePath, pronom);
                }
            } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
			FileManager.Instance.AddFiles(files);
            file.Failed = !converted;
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error when converting file with GhostScript. Error message: " + e.Message, true, filename: file.FilePath);
        }
    }



    /// <summary>
    ///     Convert to PDF using GhostScript
    /// </summary>
    /// <param name="filePath"> Name and path of original file </param>
    /// <param name="outputFileName"> Filename of the converted file </param>
    /// <param name="sDevice"> Ghostscript variable that determines what conversion it should do </param>
    /// <param name="extension"> Extension for the new file </param>
    /// <param name="pdfVersion"> The PDF version to covnert to </param>
    /// <param name="pronom"> The output pronom </param>
	void convertToPDF(FileToConvert file, string outputFileName, string sDevice, string extension, string pdfVersion, string pronom)
    {
        string? outputFolder = Path.GetDirectoryName(file.FilePath);

        string outputFilePath = Path.Combine(outputFolder, outputFileName + extension);
        string arguments = "-dCompatibilityLevel=" + pdfVersion + $" -sDEVICE={sDevice} -o " + outputFilePath + " " + file.FilePath;
        string command;

        try
        {
            int count = 0;
            bool converted = false;
            do
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
				if (OperatingSystem.IsWindows())
				{
					startInfo.FileName = gsWindowsExecutable;
					command = arguments;
				}
				else
				{
					startInfo.FileName = "/bin/bash";
					string linuxCommand = $"gs " + arguments;
					command = $"-c \"{linuxCommand}\"";
				}
				startInfo.Arguments = command;
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;
				startInfo.CreateNoWindow = true;
				using (Process? exeProcess = Process.Start(startInfo))
				{
					exeProcess?.WaitForExit();
				}

                // Set the new filename and check if the document was converted correctly
                file.FilePath = outputFilePath;
                string? currPronom = GetPronom(outputFilePath);
                if (currPronom == null)
                {
                    throw new Exception("Could not get pronom for file");
                }
                //Convert to another PDF format if Ghostscript's standard output format is not the desired one
                if (currPronom != pronom && 
						(PDFPronoms.Contains(pronom) || PDFAPronoms.Contains(pronom) ))
                {
                    var converter = new iText7();
                    // Add iText7 to the list of conversion tools
                    var FileInfoMap = ConversionManager.Instance.FileInfoMap;
                    if (FileInfoMap.ContainsKey(file.Id) && !FileInfoMap[file.Id].ConversionTools.Contains(converter.NameAndVersion))
                    {
                        FileInfoMap[file.Id].ConversionTools.Add(converter.NameAndVersion);
                    }
                    converter.convertFromPDFToPDF(file, pronom);
                }
                converted = CheckConversionStatus(outputFilePath, pronom);
            } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
            if (!converted)
            {
                file.Failed = true;
            }
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error when converting file with GhostScript. Error message: " + e.Message, true, filename: file.FilePath);
        }
    }


	List<string> PNGPronoms =
		[
        "fmt/11",
        "fmt/12",
        "fmt/13",
        "fmt/935"
        ];

	List<string> JPGPronoms =
		[
        "fmt/41",
        "fmt/42",
        "fmt/43",
        "fmt/44",
        "x-fmt/398",
        "x-fmt/390",
        "x-fmt/391",
        "fmt/645",
        "fmt/1507",
        "fmt/112"
        ];

	List<string> TIFFPronoms =
		[
        "fmt/1917",
        "x-fmt/399",
        "x-fmt/388",
        "x-fmt/387",
        "fmt/155",
        "fmt/353",
        "fmt/154",
        "fmt/153",
        "fmt/156"
        ];

	List<string> BMPPronoms =
		[
        "x-fmt/270",
        "fmt/115",
        "fmt/118",
        "fmt/119",
        "fmt/114",
        "fmt/116",
        "fmt/117"
        ];

    List<string> PDFPronoms =
        [
        "fmt/15",
        "fmt/16",
        "fmt/17",
        "fmt/18",
        "fmt/19",
        "fmt/20",
        "fmt/276",
        "fmt/1129"
        ];

    List<string> PDFAPronoms =
        [
        "fmt/95",       // PDF/A 1A
        "fmt/354",      // PDF/A 1B
        "fmt/476",      // PDF/A 2A
        "fmt/477",      // PDF/A 2B
        "fmt/478",      // PDF/A 2U
        "fmt/479",      // PDF/A 3A
        "fmt/480",      // PDF/A 3B
		];

    List<string> PostScriptPronoms =
        [
        "fmt/124",
        "x-fmt/91",
        "x-fmt/406",
        "x-fmt/407",
        "x-fmt/408",
        "fmt/501"
        ];

	string? GetExtension(string pronom)
	{
		if(PDFPronoms.Contains(pronom) || PDFAPronoms.Contains(pronom))
		{
			return ".pdf";
		}
		if (PNGPronoms.Contains(pronom))
		{
			return ".png";
		}
		if (JPGPronoms.Contains(pronom))
		{
            return ".jpg";
        }
		if (TIFFPronoms.Contains(pronom))
		{
            return ".tiff";
        }
		if (BMPPronoms.Contains(pronom))
		{
            return ".bmp";
        }

		return null;
	}

	string? GetDevice(string pronom)
	{
        if (PDFPronoms.Contains(pronom) || PDFAPronoms.Contains(pronom))
        {
            return "pdfwrite";
        }
        if (PNGPronoms.Contains(pronom))
        {
            return "png16m";
        }
        if (JPGPronoms.Contains(pronom))
        {
            return "jpeg";
        }
        if (TIFFPronoms.Contains(pronom))
        {
            return "tiff24nc";
        }
        if (BMPPronoms.Contains(pronom))
        {
            return "bmp16m";
        }
        return null;
	}

	string GetPDFVersion(string pronom)
	{
		switch (pronom)
		{
			case "fmt/15":		return "1.1";
			case "fmt/16":		return "1.2";
			case "fmt/17":		return "1.3";
			case "fmt/18":		return "1.4";
			case "fmt/19":		return "1.5";
			case "fmt/20":		return "1.6";
			case "fmt/276":		return "1.7";
			case "fmt/1129":	return "2";
			default:			return "2";
		}
	}
}


