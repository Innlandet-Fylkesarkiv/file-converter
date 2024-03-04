﻿using iText.Kernel.Pdf;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
/// </summary>
public class LibreOfficeConverter : Converter
{
    Logger log = Logger.Instance;
    private static readonly object locker = new object();
    public LibreOfficeConverter()
    {
        Name = "Libreoffice";
        Version = getLibreOfficeVersion();
        SupportedConversions = getListOfSupportedConvesions();
        SupportedOperatingSystems = getSupportedOS();
    }

    private static string getLibreOfficeVersion()
    {
        //TODO: This opens a new window that needs user input. Not sure what the problem is
        //Run command soffice --version to get the version of LibreOffice
        /*
        string output = "";
        var error = "";
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "soffice";
            process.StartInfo.Arguments = "--version";
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
            Console.WriteLine("Error getting LibreOffice version: " + error);
        }
        var version = output.Split(' ')[1];
        */
        var version = "7.6.4";
        return version;
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
    public override void ConvertFile(FileToConvert file, string pronom)
    {
        // Get correct folders and properties required for conversion
        string inputFolder = GlobalVariables.parsedOptions.Input;
        string outputFolder = GlobalVariables.parsedOptions.Output;

        string outputDir = Directory.GetParent(file.FilePath.Replace(inputFolder, outputFolder)).ToString();
        string inputDirectory = Directory.GetParent(file.FilePath).ToString();
        string inputFilePath = Path.Combine(inputDirectory, Path.GetFileName(file.FilePath));
        string executableName = "soffice.exe";

        bool sofficePathWindows = checkSofficePathWindows(executableName);
        bool sofficePathLinux = checkSofficePathLinux("soffice");

        // Depending on operating system run Libreoffice with different executable path
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            lock (locker)
            {
                RunOfficeToPdfConversion(inputFilePath, outputDir, pronom, sofficePathWindows);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            lock (locker)
            {
                RunOfficeToPdfConversion(inputFilePath, outputDir, pronom, sofficePathLinux);
            }
        }
        else
        {
            log.SetUpRunTimeLogMessage("Operating system not supported for office conversion", true, file.FilePath);
        }
    }

    /// <summary>
    /// Reference list stating supported conversions containing key value pairs with string input pronom and string output pronom
    /// </summary>
    /// <returns>List of all supported conversions</returns>
    public override Dictionary<string, List<string>> getListOfSupportedConvesions()
    {
        var supportedConversions = new Dictionary<string, List<string>>();

        // XLS to XLSX, ODS and PDF
        foreach (string XLSPronom in XLSPronoms)
        {
            if (!supportedConversions.ContainsKey(XLSPronom))
            {
                supportedConversions[XLSPronom] = new List<string>();
            }
            supportedConversions[XLSPronom].AddRange(XLSXPronoms);
            supportedConversions[XLSPronom].AddRange(ODSPronoms);
            supportedConversions[XLSPronom].AddRange(PDFPronoms);
        }
        // XLSX to ODS and PDF
        foreach (string XLSXPronom in XLSXPronoms)
        {
            if (!supportedConversions.ContainsKey(XLSXPronom))
            {
                supportedConversions[XLSXPronom] = new List<string>();
            }
            supportedConversions[XLSXPronom].AddRange(ODSPronoms);
            supportedConversions[XLSXPronom].AddRange(PDFPronoms);
        }
        // XLSM to PDF, XLSX and ODS
        foreach (string XLSMPronom in XLSMPronoms)
        {
            if (!supportedConversions.ContainsKey(XLSMPronom))
            {
                supportedConversions[XLSMPronom] = new List<string>();
            }
            supportedConversions[XLSMPronom].AddRange(XLSXPronoms);
            supportedConversions[XLSMPronom].AddRange(ODSPronoms);
            supportedConversions[XLSMPronom].AddRange(PDFPronoms);
        }
        // XLTX to XLSX. ODS and PDf
        foreach (string XLTXPronom in XLTXPronoms)
        {
            if (!supportedConversions.ContainsKey(XLTXPronom))
            {
                supportedConversions[XLTXPronom] = new List<string>();
            }
            supportedConversions[XLTXPronom].AddRange(XLSXPronoms);
            supportedConversions[XLTXPronom].AddRange(ODSPronoms);
            supportedConversions[XLTXPronom].AddRange(PDFPronoms);
        }
        // DOC to ODT, DOCX and PDF
        foreach (string docPronom in DOCPronoms)
        {
            if (!supportedConversions.ContainsKey(docPronom))
            {
                supportedConversions[docPronom] = new List<string>();
            }
            supportedConversions[docPronom].AddRange(DOCXPronoms);
            supportedConversions[docPronom].AddRange(ODTPronoms);
            supportedConversions[docPronom].AddRange(PDFPronoms);
        }
        // DOCX to ODT and PDF
        foreach (string docxPronom in DOCXPronoms)
        {
            if (!supportedConversions.ContainsKey(docxPronom))
            {
                supportedConversions[docxPronom] = new List<string>();
            }
            supportedConversions[docxPronom].AddRange(ODTPronoms);
            supportedConversions[docxPronom].AddRange(PDFPronoms);
        }
        // DOCM to DOCX, ODT and PDF
        foreach (string docmPronom in DOCMPronoms)
        {
            if (!supportedConversions.ContainsKey(docmPronom))
            {
                supportedConversions[docmPronom] = new List<string>();
            }
            supportedConversions[docmPronom].AddRange(DOCXPronoms);
            supportedConversions[docmPronom].AddRange(PDFPronoms);
            supportedConversions[docmPronom].AddRange(ODTPronoms);
        }
        // DOTX to DOCX, ODT and PDF
        foreach (string dotxPronom in DOTXPronoms)
        {
            if (!supportedConversions.ContainsKey(dotxPronom))
            {
                supportedConversions[dotxPronom] = new List<string>();
            }
            supportedConversions[dotxPronom].AddRange(DOCXPronoms);
            supportedConversions[dotxPronom].AddRange(PDFPronoms);
            supportedConversions[dotxPronom].AddRange(ODTPronoms);
        }
        // PPT to PPTX, ODP and PDF
        foreach (string pptPronom in PPTPronoms)
        {
            if (!supportedConversions.ContainsKey(pptPronom))
            {
                supportedConversions[pptPronom] = new List<string>();
            }
            supportedConversions[pptPronom].AddRange(PDFPronoms);
            supportedConversions[pptPronom].AddRange(ODPPronoms);
            supportedConversions[pptPronom].AddRange(PPTXPronoms);
        }
        // PPTX to ODP and PDF
        foreach (string pptxPronom in PPTXPronoms)
        {
            if (!supportedConversions.ContainsKey(pptxPronom))
            {
                supportedConversions[pptxPronom] = new List<string>();
            }
            supportedConversions[pptxPronom].AddRange(PDFPronoms);
            supportedConversions[pptxPronom].AddRange(ODPPronoms);
        }
        // PPTM to PPTX, ODP and PDF
        foreach (string pptmPronom in PPTMPronoms)
        {
            if (!supportedConversions.ContainsKey(pptmPronom))
            {
                supportedConversions[pptmPronom] = new List<string>();
            }
            supportedConversions[pptmPronom].AddRange(PDFPronoms);
            supportedConversions[pptmPronom].AddRange(ODPPronoms);
            supportedConversions[pptmPronom].AddRange(PPTXPronoms);
        }
        // POTX to PPTX, ODP and PDF
        foreach (string potxPronom in POTXPronoms)
        {
            if (!supportedConversions.ContainsKey(potxPronom))
            {
                supportedConversions[potxPronom] = new List<string>();
            }
            supportedConversions[potxPronom].AddRange(PDFPronoms);
            supportedConversions[potxPronom].AddRange(ODPPronoms);
            supportedConversions[potxPronom].AddRange(PPTXPronoms);
        }
        // ODP to PPTX and PDF
        foreach (string odpPronom in ODPPronoms)
        {
            if (!supportedConversions.ContainsKey(odpPronom))
            {
                supportedConversions[odpPronom] = new List<string>();
            }
            supportedConversions[odpPronom].AddRange(PDFPronoms);
            supportedConversions[odpPronom].AddRange(PPTXPronoms);
        }
        // ODS to XLSX and PDF
        foreach (string odsPronom in ODSPronoms)
        {
            if (!supportedConversions.ContainsKey(odsPronom))
            {
                supportedConversions[odsPronom] = new List<string>();
            }
            supportedConversions[odsPronom].AddRange(PDFPronoms);
            supportedConversions[odsPronom].AddRange(XLSXPronoms);
        }
        // ODT to XLSX and PDF
        foreach (string odtPronom in ODTPronoms)
        {
            if (!supportedConversions.ContainsKey(odtPronom))
            {
                supportedConversions[odtPronom] = new List<string>();
            }
            supportedConversions[odtPronom].AddRange(PDFPronoms);
            supportedConversions[odtPronom].AddRange(DOCXPronoms);
        }

        // RTF to PDF, DOCX and ODT
        foreach (string rtfPronom in RTFPronoms)
        {
            if (!supportedConversions.ContainsKey(rtfPronom))
            {
                supportedConversions[rtfPronom] = new List<string>();
            }
            supportedConversions[rtfPronom].AddRange(PDFPronoms);
            supportedConversions[rtfPronom].AddRange(DOCXPronoms);
            supportedConversions[rtfPronom].AddRange(ODTPronoms);
        }
        foreach (string csvPronom in CSVPronoms)
        {
            if (!supportedConversions.ContainsKey(csvPronom))
            {
                supportedConversions[csvPronom] = new List<string>();
                supportedConversions[csvPronom].AddRange(PDFPronoms);
            }
        }

        return supportedConversions;
    }

    /// <summary>
    /// Converts and office file 
    /// </summary>
    /// <param name="sourceDoc"></param>
    /// <param name="destinationPdf"></param>
    /// <param name="pronom"></param>
    /// <param name="sofficePath"></param>
    void RunOfficeToPdfConversion(string sourceDoc, string destinationPdf, string pronom, bool sofficePath)
    {
        try
        {
            using (Process process = new Process())
            {
                // Set the correct properties for the process thta will run libreoffice
                process.StartInfo.FileName = GetPlatformExecutionFile();
                process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

                string sofficeCommand = GetSofficePath(sofficePath);
                string arguments = GetLibreOfficeCommand(destinationPdf, sourceDoc, sofficeCommand);
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

                // Get the new filename and check if the document was converted correctly
                string newFileName = Path.Combine(destinationPdf, Path.GetFileNameWithoutExtension(sourceDoc) + ".pdf");
                bool converted = CheckConversionStatus(sourceDoc, newFileName, pronom);
                if (!converted)
                {
                    throw new Exception("File was not converted");
                }
                else
                {
                    // Delete copy in ouputfolder if converted successfully
                    deleteOriginalFileFromOutputDirectory(sourceDoc);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF. File is not converted: " + e.Message, true, filename: sourceDoc);
            throw;
        }
    }

    /// <summary>
    /// Get the correct place where the process should be run depending on operating system
    /// </summary>
    /// <returns> String - Name of where the process should be run</returns>
    static string GetPlatformExecutionFile()
    {
        return Environment.OSVersion.Platform == PlatformID.Unix ? "bash" : "cmd.exe";
    }

    /// <summary>
    /// Gets the path to the soffice executable unless it is added to the systems PATH
    /// then it just returns "soffice" which the name of the executable
    /// </summary>
    /// <param name="sofficePath"> Bool - Indicating if is in the PATH of the environment</param>
    /// <returns></returns>
    static string GetSofficePath(bool sofficePath)
    {
        return sofficePath ? "soffice" : Environment.OSVersion.Platform == PlatformID.Unix ? "/usr/lib/libreoffice/program/soffice" : "C:\\Program Files\\LibreOffice\\program\\soffice.exe";
    }

    /// <summary>
    /// Gets the command for running Libreoffice with the correct arguments.
    /// </summary>
    /// <param name="destinationPDF"> Output folder for the PDF</param>
    /// <param name="sourceDoc"> Path to the original document</param>
    /// <param name="sofficeCommand"> Either path to the executable or just 'soffice'</param>
    /// <returns></returns>
    static string GetLibreOfficeCommand(string destinationPDF, string sourceDoc, string sofficeCommand)
    {

        return Environment.OSVersion.Platform == PlatformID.Unix ? $@"-c ""soffice --headless --convert-to pdf --outdir '{destinationPDF}' '{sourceDoc}'""" : $@"/C {sofficeCommand} --headless --convert-to pdf --outdir ""{destinationPDF}"" ""{sourceDoc}""";
    }

    /// <summary>
    /// Checks if the folder with the soffice.exe executable exists in the PATH.
    /// </summary>
    /// <param name="executableName">Name of the executable to have its folder in the PATH</param>
    /// <returns>Bool indicating if the directory containing the executable was found </returns>
    static bool checkSofficePathWindows(string executableName)
    {

        string pathVariable = Environment.GetEnvironmentVariable("PATH"); // Get the environment variables as a string
        string[] paths = pathVariable.Split(Path.PathSeparator);          // Split them into individual entries

        foreach (string path in paths)                                    // Go through and check if found  
        {
            string fullPath = Path.Combine(path, executableName);
            if (File.Exists(fullPath))
            {
                return true;
            }
        }

        return false;
    }
    /// <summary>
    /// Same function as for windows, but with small changes to facilitate for linux users
    /// </summary>
    /// <param name="executableName">Name of the executable to have its folder in the PATH</param>
    /// <returns></returns>
    static bool checkSofficePathLinux(string executableName)
    {
        string pathVariable = Environment.GetEnvironmentVariable("PATH");
        char pathSeparator = Path.PathSeparator;

        // Use : as the separator on Linux
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            pathSeparator = ':';
        }

        string[] paths = pathVariable.Split(pathSeparator);

        foreach (string path in paths)
        {
            string fullPath = Path.Combine(path, executableName);

            // Linux is case-sensitive, so check for case-insensitive existence
            if (Directory.Exists(fullPath))
            {
                return true;
            }
        }

        return false;
    }

    List<string> PDFPronoms =
    [
        "fmt/276",
        "fmt/477", // PDF 2-A
    ];
    List<string> DOCPronoms =
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
    List<string> DOCXPronoms =
    [
        // DOCX
        "fmt/473",
        "fmt/1827",
        "fmt/412",
    ];

    List<string> DOCMPronoms =
    [
        "fmt/523", // DOCM
    ];
    List<string> DOTXPronoms =
    [
         "fmt/597", // DOTX
    ];

    List<string> XLSPronoms =
    [
        //XLS
        "fmt/55",
        "fmt/56",
        "fmt/57",
        "fmt/61",
        "fmt/62",
        "fmt/59",
    ];
    List<string> XLTXPronoms =
    [
        "fmt/598", // XLTX
    ];
    List<string> XLSMPronoms =
    [
        "fmt/445", // XLSM
    ];
    List<string> XLSXPronoms =
    [
        //XLSX
        "fmt/214",
        "fmt/1828",
    ];
    List<string> CSVPronoms =
   [
       "x-fmt/18", //CSV
       "fmt/800",
   ];
    List<string> PPTPronoms =
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

    List<string> PPTXPronoms =
    [
        // PPTX
        "fmt/215",
        "fmt/1829",
        "fmt/494",
    ];
    List<string> PPTMPronoms =
    [
        // PPTM
        "fmt/487",
    ];
    List<string> POTXPronoms =
    [
        //POTX
        "fmt/631",
    ];

    List<string> ODPPronoms =
    [
        // ODT
        "fmt/293",
        "fmt/292",
        "fmt/138",
        "fmt/1754",
    ];

    List<string> ODTPronoms =
    [
        // ODT
        "x-fmt/3",
        "fmt/1756",
        "fmt/136",
        "fmt/290",
        "fmt/291",
    ];
    List<string> ODSPronoms =
    [
        // ODS
        "fmt/1755",
        "fmt/137",
        "fmt/294",
        "fmt/295",
    ];


    List<string> RTFPronoms =
    [
        "fmt/969",
        "fmt/45",
        "fmt/50",
        "fmt/52",
        "fmt/53",
        "fmt/355",
    ];
}