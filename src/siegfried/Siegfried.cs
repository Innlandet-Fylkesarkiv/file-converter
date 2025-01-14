﻿using SharpCompress.Archives;
using SharpCompress.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileConverter.HelperClasses;

namespace FileConverter.Siegfried
{
    public class SiegfriedJSON
    {
        [JsonPropertyName("siegfried")]
        public string siegfriedVersion { get; set; } = "";
        [JsonPropertyName("scandate")]
        public string scandate { get; set; } = "";
        [JsonPropertyName("files")]
        public SiegfriedFile[] files { get; set; } = [];
    }

    public class SiegfriedFile
    {
        [JsonPropertyName("filename")]
        public string filename { get; set; } = "";
        [JsonPropertyName("filesize")]
        public long filesize { get; set; } = 0;
        [JsonPropertyName("modified")]
        public string modified { get; set; } = "";
        [JsonPropertyName("errors")]
        public string errors { get; set; } = "";
        public string hash { get; set; } = "";
        [JsonPropertyName("matches")]
        public SiegfriedMatches[] matches { get; set; } = [];
    }
    public class SiegfriedMatches
    {
        [JsonPropertyName("ns")]
        public string ns { get; set; } = "";
        [JsonPropertyName("id")]
        public string id { get; set; } = "";
        [JsonPropertyName("format")]
        public string format { get; set; } = "";
        [JsonPropertyName("version")]
        public string version { get; set; } = "";
        [JsonPropertyName("mime")]
        public string mime { get; set; } = "";
        [JsonPropertyName("class")]
        public string class_ { get; set; } = "";
        [JsonPropertyName("basis")]
        public string basis { get; set; } = "";
        [JsonPropertyName("warning")]
        public string warning { get; set; } = "";
    }

    public class Siegfried
    {
        private static Siegfried? instance;
        public string? Version { get; set; } = null;
        public string? ScanDate { get; set; } = null;
        public string OutputFolder { get; set; } = "siegfried/JSONoutput";
        private string ExecutableName = OperatingSystem.IsLinux() ? "sf" : "sf.exe";
        private string HomeFolder = "siegfried/";
        private readonly string PronomSignatureFile = "default.sig";      //"pronom64k.sig";
        public int Multi { get; set; } = 64;
        public int groupSize { get; set; } = 256;
        private static readonly object lockObject = new object();
        public List<List<string>> CompressedFolders { get; set; }
        public ConcurrentBag<FileInfo2> Files { get; set; } = new ConcurrentBag<FileInfo2>();

