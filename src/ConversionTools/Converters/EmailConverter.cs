﻿using System.Diagnostics;
using FileConverter.HelperClasses;
using FileConverter.Managers;
using SF = FileConverter.Siegfried;

/// <summary>
/// Converts EML and MSG to pdf. Also allows for converting MSG to EML.
/// MSG TO EML on LINUX:          https://www.matijs.net/software/msgconv/ https://github.com/mvz/email-outlook-message-perl
/// MSG TO EML on WINDOWS:        https://www.rebex.net/mail-converter/
/// EML TO PDF WINDOWS and LINUX: https://github.com/nickrussler/email-to-pdf-converter
/// OLM to EML>                   https://github.com/PeterWarrington/olm-convert
/// 
/// EML TO PDF converter requires Java installed on the pc aswell as https://github.com/wkhtmltopdf/wkhtmltopdf
/// needs to be in the systems PATH
/// 
/// MSG TO EML on linux has simple installation steps found in the link above.
/// </summary>
/// 
namespace ConversionTools.Converters
{
    /// <summary>
    /// Class with functions required to convert the email formats mentioned above
    /// </summary>
    public class EmailConverter : Converter
    {
        public readonly OperatingSystem currentOS;
        readonly List<string> PDFPronoms = ["fmt/18"];

        /// <summary>
        /// Constructor setting important properties for the class.
        /// </summary>
        public EmailConverter()
        {
            Name = "EmailConverter";
            Version = "";
            SupportedConversions = GetListOfSupportedConvesions();
            BlockingConversions = GetListOfBlockingConversions();
            SupportedOperatingSystems = GetSupportedOS();
            currentOS = Environment.OSVersion;
            DependenciesExists = CheckDependencies();
        }

        public List<string> EMLPronoms { get; set; } = ["fmt/278", "fmt/950"];
        public List<string> MSGPronoms { get; set; } = ["x-fmt/430", "fmt/1144"];

        /// <summary>
        /// Converts the file sent to a new target format
        /// </summary>
        /// <param name="file">The file to convert</param>
        /// <param name="pronom">The file format to convert to</param>
        public async override Task ConvertFile(FileToConvert file, string pronom)
        {
            string inputFolder =  GlobalVariables.ParsedOptions.Input;
            string outputFolder = GlobalVariables.ParsedOptions.Output;

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
        private async Task RunConversion(string inputFilePath, string destinationDir, FileToConvert file, string pronom)
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
                bool converted = false;
                int count = 0;
                do
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
                            Logger.Instance.SetUpRunTimeLogMessage("Error converting file to PDF. File is not converted: " + standardError, true, filename: inputFilePath);
                        }

