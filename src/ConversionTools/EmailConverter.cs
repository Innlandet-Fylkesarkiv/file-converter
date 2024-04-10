﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Converts EML and MSG to pdf. Also allows for converting MSG to EML.
/// MSG TO EML on LINUX:          https://www.matijs.net/software/msgconv/ https://github.com/mvz/email-outlook-message-perl
/// MSG TO EML on WINDOWS:        https://www.rebex.net/mail-converter/
/// EML TO PDF WINDOWS and LINUX: https://github.com/nickrussler/email-to-pdf-converter
/// OLM to EML>                   https://github.com/PeterWarrington/olm-convert
/// 
/// EMLTO PDF converter requires Java installed on the pc aswell as https://github.com/wkhtmltopdf/wkhtmltopdf
/// needs to be in the systems PATH
/// 
/// MSG TO EML on linux has simple installation steps found in the link above.
/// </summary>
public class EmailConverter : Converter
{
    OperatingSystem currentOS;
    /// <summary>
    /// Constructor setting important properties for the class.
    /// </summary>
    public EmailConverter()
    {
        Name = "EmailConverter";
        Version = "";
        SupportedConversions = getListOfSupportedConvesions();
        BlockingConversions = getListOfBlockingConversions();
        SupportedOperatingSystems = getSupportedOS();
        currentOS = Environment.OSVersion;
    }

    /// <summary>
    /// Converts the file sent to a new target format
    /// </summary>
    /// <param name="filePath">The file to be converted</param>
    /// <param name="pronom">The file format to convert to</param>
    async public override Task ConvertFile(FileToConvert file, string pronom)
    {
        string inputFolder = GlobalVariables.parsedOptions.Input;   
        string outputFolder = GlobalVariables.parsedOptions.Output; 

        // Get the full path to the input directory and output directory 
        string outputDir = Directory.GetParent(file.FilePath.Replace(inputFolder, outputFolder))?.ToString() ?? "";
        string inputDirectory = Directory.GetParent(file.FilePath)?.ToString() ?? "";
        string inputFilePath = Path.Combine(inputDirectory, Path.GetFileName(file.FilePath));

        // Run conversion with correct paths
        await RunConversion(inputFilePath, outputDir, file, pronom);
    }

    /// <summary>
    /// Converts the file sent as a parameter to the new target format
    /// </summary>
    /// <param name="inputFilePath">Full path tot he input file</param>
    /// <param name="destinationDir">Full path to the output directory</param>
    /// <param name="file">The file to convert</param>
    /// <param name="pronom">The target pronom for the conversion</param>
    async Task RunConversion(string inputFilePath, string destinationDir, FileToConvert file, string pronom)
    {
        string workingDirectory = Directory.GetCurrentDirectory();
        string commandToExecute = "";
        string targetFormat = "";
        // Sets the targetFormat and command to execute based on the current pronom
        switch (file.CurrentPronom)      
        {
            // EML pronoms set targetFormat to pdf and get correct command
            case "fmt/278":
            case "fmt/950":
                commandToExecute = GetEmlToPdfCommand(inputFilePath, workingDirectory);
                targetFormat = "pdf";
                break;
            // MSG pronoms set targetformat to eml and get correct command based on OS
            case "x-fmt/430":
            case "fmt/1144":
                if (currentOS.Platform == PlatformID.Unix)
                {
                    commandToExecute = GetMsgToEmlCommandUnix(inputFilePath);
                }
                else
                {
                    commandToExecute = GetMsgToEmlCommandWindows(inputFilePath, workingDirectory, destinationDir);
                }
                targetFormat = "eml";
                break;
            default:
                break;
        }
        try
        {
            // Create the new process for the conversion
            using (Process process = new Process())
            {
                // Set the correct properties for the process that will run the conversion
                process.StartInfo.FileName = GetPlatformExecutionFile();
                process.StartInfo.WorkingDirectory = workingDirectory;

                process.StartInfo.Arguments = commandToExecute;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Get output and potential error
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();

                // wait for process to finish and get exit code
                process.WaitForExit();
                int exitCode = process.ExitCode;

                if (exitCode != 0)      // Something went wrong, warn the user
                {
                    Console.WriteLine($"\n Filepath: {file.FilePath} :  Exit Code: {exitCode}\n");
                    Console.WriteLine("Standard Output:\n" + standardOutput);
                    Console.WriteLine("Standard Error:\n" + standardError);
                }

                // Get the new filename and check if the document was converted correctly
                string newFileName = Path.Combine(destinationDir, Path.GetFileNameWithoutExtension(inputFilePath) + "." + targetFormat);
                file.FilePath = inputFilePath;
                bool converted = CheckConversionStatus(newFileName, pronom, file);
                if (!converted)
                {
                    throw new Exception("File was not converted");
                }
                else
                {
                    // Conversion was succesfull get new path and check for attachments
                    string newFolderName = Path.GetFileNameWithoutExtension (inputFilePath) + "-attachments";
                    string folderWithAttachments = Path.Combine(Path.GetDirectoryName(inputFilePath), newFolderName);
                    if (Directory.Exists(folderWithAttachments))
                    {
                        // Attachements found, add them to the working set for further conversion
                        await addAttachementFilesToWorkingSet(inputFilePath, folderWithAttachments);
                    } 
                    // Delete copy in ouputfolder if converted successfully
                    deleteOriginalFileFromOutputDirectory(inputFilePath);
                }
            }
        }
        // Catch error and log it
        catch (Exception e)
        {
            Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF. File is not converted: " + e.Message, true, filename: inputFilePath);
            throw;
        }
    }


