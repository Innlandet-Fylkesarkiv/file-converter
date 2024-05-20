using System.Xml;
using FileConverter.HelperClasses;
using FileConverter.Managers;

namespace FileConverter
{
    public class ConversionSettingsData
    {
        // List of input pronom codes
        public List<string> PronomsList { get; set; } = new List<string>();
        // Whether to merge images or not
        public bool Merge { get; set; } = false;
        // Default type of the FileClass
        public string ClassName { get; set; } = "";

        // Name of the FileTypes
        public string FormatName { get; set; } = "";
        // Name of the FileClass
        public string ClassDefault { get; set; } = "";
        // Default type of the FileTypes
        public string DefaultType { get; set; } = "";
        // do not convert when set to true
        public bool DoNotConvert { get; set; } = false;
    }
    class ConversionSettings
    {
        private static ConversionSettings? instance;
        private static readonly object lockObject = new object();

        /// <summary>
        /// Make sure that only one instance of ConversionSettings is created
        /// </summary>
        public static ConversionSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new ConversionSettings();
                        }
                    }
                }
                return instance;
            }
        }

        internal static readonly char[] separator = [','];

        /// <summary>
        /// Reads ConversionSettings from file
        /// </summary>
        /// <param name="pathToConversionSettings"> the path to the ConversionSettings file from working directory </param>
        public static void ReadConversionSettings(string pathToConversionSettings)
        {
            Logger logger = Logger.Instance;
            //Reset the global variables
            GlobalVariables.Reset();

            try
            {
                // Load the XML document from a file
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(pathToConversionSettings);

                if (xmlDoc.DocumentElement == null) { logger.SetUpRunTimeLogMessage("Could not find ConversionSettings file", true, filename: pathToConversionSettings); return; }

                // Access the root element
                XmlNode? root = xmlDoc.SelectSingleNode("/root");
                if (root == null) { logger.SetUpRunTimeLogMessage("Could not find root", true, filename: pathToConversionSettings); return; }

                SetUpMetadata(root);

                // Access elements and attributes
                XmlNodeList? classNodes = root?.SelectNodes("FileClass");
                if (classNodes == null) { logger.SetUpRunTimeLogMessage("Could not find any classNodes", true, filename: pathToConversionSettings); return; }
                
                // Loop through each class node and handle the file types
                foreach (XmlNode classNode in classNodes)
                {
                    string? defaultType = classNode.SelectSingleNode("Default")?.InnerText;

                    if (defaultType == null)
                    {
                        defaultType = "fmt/477"; // Default to PDF/A-2b
                        PrintHelper.PrintLn("Could not find default type for FileClass, setting to PDF/A-2b", ConsoleColor.Red);
                    }
                    XmlNodeList? fileTypeNodes = classNode?.SelectNodes("FileTypes");
                    if (fileTypeNodes == null)
                    {
                        logger.SetUpRunTimeLogMessage("Settings: Could not find any FileType nodes in FileClass", true, filename: pathToConversionSettings);
                        continue;
                    }
                    // Loop through each file type node and handle them
                    foreach (XmlNode fileTypeNode in fileTypeNodes)
                    {
                        HandleFileTypeNode(fileTypeNode, defaultType);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.SetUpRunTimeLogMessage(ex.Message, true);
            }
        }

        /// <summary>
        /// Sets up global variables from the ConversionSettings file
        /// </summary>
        /// <param name="root">Root node in settings file</param>
        private static void SetUpMetadata(XmlNode root)
        {
            XmlNode? requesterNode = root.SelectSingleNode("Requester");
            XmlNode? converterNode = root.SelectSingleNode("Converter");
            XmlNode? inputNode = root.SelectSingleNode("InputFolder");
            XmlNode? outputNode = root.SelectSingleNode("OutputFolder");
            XmlNode? maxThreadsNode = root.SelectSingleNode("MaxThreads");
            XmlNode? timeoutNode = root.SelectSingleNode("Timeout");
            XmlNode? maxFileSizeNode = root.SelectSingleNode("MaxFileSize");

            string? requester = requesterNode?.InnerText.Trim();
            string? converter = converterNode?.InnerText.Trim();
            if (!String.IsNullOrEmpty(requester))
            {
                Logger.JsonRoot.Requester = requester;
            }
            if (!String.IsNullOrEmpty(converter))
            {
                Logger.JsonRoot.Converter = converter;
            }
            string? input = inputNode?.InnerText.Trim();
            string? output = outputNode?.InnerText.Trim();
            if (!String.IsNullOrEmpty(input))
            {
                GlobalVariables.ParsedOptions.Input = input;
            }
            if (!String.IsNullOrEmpty(output))
            {
                GlobalVariables.ParsedOptions.Output = output;
            }

            string? inputMaxThreads = maxThreadsNode?.InnerText;
            if (!String.IsNullOrEmpty(inputMaxThreads) && int.TryParse(inputMaxThreads, out int maxThreads))
            {
                GlobalVariables.MaxThreads = maxThreads > 0 ? maxThreads : Environment.ProcessorCount * 2;
            }

            string? inputTimeout = timeoutNode?.InnerText;
            if (!String.IsNullOrEmpty(inputTimeout) && int.TryParse(inputTimeout, out int timeout))
            {
                GlobalVariables.Timeout = timeout;
            }

            string? inputMaxFileSize = maxFileSizeNode?.InnerText;
            if (!String.IsNullOrEmpty(inputMaxFileSize) && double.TryParse(inputMaxFileSize, out double maxFileSize))
            {
                GlobalVariables.MaxFileSize = maxFileSize;
            }

            string? checksumHashing = root?.SelectSingleNode("ChecksumHashing")?.InnerText;
            if (checksumHashing != null)
            {
                checksumHashing = checksumHashing.ToUpper().Trim();
                GlobalVariables.ChecksumHash = checksumHashing switch
                {
                    "MD5" => HashAlgorithms.MD5,
                    _ => HashAlgorithms.SHA256,
                };
            }
        }

        /// <summary>
        /// Handles a FileType node from the ConversionSettings file
        /// </summary>
        /// <param name="fileTypeNode">Node that should be parsed</param>
        /// <param name="defaultType">default type for FileClass</param>
        static void HandleFileTypeNode(XmlNode fileTypeNode, string defaultType)
        {
            string? pronoms = fileTypeNode.SelectSingleNode("Pronoms")?.InnerText;
            string? innerDefault = fileTypeNode.SelectSingleNode("Default")?.InnerText;
            string? doNotConvert = fileTypeNode.SelectSingleNode("DoNotConvert")?.InnerText.ToUpper().Trim();
            if (String.IsNullOrEmpty(innerDefault))
            {
                innerDefault = defaultType;
            }

            // Remove whitespace and split pronoms string by commas into a list of strings
            List<string> pronomsList = [];
            if (!string.IsNullOrEmpty(pronoms))
            {
                pronomsList.AddRange(pronoms.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(pronom => pronom.Trim()));
            }
            ConversionSettingsData ConversionSettings = new ConversionSettingsData
            {
                PronomsList = pronomsList,
                DefaultType = innerDefault,
                DoNotConvert = doNotConvert == "YES",
            };
            // Add the beginning and end of routes for the file type
            foreach (string pronom in ConversionSettings.PronomsList)
            {
                if (ConversionSettings.DoNotConvert)
                {
                    GlobalVariables.FileConversionSettings.TryAdd(pronom, pronom);
                }
                else
                {
                    GlobalVariables.FileConversionSettings.TryAdd(pronom, ConversionSettings.DefaultType);
                }
            }
        }

        /// <summary>
        /// Sets up the FolderOverride Dictionary
        /// </summary>
        /// <param name="pathToConversionSettings"> relative path to ConversionSettings file from working directory </param>
        public static void SetUpFolderOverride(string pathToConversionSettings)
        {
            Logger logger = Logger.Instance;
            try
            {
                // Load the XML document from a file
                XmlDocument xmlDoc = new();
                xmlDoc.Load(pathToConversionSettings);

                if (xmlDoc.DocumentElement == null) { logger.SetUpRunTimeLogMessage("Could not find ConversionSettings file", true, filename: pathToConversionSettings); return; }

                XmlNodeList? folderOverrideNodes = xmlDoc.SelectNodes("/root/FolderOverride");
                if (folderOverrideNodes == null)
                {
                    return;
                }
                
                foreach (XmlNode folderOverrideNode in folderOverrideNodes)
                {
                   HandleFolderOverrideNode(folderOverrideNode);
                }
            }
            catch (Exception ex)
            {
                logger.SetUpRunTimeLogMessage(ex.Message, true);
            }
        }

        /// <summary>
        /// Tries to parse a FolderOverride node and add it to the FolderOverride dictionary
        /// </summary>
        /// <param name="folderOverrideNode">The node that should be parsed</param>
        private static void HandleFolderOverrideNode(XmlNode folderOverrideNode)
        {
            string? folderPath = folderOverrideNode.SelectSingleNode("FolderPath")?.InnerText;
            string? pronoms = folderOverrideNode.SelectSingleNode("Pronoms")?.InnerText;
            string? merge = folderOverrideNode.SelectSingleNode("MergeImages")?.InnerText.ToUpper().Trim();
            string inputFolder = GlobalVariables.ParsedOptions.Input;

            List<string> pronomsList = new List<string>();
            if (!string.IsNullOrEmpty(pronoms))
            {
                pronomsList.AddRange(pronoms.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(pronom => pronom.Trim()));
            }

            var defaultType = folderOverrideNode.SelectSingleNode("ConvertTo")?.InnerText ?? "";
            defaultType = defaultType.Replace("\n", "").Replace("\r","").Replace("\t","").Replace(" ","");
            ConversionSettingsData ConversionSettings = new ConversionSettingsData
            {
                PronomsList = pronomsList,
                DefaultType = defaultType,
                Merge = merge == "YES",
            };

            if (String.IsNullOrEmpty(folderPath) || String.IsNullOrEmpty(pronoms) || String.IsNullOrEmpty(ConversionSettings.DefaultType)
                || !Directory.Exists(Path.Combine(inputFolder, folderPath)))
            {
                Logger.Instance.SetUpRunTimeLogMessage("something went wrong with parsing a folderOverride in ConversionSettings", true);
                return;
            }

            // Ensure that the folder path is valid on all operating systems
            folderPath = folderPath.Replace('\\', Path.DirectorySeparatorChar);
            folderPath = folderPath.Replace('/', Path.DirectorySeparatorChar);
            GlobalVariables.FolderOverride.Add(folderPath, ConversionSettings);
            List<string> subfolders = GetSubfolderPaths(Path.Combine(GlobalVariables.ParsedOptions.Output,folderPath));

            foreach (string subfolder in subfolders)
            {
                var adjustedPath = Path.GetRelativePath(GlobalVariables.ParsedOptions.Output, subfolder);
                GlobalVariables.FolderOverride.TryAdd(adjustedPath, ConversionSettings);
            }
        }

        /// <summary>
        /// Recursively retrieves all subfolders of a given parent folder.
        /// </summary>
        /// <param name="folderName">the name of the parent folder</param>
        /// <returns>list with paths to all subfolders</returns>
        private static List<string> GetSubfolderPaths(string folderName)
        {
            //Remove output path from folderName
            string relativePath = folderName;//folderName.Replace(GlobalVariables.ParsedOptions.Output, "");
            List<string> subfolders = [];
            //subfolders.Add(folderName);
            try
            {
                subfolders.Add(folderName);
                string targetFolderPath = relativePath;//Path.Combine(GlobalVariables.ParsedOptions.Output, relativePath);
                var subfolderpaths = Directory.GetDirectories(targetFolderPath);

                // Recursively get subfolders of each subfolder
                foreach (string subfolder in subfolderpaths)
                {
                    subfolders.AddRange(GetSubfolderPaths(subfolder));
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Instance.SetUpRunTimeLogMessage("You do not have permission to access this folder", true, filename: folderName);
            }

            return subfolders;
        }

        /// <summary>
        /// Gets the target PRONOM for a file
        /// </summary>
        /// <param name="f"> the file</param>
        /// <returns> the target PRONOM if it found it </returns>
        public static string? GetTargetPronom(FileInfo2 f)
        {
            if (f.IsPartOfSplit)
            {
                f = FileManager.Instance.GetFile(f.Parent) ?? f;
            }
            //Get the parent directory of the file
            var parentDir = Path.GetDirectoryName(Path.GetRelativePath(GlobalVariables.ParsedOptions.Output, f.FilePath));
            //If the file is in a folder that has a folder override, check if the file is at the correct output format for that folder
            if (parentDir != null && GlobalVariables.FolderOverride.TryGetValue(parentDir, out var folderOverride) &&
                folderOverride.PronomsList.Contains(f.OriginalPronom))
            {
                return folderOverride.DefaultType;
            }
            //Otherwise, check if the new type matches the global ConversionSettings for the input format
            if (GlobalVariables.FileConversionSettings.TryGetValue(f.OriginalPronom, out var conversionSetting))
            {
                return conversionSetting;
            }
            return null;
        }

        /// <summary>
        /// Checks if a file should be merged
        /// </summary>
        /// <param name="f">The file that should be checked</param>
        /// <returns>True if it should be merged, otherwise False</returns>
        public static bool ShouldMerge(FileInfo2 f)
        {
            var parentDir = Path.GetDirectoryName(Path.GetRelativePath(GlobalVariables.ParsedOptions.Output, f.FilePath));
            if (parentDir != null && GlobalVariables.FolderOverride.TryGetValue(parentDir, out var folderOverride))
            {
                return folderOverride.Merge;
            }
            return false;
        }
    }
}