                        // Get the new filename and check if the document was converted correctly
                        string newFileNameNotRelative = Path.Combine(destinationDir, Path.GetFileNameWithoutExtension(inputFilePath) + "." + targetFormat);
                        converted = CheckConversionStatus(newFileNameNotRelative, file);
                        if (!converted)
                        {
                            file.Failed = true;
                        }
                        else
                        {
                            // Conversion was succesfull get new path and check for attachments
                            string newFolderName = Path.GetFileNameWithoutExtension(inputFilePath) + "-attachments";
                            string folderWithAttachments = Path.Combine(Path.GetDirectoryName(inputFilePath) ?? "", newFolderName);
                            if (Directory.Exists(folderWithAttachments))
                            {
                                // Attachements found, add them to the working set for further conversion
                                await AddAttachementFilesToWorkingSet(folderWithAttachments);
                            }
                            // Delete copy in ouputfolder if converted successfully
                            DeleteOriginalFileFromOutputDirectory(inputFilePath);
                        }
                    }
                } while (!converted && ++count < GlobalVariables.MAX_RETRIES);
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
        public override Dictionary<string, List<string>> GetListOfSupportedConvesions()
        {
            var supportedConversions = new Dictionary<string, List<string>>();
            // eml to pdf
            foreach (string emlPronom in EMLPronoms)
            {
                // pronomList becomes a reference to the list with the emlpronom key if found
                if (!supportedConversions.TryGetValue(emlPronom, out var pronomList))
                {
                    // Otherwise create a new entry in the dictionary
                    pronomList = new List<string>();
                    supportedConversions[emlPronom] = pronomList;
                }
                pronomList.AddRange(PDFPronoms);
            }
            // msg to eml 
            foreach (string msgPronom in MSGPronoms)
            {
                if (!supportedConversions.TryGetValue(msgPronom, out var pronomList))
                {
                    pronomList = new List<string>();
                    supportedConversions[msgPronom] = pronomList;
                }
                pronomList.AddRange(EMLPronoms);
            }
            return supportedConversions;
        }

        /// <summary>
        /// Get a dictionary of all conversions that blocks multithreading
        /// </summary>
        /// <returns> the list </returns>
        public override Dictionary<string, List<string>> GetListOfBlockingConversions()
        {
            // No blocking conversions for this converter
            return new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Gets the correct command for executing eml to pdf conversion
        /// </summary>
        /// <param name="inputFilePath">Full path to the input file</param>
        /// <param name="workingDirectory">Working directory for the program</param>
        /// <returns>Returns the string with the correct command</returns>
        private string GetEmlToPdfCommand(string inputFilePath, string workingDirectory)
        {
            Version = "'EML to PDF - 2.6.0'";

            // Get correct path to email converter relative to the working directory
            string relativeJarPathWindows = ".\\src\\ConversionTools\\emailconverter-2.6.0-all.jar";
            string relativeJarPathLinux = "./src/ConversionTools/emailconverter-2.6.0-all.jar";
            string jarFile = Environment.OSVersion.Platform == PlatformID.Unix ? Path.Combine(workingDirectory + relativeJarPathLinux)
                                                                               : Path.Combine(workingDirectory + relativeJarPathWindows);
            return Environment.OSVersion.Platform == PlatformID.Unix ? $@"-c java -jar ""{jarFile}"" ""{inputFilePath}"" -a" :
                                                                       $@" /C java -jar ""{jarFile}"" ""{inputFilePath}"" -a";
        }

        /// <summary>
        /// Get Command for MSG to EML for linux
        /// </summary>
        /// <param name="inputFilePath">Full patht o the input path</param>
        /// <returns>Returns the string with the correct command </returns>
        private string GetMsgToEmlCommandUnix(string inputFilePath)
        {
            Version = "'MSG to EML(Linux) - 0.921'";
            return $@"-c msgconvert ""{inputFilePath}"" ";
        }

        /// <summary>
        /// Get command for MSG to EML for Windows
        /// </summary>
        /// <param name="inputFilePath">Full path to the input file </param>
        /// <param name="workingDirectory">Current working directoy</param>
        /// <param name="destinationDir">Destination directory for the output file</param>
        /// <returns>returns the string with the correct command</returns>
        private string GetMsgToEmlCommandWindows(string inputFilePath, string workingDirectory, string destinationDir)
        {
            Version = "'MSG to EML(Windows) - 1.1.0'";
            // Get the correct path to the exe file for the mailcovnerter
            string relativeRebexFilePath = "src\\ConversionTools\\MailConverter.exe";
            string rebexConverterFile = Path.Combine(workingDirectory, relativeRebexFilePath);
            return $@" /C {rebexConverterFile} to-mime --ignore ""{inputFilePath}"" ""{destinationDir}"" ";
        }

        /// <summary>
        /// Get the supported operating system for this converter 
        /// </summary>
        /// <returns>Returns list of strings wiht the supported OS'es</returns>
        public override List<string> GetSupportedOS()
        {
            var supportedOS = new List<string>();
            supportedOS.Add(PlatformID.Win32NT.ToString());
            supportedOS.Add(PlatformID.Unix.ToString());
            return supportedOS;
        }

        /// <summary>
        /// Adds the attachement files to the current working set for conversion
        /// </summary>
        /// <param name="folderWithAttachments"> the folder containing the attachments </param>
        public async Task AddAttachementFilesToWorkingSet(string folderWithAttachments)
        {
            // Identify files in the new attachement folder
            List<FileInfo2>? attachementFiles = await SF.Siegfried.Instance.IdentifyFilesIndividually(folderWithAttachments)!;

            foreach (FileInfo2 newFile in attachementFiles)
            {

                Guid id = Guid.NewGuid();
                newFile.Id = id;
                var newFileToConvert = new FileToConvert(newFile);
                newFileToConvert.TargetPronom = FileConverter.ConversionSettings.GetTargetPronom(newFile)!;
                newFile.AddConversionTool(NameAndVersion);
                // Use current and target pronom to create a key for the conversion map
                var key = new KeyValuePair<string, string>(newFileToConvert.CurrentPronom, newFileToConvert.TargetPronom);
                // If the conversion map contains the key, set the route to the value of the key
                if (ConversionManager.Instance.ConversionMap.TryGetValue(key, out var route))
                {
                    newFileToConvert.Route = new List<string>(route);
                }
                // If the conversion map does not contain the key, set the route to the target pronom
                else if (newFileToConvert.CurrentPronom != newFileToConvert.TargetPronom)
                {
                    newFileToConvert.Route.Add(newFileToConvert.TargetPronom);
                }
                else
                {
                    continue;
                }
                // set the remaining necessary properties for the files and ad them to the working set, FileInfoMap and Files
                newFile.Route = newFileToConvert.Route;
                newFileToConvert.addedDuringRun = true;
                newFile.FilePath = Path.GetRelativePath(Directory.GetCurrentDirectory(),  newFile.FilePath);
               
                ConversionManager.Instance.WorkingSet.TryAdd(id, newFileToConvert);
                ConversionManager.Instance.FileInfoMap.TryAdd(id, newFile);
                FileManager.Instance.Files.TryAdd(newFile.Id, newFile);
            }
        }

        /// <summary>
        /// Checks if the necessary dependencies for the email converter exists
        /// </summary>
        /// <returns> true if it found them </returns>
        private bool CheckDependencies()
        {
            string wkhtmltopdfExecutable = currentOS.Platform == PlatformID.Unix ? "wkhtmltopdf" : "wkhtmltopdf.exe";
            string javaExecutable = currentOS.Platform == PlatformID.Unix ? "java" : "java.exe";
            bool wkhtmltopdfFound = currentOS.Platform == PlatformID.Unix ? CheckPathVariableLinux(wkhtmltopdfExecutable) : CheckPathVariableWindows(wkhtmltopdfExecutable);
            bool javaFound = currentOS.Platform == PlatformID.Unix ? CheckPathVariableLinux(javaExecutable) : CheckPathVariableWindows(javaExecutable);

            return wkhtmltopdfFound && javaFound;
        }

    }
}