    /// <summary>
    /// Reference list stating supported conversions containing 
    /// key value pairs with string input pronom and string output pronom
    /// </summary>
    /// <returns>List of all supported conversions</returns>
    public override Dictionary<string, List<string>> getListOfSupportedConvesions()
    {
        var supportedConversions = new Dictionary<string, List<string>>();
        // eml to pdf
        foreach (string emlPronom in EMLPronoms)
        {
            if (!supportedConversions.ContainsKey(emlPronom))
            {
                supportedConversions[emlPronom] = new List<string>();
            }
            supportedConversions[emlPronom].AddRange(PDFPronoms);
        }
        // msg to eml 
        foreach (string msgPronom in MSGPronoms)
        {
            if (!supportedConversions.ContainsKey(msgPronom))
            {
                supportedConversions[msgPronom] = new List<string>();
            }
            supportedConversions[msgPronom].AddRange(EMLPronoms);
        }
        return supportedConversions;
    }

    public override Dictionary<string, List<string>> getListOfBlockingConversions()
    {
        return new Dictionary<string, List<string>>();
    }


    /// <summary>
    /// Gets the correct command for executing eml to pdf conversion
    /// </summary>
    /// <param name="inputFilePath">Full path to the input file</param>
    /// <param name="workingDirectory">Working directory for the program</param>
    /// <returns>Returns the string with the correct command</returns>
    string GetEmlToPdfCommand(string inputFilePath, string workingDirectory)
    {
        Version = "2.6.0";
        
        // Get correct path to email converter relative to the workign directory
        string relativeJarPathWindows = ".\\src\\ConversionTools\\emailconverter-2.6.0-all.jar";
        string relativeJarPathLinux = "./src/ConversionTools/emailconverter-2.6.0-all.jar";
        string jarFile = Environment.OSVersion.Platform == PlatformID.Unix ? Path.Combine(workingDirectory + relativeJarPathLinux) 
                                                                           : Path.Combine(workingDirectory + relativeJarPathWindows);
        return Environment.OSVersion.Platform == PlatformID.Unix ? $@"-c java -jar ""{jarFile}"" ""{inputFilePath}"" -a": 
                                                                   $@" /C java -jar ""{jarFile}"" ""{inputFilePath}"" -a";
    }

    /// <summary>
    /// Get Command for MSG to EML for linux
    /// </summary>
    /// <param name="inputFilePath">Full patht o the input path</param>
    /// <returns>Returns the string with the correct command </returns>
    string GetMsgToEmlCommandUnix(string inputFilePath)
    {
        return $@"-c msgconvert ""{inputFilePath}"" ";
    }

    /// <summary>
    /// Get command for MSG to EML for Windows
    /// </summary>
    /// <param name="inputFilePath">Full path to the input file </param>
    /// <param name="workingDirectory">Current working directoy</param>
    /// <param name="destinationDir">Destination directory for the output file</param>
    /// <returns>returns the string with the correct command</returns>
    string GetMsgToEmlCommandWindows(string inputFilePath, string workingDirectory, string destinationDir)
    {
        // Get the correct path to the exe file for the mailcovnerter
        string relativeRebexFilePath = "src\\ConversionTools\\MailConverter.exe";
        string rebexConverterFile = Path.Combine(workingDirectory, relativeRebexFilePath);
        return $@" /C {rebexConverterFile} to-mime --ignore ""{inputFilePath}"" ""{destinationDir}"" ";
    }

    /// <summary>
    /// Get the supported operating system for this converter 
    /// </summary>
    /// <returns>Returns list of strings wiht the supported OS'es</returns>
    public override List<string> getSupportedOS()
    {
        var supportedOS = new List<string>();
        supportedOS.Add(PlatformID.Win32NT.ToString());
        supportedOS.Add(PlatformID.Unix.ToString());
        return supportedOS;
    }

    /// <summary>
    /// Adds the attachement files to the current working set for conversion
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <returns></returns>
    public async Task addAttachementFilesToWorkingSet(string inputFilePath, string folderWithAttachments)
    {
        List<FileInfo>? attachementFiles = await Siegfried.Instance.IdentifyFilesIndividually(folderWithAttachments);
        foreach (FileInfo newFile in attachementFiles)
        {
            Guid id = Guid.NewGuid();
            newFile.Id = id;
            var newFileToConvert = new FileToConvert(newFile);
            newFileToConvert.TargetPronom = Settings.GetTargetPronom(newFile);

            //Use current and target pronom to create a key for the conversion map
            var key = new KeyValuePair<string, string>(newFileToConvert.CurrentPronom, newFileToConvert.TargetPronom);
            //If the conversion map contains the key, set the route to the value of the key
            if (ConversionManager.Instance.ConversionMap.ContainsKey(key))
            {
                newFileToConvert.Route = new List<string>(ConversionManager.Instance.ConversionMap[key]);
            }
            //If the conversion map does not contain the key, set the route to the target pronom
            else if (newFileToConvert.CurrentPronom != newFileToConvert.TargetPronom)
            {
                newFileToConvert.Route.Add(newFileToConvert.TargetPronom);
            }
            else
            {
                continue;
            }
            newFile.Route = newFileToConvert.Route;
            newFileToConvert.addedDuringRun = true;
            ConversionManager.Instance.WorkingSet.TryAdd(id, newFileToConvert);
            ConversionManager.Instance.FileInfoMap.TryAdd(id, newFile);
        }
    }

    // Lists with the pronoms for correctly identifying the formats
    List<string> PDFPronoms =
    [
        "fmt/18",
    ];
    public List<string> EMLPronoms =
    [
        "fmt/278",
        "fmt/950"
    ];
    public List<string> MSGPronoms =
    [
        "x-fmt/430",
        "fmt/1144"
    ];
}