        /// <summary>
        /// Makes sure that only one instance of Siegfried is created
        /// </summary>
        public static Siegfried Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new Siegfried();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Constructor for Siegfried
        /// </summary>
        /// <exception cref="FileNotFoundException">Siegfried process not found</exception>
        private Siegfried()
        {
            Logger logger = Logger.Instance;
            CompressedFolders = new List<List<string>>();
            //Look for Siegfried files
            if (OperatingSystem.IsWindows())
            {
                GetExecutable();
                var found = Path.Exists(ExecutableName);
                logger.SetUpRunTimeLogMessage("SF Siegfried executable " + (found ? "" : "not ") + "found", !found);
                if (!found)
                {
                    PrintHelper.PrintLn("Cannot find Siegfried executable", GlobalVariables.ERROR_COL);
                }
                found = Path.Exists(Path.Combine(HomeFolder, PronomSignatureFile));
                logger.SetUpRunTimeLogMessage($"SF Pronom signature file '{PronomSignatureFile}' " + (found ? "" : "not ") + "found", !found);
                if (!found)
                {
                    PrintHelper.PrintLn("Cannot find Pronom signature file", GlobalVariables.ERROR_COL);
                }
                logger.SetUpRunTimeLogMessage("SF Home folder: " + HomeFolder, false);
            }
            else if (OperatingSystem.IsLinux())
            {   
                //Check if Siegfried is installed on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = "-c \" " + "sf -version" + " \"";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;

                try
                {
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    if (!output.Contains("siegfried"))
                    {
                        throw new FileNotFoundException("Cannot find Siegfried on Linux");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Gets the Siegfried executable and the home folder
        /// </summary>
        private void GetExecutable()
        {
            //Linux does not need to find the executable
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            string filename = "sf.exe";
            string[] executables = Directory.GetFiles(Directory.GetCurrentDirectory(), filename, SearchOption.AllDirectories);
            string[] sigFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), PronomSignatureFile, SearchOption.AllDirectories);
            //If the executable is found
            if (executables.Length > 0)
            {
                foreach (string exeFile in executables)
                {
                    foreach (string sigFile in sigFiles)
                    {
                        //Make sure that the sig file is in the same directory as the executable
                        var exeDir = Path.GetDirectoryName(exeFile);
                        if (exeDir != null && exeDir == Path.GetDirectoryName(sigFile))
                        {
                            ExecutableName = exeFile;
                            HomeFolder = Path.Combine(exeDir);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the Siegfried output folder
        /// </summary>
        public void ClearOutputFolder()
        {
            if (Directory.Exists(OutputFolder))
            {
                try
                {
                    Directory.Delete(OutputFolder, true);
                }
                catch
                {
                    Logger logger = Logger.Instance;
                    logger.SetUpRunTimeLogMessage("SF Could not delete Siegfried output folder", true);
                }
            }
        }

        /// <summary>
        /// Converts the hash enum to a string
        /// </summary>
        /// <param name="hash">the hash to be converted</param>
        /// <returns>the hash as a string</returns>
        public static string HashEnumToString(HashAlgorithms hash)
        {
            switch (hash)
            {
                case HashAlgorithms.MD5:
                    return "md5";
                default:
                    return "sha256";
            }
        }

        /// <summary>
        /// Retrieves the third element in the call stack for the method that called this method
        /// </summary>
        /// <param name="stackTrace">Stack trace from the method that calls this method</param>
        /// <returns>String containing third element in call stack or null</returns>
        private static string? RetrieveCallerMethod(StackTrace stackTrace)
        {
            // Get the frame of the method that called MethodA
            StackFrame? callerFrame = stackTrace.GetFrame(2); // Index 2 represents 2 steps up in the callstack frame

            // Get the method name from the caller frame
            string? callerMethodName = null;
            if (callerFrame != null)
            {
                var method = callerFrame.GetMethod();
                if (method != null)
                {
                    callerMethodName = method.Name;
                }
            }
            return callerMethodName;
        }
        /// <summary>
        /// Returns the pronom id of a specified file
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <param name="hash">True if file should be hashed</param>
        /// <returns>Parsed SiegfriedFile or null</returns>
        public SiegfriedFile? IdentifyFile(string path, bool hash)
        {
            // Wrap the file path in quotes
            string wrappedPath = "\"" + path + "\"";
            string options;

            StackTrace stackTrace = new StackTrace();

            string? callerMethodName = RetrieveCallerMethod(stackTrace);

            if (OperatingSystem.IsWindows())
            {
                options = String.Format("-coe -home {0} -json {1} -sig {2} ", Path.Combine(HomeFolder), hash ? "-hash " + HashEnumToString(GlobalVariables.ChecksumHash) : "", PronomSignatureFile);
            }
            else
            {
                options = String.Format("-coe -json {0}", hash ? "-hash " + HashEnumToString(GlobalVariables.ChecksumHash) : "");
            }

            // Define the process start info
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $"{ExecutableName}", // or any other command you want to run
                Arguments = options + wrappedPath,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string error = "";
            string output = "";
            bool exception = false;
            try
            {
                // Create the process
                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();

                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage(String.Format("SF IdentifyFile: {0}" + e.Message, callerMethodName != null ? "(Called by " + callerMethodName + ")" : ""), true);
                exception = true;
            }

            if (error.Length > 0 && !exception)
            {
                Logger.Instance.SetUpRunTimeLogMessage(String.Format("SF IdentifyFile: {0}" + error, callerMethodName != null ? "(Called by " + callerMethodName + ")" : ""), true);
            }
            //Parse the JSON output and return it
            var parsedData = ParseJSONOutput(output, false);
            return ValidateParsedData(parsedData);
        }

        /// <summary>
        /// Validaes if JSON data from SiegFried is valid and returns the file if valid
        /// </summary>
        /// <param name="parsedData">JSON data from Siegfried</param>
        /// <returns>SiegfriedFile if valid JSON data was sent</returns>
        private static SiegfriedFile? ValidateParsedData(SiegfriedJSON? parsedData)
        {
            //Check for null values
            if (parsedData == null || parsedData.files == null)
                return null;
            //Check if there are any files in the JSON data
            if (parsedData.files.Length > 0)
            {
                return parsedData.files[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a SiegfriedFile list of a specified file array
        /// </summary>
        /// <param name="paths">Array of file paths to </param>
        /// <returns>Pronom id or null</returns>
        public List<FileInfo2>? IdentifyList(string[] paths)
        {
            // If no paths are given, return null
            if (paths.Length < 1)
            {
                return null;
            }
            // Wrap the file paths in quotes and join them with a space
            string wrappedPaths = WrapPaths(paths);
            // Define the options for the process
            string options;
            if (OperatingSystem.IsWindows())
            {
                options = String.Format("-home \"{0}\" -json {1} -sig {2} -multi {3} ", Path.Combine(HomeFolder), "-hash " + HashEnumToString(GlobalVariables.ChecksumHash), PronomSignatureFile, Multi);
            }
            else
            {
                options = String.Format("-json {0}", "-hash " + HashEnumToString(GlobalVariables.ChecksumHash));
            }
            // Setup the output JSON file
            string outputFile = Path.Combine(OutputFolder, Guid.NewGuid().ToString() + ".json");
            string? parentDir = Directory.GetParent(outputFile)?.FullName;
            CreateJSONOutputFile(parentDir, outputFile);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $"{ExecutableName}",
                Arguments = options + wrappedPaths,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string error = "";
            bool exception = false;
            try
            {
                // Create the process
                using (Process process = new Process { StartInfo = psi })
                {
                    // Create the StreamWriter to write to the file
                    using (StreamWriter sw = new StreamWriter(outputFile))
                    {
                        // Set the output stream for the process
                        process.OutputDataReceived += (sender, e) => { if (e.Data != null) sw.WriteLine(e.Data); };

                        // Start the process
                        process.Start();

                        // Begin asynchronous read operations for output and error streams
                        process.BeginOutputReadLine();
                        error = process.StandardError.ReadToEnd();

                        // Wait for the process to exit
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception e)
            {
                //Remove \n from error message
                error = e.Message.Replace("\n", " - ");
                Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: " + e.Message, true);
                exception = true;
            }

            if (error.Length > 0 && !exception)
            {
                //Remove \n from error message
                error = error.Replace("\n", " - ");
                Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: " + error, true);
            }
            //Parse the JSON output and return the files
            var parsedData = ParseJSONOutput(outputFile, true);
            return AddFilesAndSetProperties(parsedData, paths);
        }

        /// <summary>
        /// Creates new FileInfo2 objects and populates them with data identified from Siegfried 
        /// </summary>
        /// <param name="parsedData">Data that was parsed from siegfried and will be used for the new files</param>
        /// <param name="paths">Paths to the file locations in output folder</param>
        /// <returns>The new list of FileInfo2 objects with their properties set</returns>
        private List<FileInfo2>? AddFilesAndSetProperties(SiegfriedJSON? parsedData, string[] paths)
        {
            List<FileInfo2>? newFiles = new List<FileInfo2>();
            //Check for null values
            if (parsedData == null)
                return null;
            //Set the version and scandate if they are not set
            if (Version == null || ScanDate == null)
            {
                Version = parsedData.siegfriedVersion;
                ScanDate = parsedData.scandate;
            }
            //Create new FileInfo2 objects and set their properties
            for (int i = 0; i < parsedData.files.Length; i++)
            {
                var file = new FileInfo2(parsedData.files[i], paths[i]);
                //Filepath is overridden to allow special characters in the path
                file.FilePath = paths[i];
                newFiles.Add(file);
            }
            return newFiles;
        }

        /// <summary>
        /// Wraps paths in quotes and joins them with a space
        /// </summary>
        /// <param name="paths"> list of paths to be wrapped </param>
        /// <returns> a string of all paths each wrapped by quotes and separated by spaces </returns>
        private static string WrapPaths(string[] paths)
        {
            string[] tempPaths = new string[paths.Length];
            // Wrap the file paths in quotes
            for (int i = 0; i < paths.Length; i++)
            {
                tempPaths[i] = "\"" + paths[i] + "\"";
            }
            return String.Join(" ", tempPaths);
        }

        /// <summary>
        /// Creates a JSON output file in the specified directory. If the directory does not exist, it is created.
        /// </summary>
        /// <param name="parentDir"> the directory the file is to be created in </param>
        /// <param name="outputFile"> the JSON output file</param>
        static private void CreateJSONOutputFile(string? parentDir, string outputFile)
        {
            try
            {
                //Check if the parent directory exists, if not create it
                if (parentDir != null && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
                //Create the output file
                if (parentDir != null)
                {
                    File.Create(outputFile).Close();
                }
                else
                {
                    Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: parentDir is null " + outputFile, true);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: could not create output file " + e.Message, true);
            }
        }

        /// <summary>
        /// Identifies all files in input directory and returns a List of FileInfo objects.
        /// </summary>
        /// <param name="input">Path to root folder for search</param>
        /// <returns>A List of identified files</returns>
        public Task<List<FileInfo2>>? IdentifyFilesIndividually(string input)
        {
            Logger logger = Logger.Instance;
            var files = new ConcurrentBag<FileInfo2>();
            //Get all file paths in input directory
            List<string> filePaths = new List<string>(Directory.GetFiles(input, "*.*", SearchOption.AllDirectories));
            //Group file paths
            ConcurrentBag<string[]> filePathGroups = new ConcurrentBag<string[]>(GroupPaths(filePaths));
            //For each group of file paths, identify all files
            Parallel.ForEach(filePathGroups, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, filePaths =>
            {
                var output = IdentifyList(filePaths);
                if (output == null)
                {
                    logger.SetUpRunTimeLogMessage("SF IdentifyFilesIndividually: could not identify files", true);
                    return; //Skip current group
                }
                //Add all identified files to the list
                foreach (var f in output)
                {
                    files.Add(f);
                }
            });
            //Return the list of identified files
            return Task.FromResult(files.ToList());
        }

        /// <summary>
        /// Less parallelised version of IdentifyFilesIndividually, but ensures that the given files are correctly updated and the result is tied to the same file ID.
        /// </summary>
        /// <param name="input">Path to root folder for search</param>
        /// <returns>A List of identified files</returns>
        public Task<List<FileInfo2>>? IdentifyFilesIndividually(List<FileInfo2> inputFiles)
        {
            Logger logger = Logger.Instance;
            var files = new ConcurrentBag<FileInfo2>();
            //For each file, identify it
            Parallel.ForEach(inputFiles, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, file =>
            {
                //Skip files that should be merged (they may not exist anymore and are documented in other methods)
                if (file.ShouldMerge || file.IsDeleted)
                {
                    return;
                }
                //Identify the file
                var result = IdentifyFile(file.FilePath, true);
                if (result == null)
                {
                    logger.SetUpRunTimeLogMessage("SF IdentifyFilesIndividually: could not identify file", true, filename: file.FilePath);
                    return; //Skip current file
                }
                //Create a new FileInfo2 object with the identified data
                var newFile = new FileInfo2(result,file.OriginalFilePath);
                newFile.Id = file.Id;
                files.Add(newFile);
            });
            //Return the list of identified files
            return Task.FromResult(files.ToList());
        }

        /// <summary>
        /// Groups paths into groups of a specified size (groupSize)
        /// </summary>
        /// <param name="paths">list of filepaths that need to be grouped</param>
        /// <returns>list of grouped paths</returns>
        public List<string[]> GroupPaths(List<string> paths)
        {
            var filePathGroups = new List<string[]>();
            var tmpGroup = new List<string>();
            var currLength = 0;
            var currNumFiles = 0;
            //Group paths based on string length and number of files
            foreach (var path in paths)
            {
                //If the current group is too large, add it to the list and start a new group
                if (currLength > 13600 || currNumFiles >= groupSize)
                {
                    filePathGroups.Add(tmpGroup.ToArray());
                    tmpGroup.Clear();
                    currLength = 0;
                    currNumFiles = 0;
                }
                //Add the path to the current group
                tmpGroup.Add(path);
                currLength += path.Length;
                currNumFiles++;
            }
            //Add the last group
            if (tmpGroup.Count > 0)
            {
                filePathGroups.Add(tmpGroup.ToArray());
            }
            return filePathGroups;
        }

        /// <summary>
        /// Identifies compressed files, unpacks them and identifies all files in them
        /// </summary>
        /// <returns>All files inside the compressed files</returns>
        public List<FileInfo2> IdentifyCompressedFiles()
        {
            Logger logger = Logger.Instance;
            UnpackCompressedFolders();
            var fileBag = new ConcurrentBag<FileInfo2>();

            //For each compressed folder, identify all files
            Parallel.ForEach(CompressedFolders, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, folders =>
            {
                foreach (string folder in folders)
                {
                    //Identify all file paths in compressed folder and group them
                    var pathWithoutExt = folder.LastIndexOf('.') > -1 ? folder.Substring(0, folder.LastIndexOf('.')) : folder;
                    var paths = Directory.GetFiles(pathWithoutExt, "*.*", SearchOption.TopDirectoryOnly);
                    var filePathGroups = GroupPaths(new List<string>(paths));
                    //Identify all files in each group
                    Parallel.ForEach(filePathGroups, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, paths =>
                    {
                        var files = IdentifyList(paths);
                        if (files != null)
                        {
                            foreach (FileInfo2 file in files)
                            {
                                //TODO: File needs to get original file path from input, not output
                                fileBag.Add(file);
                            }
                        }
                        else
                        {
                            logger.SetUpRunTimeLogMessage("SF IdentifyCompressedFilesJSON: " + folder + " could not be identified", true);
                        }
                    });
                }
            });
            return fileBag.ToList();
        }

        /// <summary>
        /// Parses the JSON output from Siegfried
        /// </summary>
        /// <param name="json">path to JSON file or contents of the JSON file</param>
        /// <param name="readFromFile">if it is a JSON file or contents of a JSON file being sent as input</param>
        /// <returns>The parsed JSON as a siegfried JSON</returns>
        public static SiegfriedJSON? ParseJSONOutput(string json, bool readFromFile)
        {
            try
            {
                SiegfriedJSON siegfriedJson;
                FileStream? file = null;
                //If the input is a file, open it
                if (readFromFile)
                {
                    file = File.OpenRead(json);
                }
                //If the file could not be opened, return null
                if (readFromFile && file == null)
                {
                    Logger.Instance.SetUpRunTimeLogMessage("SF ParseJSON: file not found", true);
                    return null;
                }
                //Parse the JSON data
                using (JsonDocument document = readFromFile ? JsonDocument.Parse(file!) : JsonDocument.Parse(json))
                {
                    // Access the root of the JSON document
                    JsonElement root = document.RootElement;

                    // Deserialize JSON into a SiegfriedJSON object
                    siegfriedJson = new SiegfriedJSON
                    {
                        siegfriedVersion = root.GetProperty("siegfried").GetString() ?? "",
                        scandate = root.GetProperty("scandate").GetString() ?? "",
                        files = root.GetProperty("files").EnumerateArray()
                            .Select(fileElement => ParseSiegfriedFile(fileElement))
                            .ToArray()
                    };
                }
                //Close the file if it was opened
                if (readFromFile && file != null)
                {
                    file.Close();
                }
                return siegfriedJson;
            }
            catch (Exception e)
            {
                Console.WriteLine("Siegfried had an error parsing JSON data, this is most likely because a fatal error happened in Siegfried: " + e.Message);
                Logger.Instance.SetUpRunTimeLogMessage("SF ParseJSON: " + e.Message, true);
                return null;
            }
        }

        /// <summary>
        /// Parses the JSONElement file to a Siegfried file
        /// </summary>
        /// <param name="fileElement">JSONElement file</param>
        /// <returns>parsed file as a SiegfriedFile</returns>
        public static SiegfriedFile ParseSiegfriedFile(JsonElement fileElement)
        {
            //Get the hash method
            string hashMethod = HashEnumToString(GlobalVariables.ChecksumHash);
            JsonElement jsonElement;
            //Return the parsed file
            return new SiegfriedFile
            {
                filename = fileElement.GetProperty("filename").GetString() ?? "",

                hash = fileElement.TryGetProperty(hashMethod, out jsonElement) ? fileElement.GetProperty(hashMethod).GetString() ?? "" : "",
                filesize = fileElement.GetProperty("filesize").GetInt64(),
                modified = fileElement.GetProperty("modified").GetString() ?? "",
                errors = fileElement.GetProperty("errors").GetString() ?? "",
                matches = fileElement.GetProperty("matches").EnumerateArray()
                    .Select(matchElement => ParseSiegfriedMatches(matchElement))
                    .ToArray()
            };
        }

        /// <summary>
        /// Parses the JSONElement match to a Siegfried match
        /// </summary>
        /// <param name="matchElement">The JSONElement match</param>
        /// <returns>parsed file as a SiegfriedMatches</returns>
        public static SiegfriedMatches ParseSiegfriedMatches(JsonElement matchElement)
        {
            return new SiegfriedMatches
            {
                ns = matchElement.GetProperty("ns").GetString() ?? "",
                id = matchElement.GetProperty("id").GetString() ?? "",
                format = matchElement.GetProperty("format").GetString() ?? "",
                version = matchElement.GetProperty("version").GetString() ?? "",
                mime = matchElement.GetProperty("mime").GetString() ?? "",
                class_ = matchElement.GetProperty("class").GetString() ?? "",
                basis = matchElement.GetProperty("basis").GetString() ?? "",
                warning = matchElement.GetProperty("warning").GetString() ?? ""
            };
        }

        /// <summary>
        /// Copies all files (while retaining file structure) from a source directory to a destination directory
        /// </summary>
        /// <param name="source">source directory</param>
        /// <param name="destination">destination directory</param>
        public static void CopyFiles(string source, string destination)
        {
            string[] files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            List<string> retryFiles = new List<string>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, file =>
            {
                string relativePath = file.Replace(source, "");
                string outputPath = destination + relativePath;
                string outputFolder = outputPath.Substring(0, outputPath.LastIndexOf(Path.DirectorySeparatorChar));

                //If file already exists in target destination, skip it
                if (File.Exists(outputPath))
                {
                    return;
                }
                //If the folder does not exist, create it
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                //Try to copy the file, if it fails, add it to the retry list
                try
                {
                    File.Copy(file, outputPath, true);
                }
                catch (IOException)
                {
                    Console.WriteLine("Could not open file '{0}', it may be used in another process");
                    retryFiles.Add(file);
                }
            });
            //Retry copying the files that failed
            if (retryFiles.Count > 0)
            {
                Console.WriteLine("Some files could not be copied, close the processes using them and hit enter");
                Console.ReadLine();
                Parallel.ForEach(retryFiles, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, file =>
                {
                    string relativePath = file.Replace(source, "");
                    string outputPath = destination + relativePath;
                    string outputFolder = outputPath.Substring(0, outputPath.LastIndexOf('\\'));

                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                    try
                    {
                        File.Copy(file, outputPath, true);
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.SetUpRunTimeLogMessage("SF CopyFiles: " + e.Message, true);
                    }
                });
            }
        }

        /// <summary>
        /// Compresses all folders in output directory
        /// </summary>
        public void CompressFolders()
        {
            //For each list of folders that were unpacked, compress them
            CompressedFolders.ForEach(folders =>
            {
                //Compress in reverse order to avoid compressing a folder that contains an uncompressed folder that should be compressed
                folders.Reverse();
                foreach (string folder in folders)
                {
                    var extention = Path.GetExtension(folder);
                    //Switch for different compression formats
                    switch (extention)
                    {
                        case ".zip":
                            CompressFolder(folder, ArchiveType.Zip);
                            break;
                        case ".tar":
                            CompressFolder(folder, ArchiveType.Tar);
                            break;
                        case ".gz":
                            CompressFolder(folder, ArchiveType.GZip);
                            break;
                        case ".rar":
                            CompressFolder(folder, ArchiveType.Rar);
                            break;
                        case ".7z":
                            CompressFolder(folder, ArchiveType.SevenZip);
                            break;
                        default:
                            //Do nothing
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// Unpacks all compressed folders in output directory
        /// </summary>
        public void UnpackCompressedFolders()
        {
            //Identify all files in output directory
            var compressedFoldersOutput = GetCompressedFolders(GlobalVariables.ParsedOptions.Output);
            var compressedFoldersInput = GetCompressedFolders(GlobalVariables.ParsedOptions.Input);

            // Remove root path from all paths in compressedFoldersInput
            var inputWithoutRoot = compressedFoldersInput.Select(file =>
            {
                int index = file.IndexOf(GlobalVariables.ParsedOptions.Input);
                return index >= 0 ? string.Concat(file.AsSpan(0, index), file.AsSpan(index + GlobalVariables.ParsedOptions.Input.Length)) : file;
            }).ToList();

            // Remove root path from all paths in compressedFoldersOutput
            var outputWithoutRoot = compressedFoldersOutput.Select(file =>
            {
                int index = file.IndexOf(GlobalVariables.ParsedOptions.Output);
                return index >= 0 ? string.Concat(file.AsSpan(0, index), file.AsSpan(index + GlobalVariables.ParsedOptions.Output.Length)) : file;
            }).ToList();

            //Remove all folders that are not in input directory
            foreach (string folder in outputWithoutRoot)
            {
                if (!inputWithoutRoot.Contains(folder))
                {
                    compressedFoldersOutput.Remove(GlobalVariables.ParsedOptions.Output + folder);
                }
            }

            ConcurrentBag<List<string>> allUnpackedFolders = new ConcurrentBag<List<string>>();
            //In Parallel: Unpack compressed folders recursively and delete the compressed folder
            compressedFoldersOutput.ForEach(root =>
            {
                allUnpackedFolders.Add(UnpackRecursively(root));
            });
            //Store the unpacked folders
            foreach (List<string> folder in allUnpackedFolders)
            {
                CompressedFolders.Add(folder);
            }
        }

        /// <summary>
        /// Unpacks a compressed folder recursively
        /// </summary>
        /// <param name="root">the compressed folder</param>
        /// <returns>list of paths to the unpacked directories</returns>
        private static List<string> UnpackRecursively(string root)
        {
            List<string> unpackedFolders = new List<string>();
            //Unpack the folder
            UnpackFolder(root);
            unpackedFolders.Add(root);
            //Get all subfolders in the unpacked folder
            var extractedFolder = root.LastIndexOf('.') > -1 ? root.Substring(0, root.LastIndexOf('.')) : root;
            var subFolders = GetCompressedFolders(extractedFolder);
            //Unpack all subfolders
            foreach (string folder in subFolders)
            {
                var path = Path.Combine(extractedFolder, Path.GetFileName(folder));
                unpackedFolders.AddRange(UnpackRecursively(path));
            }
            //Return the list of unpacked folders
            return unpackedFolders;
        }

        /// <summary>
        /// Gets all compressed folders in a directory
        /// </summary>
        /// <param name="dir">the parent directory</param>
        /// <returns>list of paths to all compressed folders</returns>
        private static List<string> GetCompressedFolders(string dir)
        {
            //Check if the directory exists
            if (!Directory.Exists(dir))
            {
                return new List<string>();
            }
            //Get all files in the directory with ending .zip, .tar, .gz, .rar or .7z
            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).ToList();
            var compressedFolders = files.FindAll(file => file.EndsWith(".zip") || file.EndsWith(".tar") || file.EndsWith(".gz") || file.EndsWith(".rar") || file.EndsWith(".7z"));
            return compressedFolders;
        }

        /// <summary>
        /// Compresses a folder to a specified format and deletes the unpacked folder
        /// </summary>
        /// <param name="archiveType">Format for compression</param>
        /// <param name="path">Path to folder to be compressed</param>
        private static void CompressFolder(string path, ArchiveType archiveType)
        {
            try
            {
                // Get the file extension and the path without the extension
                string fileExtension = Path.GetExtension(path);
                int lastIndexOf = path.LastIndexOf(fileExtension);
                string pathWithoutExtension = lastIndexOf > -1 ? path.Substring(0, lastIndexOf) : path;
                // Compress the folder
                using (var archive = ArchiveFactory.Create(archiveType))
                {
                    archive.AddAllFromDirectory(pathWithoutExtension);
                    archive.SaveTo(path, CompressionType.None);
                }
                // Delete the unpacked folder
                Directory.Delete(pathWithoutExtension, true);
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("SF CompressFolder " + e.Message, true);
            }
        }

        /// <summary>
        /// Unpacks a compressed folder regardless of format
        /// </summary>
        /// <param name="path">Path to compressed folder</param>
        private static void UnpackFolder(string path)
        {
            // Get path to folder without extention
            string pathWithoutExtension = path.LastIndexOf('.') > -1 ? path.Substring(0, path.LastIndexOf('.')) : path;
            // Ensure the extraction directory exists
            if (!Directory.Exists(pathWithoutExtension))
            {
                Directory.CreateDirectory(pathWithoutExtension);
            }
            try
            {
                // Extract the contents of the compressed file
                using (var archive = ArchiveFactory.Open(path))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            entry.WriteToDirectory(pathWithoutExtension, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
                // Delete the compressed file
                File.Delete(path);
            }
            //If the folder is encrypted, catch the exception
            catch (CryptographicException)
            {
                Logger.Instance.SetUpRunTimeLogMessage("SF UnpackFolder " + path + " is encrypted", true);
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("SF UnpackFolder " + e.Message, true);
            }
        }
    }
}