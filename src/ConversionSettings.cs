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
                // Access the Requester and Converter elements
                XmlNode? requesterNode = root?.SelectSingleNode("Requester");
                XmlNode? converterNode = root?.SelectSingleNode("Converter");
                XmlNode? inputNode = root?.SelectSingleNode("InputFolder");
                XmlNode? outputNode = root?.SelectSingleNode("OutputFolder");
                XmlNode? maxThreadsNode = root?.SelectSingleNode("MaxThreads");
                XmlNode? timeoutNode = root?.SelectSingleNode("Timeout");
                XmlNode? maxFileSizeNode = root?.SelectSingleNode("MaxFileSize");

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
                    GlobalVariables.MaxThreads = maxThreads;
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
                    switch (checksumHashing)
                    {
                        case "MD5": GlobalVariables.ChecksumHash = HashAlgorithms.MD5; break;
                        default: GlobalVariables.ChecksumHash = HashAlgorithms.SHA256; break;
                    }
                }

                // Access elements and attributes
                XmlNodeList? classNodes = root?.SelectNodes("FileClass");
                if (classNodes == null) { logger.SetUpRunTimeLogMessage("Could not find any classNodes", true, filename: pathToConversionSettings); return; }
                foreach (XmlNode classNode in classNodes)
                {
                    string? defaultType = classNode?.SelectSingleNode("Default")?.InnerText;

                    if (defaultType == null)
                    {
                        //TODO: This should not be thrown, but rather ask the user for a default type
                        throw new Exception("No default type found in ConversionSettings");
                    }
                    XmlNodeList? fileTypeNodes = classNode?.SelectNodes("FileTypes");
                    if (fileTypeNodes != null)
                    {
                        foreach (XmlNode fileTypeNode in fileTypeNodes)
                        {
                            string? extension = fileTypeNode.SelectSingleNode("Filename")?.InnerText;
                            string? pronoms = fileTypeNode.SelectSingleNode("Pronoms")?.InnerText;
                            string? innerDefault = fileTypeNode.SelectSingleNode("Default")?.InnerText;
                            string? doNotConvert = fileTypeNode.SelectSingleNode("DoNotConvert")?.InnerText.ToUpper().Trim();
                            if (String.IsNullOrEmpty(innerDefault))
                            {
                                innerDefault = defaultType;
                            }
                            else
                            {
                                Console.Write("");
                            }

                            // Remove whitespace and split pronoms string by commas into a list of strings
                            List<string> pronomsList = new List<string>();
                            if (!string.IsNullOrEmpty(pronoms))
                            {
                                pronomsList.AddRange(pronoms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                             .Select(pronom => pronom.Trim()));
                            }
                            ConversionSettingsData ConversionSettings = new ConversionSettingsData
                            {
                                PronomsList = pronomsList,
                                DefaultType = innerDefault,
                                DoNotConvert = doNotConvert == "YES",
                            };
                            if (ConversionSettings.PronomsList.Count > 0)
                            {
                                foreach (string pronom in ConversionSettings.PronomsList)
                                {
                                    if (ConversionSettings.DoNotConvert)
                                    {
                                        GlobalVariables.FileConversionSettings[pronom] = pronom;
                                    }
                                    else
                                    {
                                        GlobalVariables.FileConversionSettings[pronom] = ConversionSettings.DefaultType;
                                    }
                                }
                            }
                            else
                            {
                                logger.SetUpRunTimeLogMessage("Could not find any pronoms to convert to " + extension, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.SetUpRunTimeLogMessage(ex.Message, true);
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
                string inputFolder = GlobalVariables.ParsedOptions.Input;
                // Load the XML document from a file
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(pathToConversionSettings);

                if (xmlDoc.DocumentElement == null) { logger.SetUpRunTimeLogMessage("Could not find ConversionSettings file", true, filename: pathToConversionSettings); return; }

                XmlNodeList? folderOverrideNodes = xmlDoc.SelectNodes("/root/FolderOverride");
                if (folderOverrideNodes != null)
                {
                    foreach (XmlNode folderOverrideNode in folderOverrideNodes)
                    {
                        string? folderPath = folderOverrideNode.SelectSingleNode("FolderPath")?.InnerText;
                        string? pronoms = folderOverrideNode.SelectSingleNode("Pronoms")?.InnerText;
                        string? merge = folderOverrideNode.SelectSingleNode("MergeImages")?.InnerText.ToUpper().Trim();

                        List<string> pronomsList = new List<string>();
                        if (!string.IsNullOrEmpty(pronoms))
                        {
                            pronomsList.AddRange(pronoms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(pronom => pronom.Trim()));
                        }

                        ConversionSettingsData ConversionSettings = new ConversionSettingsData
                        {
                            PronomsList = pronomsList,
                            DefaultType = folderOverrideNode.SelectSingleNode("ConvertTo")?.InnerText ?? "",
                            Merge = merge == "YES",
                        };

                        bool folderPathEmpty = String.IsNullOrEmpty(folderPath);
                        bool pronomsEmpty = String.IsNullOrEmpty(pronoms);
                        bool convertToEmpty = String.IsNullOrEmpty(ConversionSettings.DefaultType);

                        if (folderPathEmpty || pronomsEmpty || convertToEmpty)
                        {
                            logger.SetUpRunTimeLogMessage("something wrong with a folderOverride in ConversionSettings", true);
                        }
                        else
                        {
                            if (Directory.Exists(Path.Combine(inputFolder, folderPath)))
                            {
                                // Ensure that the folder path is valid on all operating systems
                                folderPath = folderPath.Replace('\\', Path.DirectorySeparatorChar);
                                folderPath = folderPath.Replace('/', Path.DirectorySeparatorChar);
                                GlobalVariables.FolderOverride.Add(folderPath, ConversionSettings);
                                List<string> subfolders = GetSubfolderPaths(folderPath);
                                if (subfolders.Count > 0)
                                {
                                    foreach (string subfolder in subfolders)
                                    {
                                        // Check if the subfolder is already in the FolderOverride Map
                                        if (!GlobalVariables.FolderOverride.ContainsKey(subfolder))
                                        {
                                            GlobalVariables.FolderOverride[subfolder] = ConversionSettings;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.SetUpRunTimeLogMessage(ex.Message, true);
            }
        }
        /// <summary>
        /// Recursively retrieves all subfolders of a given parent folder.
        /// </summary>
        /// <param name="folderName">the name of the parent folder</param>
        /// <returns>list with paths to all subfolders</returns>
        private static List<string> GetSubfolderPaths(string folderName)
        {
            string outputPath = GlobalVariables.ParsedOptions.Output;
            List<string> subfolders = new List<string>();

            try
            {
                string targetFolderPath = Path.Combine(outputPath, folderName);

                if (Directory.Exists(targetFolderPath))
                {
                    // Add current folder to subfolders list

                    // Add immediate subfolders
                    foreach (string subfolder in Directory.GetDirectories(targetFolderPath))
                    {
                        // Recursively get subfolders of each subfolder
                        subfolders.AddRange(GetSubfolderPaths(Path.Combine(folderName, Path.GetFileName(subfolder))));
                    }
                }
                else
                {
                    Console.WriteLine($"Folder '{folderName}' does not exist in '{outputPath}'");
                    Logger.Instance.SetUpRunTimeLogMessage($"Folder '{folderName}' does not exist in '{outputPath}'", true, filename: folderName);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Instance.SetUpRunTimeLogMessage("You do not have permission to access this folder", true, filename: outputPath);
            }

            return subfolders;
        }

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