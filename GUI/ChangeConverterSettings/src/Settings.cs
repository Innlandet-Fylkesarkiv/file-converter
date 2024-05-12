using Avalonia.Logging;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System;
using System.Xml;
using ChangeConverterSettings;
using System.Linq;
using Avalonia.Controls;

/// <summary>
/// Class to hold information about the settings of one file type
/// </summary>
public class SettingsData
{
    // List of input pronom codes
    public List<string> PronomsList { get; set; } = new List<string>();
    // Whether to merge images or not
    public bool Merge { get; set; } = false;
    public string ClassName { get; set; } = "";
    // Default type of the FileClass
    // Name of the FileTypes
    public string FormatName { get; set; } = "";
    // Name of the FileClass
    public string ClassDefault { get; set; } = "";
    // Default type of the FileTypes
    public string DefaultType { get; set; } = "";
    // do not convert when set to true
    public bool DoNotConvert { get; set; } = false;
}
class Settings
{
    private static Settings? instance;
    private static readonly object lockObject = new object();
    internal static readonly char[] separator = [','];

    /// <summary>
    /// Makes sure that only one instance of the settings is created
    /// </summary>
    public static Settings Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new Settings();
                    }
                }
            }
            return instance;
        }
    }    

    /// <summary>
    /// Run all the methods to read the settings from the settings file
    /// </summary>
    /// <param name="pathToSettings"> both full-path and dynamic-path from the working directory works </param>
    public static void ReadAllSettings(string pathToSettings)
    {
        ReadSettings(pathToSettings);
        SetUpFolderOverride(pathToSettings);
    }

    /// <summary>
    /// Reads settings from file
    /// </summary>
    /// <param name="pathToSettings"> the path to the settings file from working directory </param>
    private static void ReadSettings(string pathToSettings)
    {
        Logger logger = Logger.Instance;
        try
        {
            // Load the XML document from a file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathToSettings);

            if (xmlDoc.DocumentElement == null) { logger.SetUpRunTimeLogMessage("Could not find settings file", true, filename: pathToSettings); return; }

            // Access the root element
            XmlNode? root = xmlDoc.SelectSingleNode("/root");
            if (root == null) { logger.SetUpRunTimeLogMessage("Could not find root", true, filename: pathToSettings); return; }
            
            SetUpMetadata(root);

            // Access elements and attributes
            XmlNodeList? classNodes = root?.SelectNodes("FileClass");
            if (classNodes == null) { logger.SetUpRunTimeLogMessage("Could not find any classNodes", true, filename: pathToSettings); return; }
            foreach (XmlNode classNode in classNodes)
            {
                string? className = classNode?.SelectSingleNode("ClassName")?.InnerText; 
                string? defaultType = classNode?.SelectSingleNode("Default")?.InnerText;

                if (defaultType == null)
                {
                    defaultType = "fmt/477"; // Default to PDF/A-2b
                }
                XmlNodeList? fileTypeNodes = classNode?.SelectNodes("FileTypes");
                if (fileTypeNodes == null)
                {
                    logger.SetUpRunTimeLogMessage("Could not find any fileTypeNodes", true);
                    continue;
                }

                foreach (XmlNode fileTypeNode in fileTypeNodes)
                {
                    HandleFileTypeNode(fileTypeNode, defaultType, className);
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
            GlobalVariables.requester = requester;
        }
        if (!String.IsNullOrEmpty(converter))
        {
            GlobalVariables.converter = converter;
        }
        string? input = inputNode?.InnerText.Trim();
        string? output = outputNode?.InnerText.Trim();
        if (!String.IsNullOrEmpty(input))
        {
            GlobalVariables.Input = input;
        }
        if (!String.IsNullOrEmpty(output))
        {
            GlobalVariables.Output = output;
        }

        string? inputMaxThreads = maxThreadsNode?.InnerText;
        if (!String.IsNullOrEmpty(inputMaxThreads) && int.TryParse(inputMaxThreads, out int maxThreads))
        {
            GlobalVariables.maxThreads = maxThreads;
        }

        string? inputTimeout = timeoutNode?.InnerText;
        if (!String.IsNullOrEmpty(inputTimeout) && int.TryParse(inputTimeout, out int timeout))
        {
            GlobalVariables.timeout = timeout;
        }

        string? maxFileSizeString = maxFileSizeNode?.InnerText;
        if (!String.IsNullOrEmpty(maxFileSizeString))
        {
            if (long.TryParse(maxFileSizeString, out long fileSize))
            {
                fileSize = fileSize / 1024 / 1024;
                GlobalVariables.maxFileSize = fileSize;
            }
        }

        string? checksumHashing = root?.SelectSingleNode("ChecksumHashing")?.InnerText;
        if (checksumHashing != null)
        {
            checksumHashing = checksumHashing.ToUpper().Trim();
            GlobalVariables.checksumHash = checksumHashing switch
            {
                "MD5" => "MD5",
                _ => "SHA256",
            };
        }
    }

    /// <summary>
    /// Handles a FileType node from the ConversionSettings file
    /// </summary>
    /// <param name="fileTypeNode">Node that should be parsed</param>
    /// <param name="defaultType">default type for FileClass</param>
    /// <param name="className">name of the FileClass</param>
    static void HandleFileTypeNode(XmlNode fileTypeNode, string defaultType, string className)
    {
        string? pronoms = fileTypeNode.SelectSingleNode("Pronoms")?.InnerText;
        string? innerDefault = fileTypeNode.SelectSingleNode("Default")?.InnerText;
        string? doNotConvert = fileTypeNode.SelectSingleNode("DoNotConvert")?.InnerText.ToUpper().Trim();
        string? formatName = fileTypeNode.SelectSingleNode("Filename")?.InnerText;
        if (String.IsNullOrEmpty(innerDefault))
        {
            innerDefault = defaultType;
        }
        if (String.IsNullOrEmpty(formatName))
        {
            return;
        }

        // Remove whitespace and split pronoms string by commas into a list of strings
        List<string> pronomsList = [];
        if (!string.IsNullOrEmpty(pronoms))
        {
            pronomsList.AddRange(pronoms.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(pronom => pronom.Trim()));
        }
        SettingsData ConversionSettings = new SettingsData
        {
            PronomsList = pronomsList,
            DefaultType = innerDefault,
            ClassDefault = defaultType,
            ClassName = className,
            DoNotConvert = doNotConvert == "YES",
            FormatName = formatName,
        };
        // Add the beginning and end of routes for the file type
        GlobalVariables.FileSettings.Add(ConversionSettings);
    }

    /// <summary>
    /// Sets up the FolderOverride Dictionary
    /// </summary>
    /// <param name="pathToSettings"> relative path to settings file from working directory </param>
    public static void SetUpFolderOverride(string pathToSettings)
    {
        Logger logger = Logger.Instance;
        try
        {
            string? inputFolder = GlobalVariables.Input;
            // Load the XML document from a file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathToSettings);

            if (xmlDoc.DocumentElement == null) { logger.SetUpRunTimeLogMessage("Could not find settings file", true, filename: pathToSettings); return; }

            XmlNodeList? folderOverrideNodes = xmlDoc.SelectNodes("/root/FolderOverride");
            if (folderOverrideNodes != null)
            {
                foreach (XmlNode folderOverrideNode in folderOverrideNodes)
                {
                    HandleFolderOverrideNode(folderOverrideNode);
                }
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
        string inputFolder = GlobalVariables.Input;

        List<string> pronomsList = new List<string>();
        if (!string.IsNullOrEmpty(pronoms))
        {
            pronomsList.AddRange(pronoms.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(pronom => pronom.Trim()));
        }

        SettingsData ConversionSettings = new SettingsData
        {
            PronomsList = pronomsList,
            DefaultType = folderOverrideNode.SelectSingleNode("ConvertTo")?.InnerText ?? "",
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
        List<string> subfolders = GetSubfolderPaths(folderPath);

        foreach (string subfolder in subfolders)
        {
            GlobalVariables.FolderOverride.TryAdd(subfolder, ConversionSettings);
        }
    }

    /// <summary>
    /// Recursively retrieves all subfolders of a given parent folder.
    /// </summary>
    /// <param name="folderName">the name of the parent folder</param>
    /// <returns>list with paths to all subfolders</returns>
    private static List<string> GetSubfolderPaths(string folderName)
    {
        string outputPath = GlobalVariables.Output;
        List<string> subfolders = [];

        try
        {
            string targetFolderPath = Path.Combine(outputPath, folderName);

            if (Directory.Exists(targetFolderPath))
            {
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


    /// <summary>
    /// Sets up and writes the xml file with the settings
    /// </summary>
    /// <param name="path"></param>
    public static void WriteSettings(string path)
    {
        // Create an XML document
        XmlDocument xmlDoc = new XmlDocument();

        // Create the root element
        XmlElement root = xmlDoc.CreateElement("root");
        xmlDoc.AppendChild(root);

        // Create child elements and set their values
        AddXmlElement(xmlDoc, root, "Requester", GlobalVariables.requester);
        AddXmlElement(xmlDoc, root, "Converter", GlobalVariables.converter);
        AddXmlElement(xmlDoc, root, "InputFolder", GlobalVariables.Input);
        AddXmlElement(xmlDoc, root, "OutputFolder", GlobalVariables.Output);
        AddXmlElement(xmlDoc, root, "MaxThreads", GlobalVariables.maxThreads.ToString());
        AddXmlElement(xmlDoc, root, "ChecksumHashing", GlobalVariables.checksumHash);
        AddXmlElement(xmlDoc, root, "Timeout", GlobalVariables.timeout.ToString());
        AddXmlElement(xmlDoc, root, "MaxFileSize", GlobalVariables.maxFileSize.ToString());

        GlobalVariables.FileSettings = GlobalVariables.FileSettings
            .OrderBy(x => x.ClassName)  // Sort by ClassName first
            .ThenBy(x => x.FormatName)  // Then sort by FormatName for items with the same ClassName
            .ToList();

        string lastClassName = ""; // to keep track of the last class name when writing FileTypes
        XmlElement? fileClass = null; // used to add to the previous fileClass if this setting has the same class name

        // Goes through all settings in Filesettings, creates a FileTypes Node and
        // either adds it to a new FileClass or to the previous FileClass if this setting has the same class name as the previous one
        foreach (SettingsData setting in GlobalVariables.FileSettings)
        {      
            if (setting.ClassName != lastClassName)
            {
                fileClass = xmlDoc.CreateElement("FileClass");
                root.AppendChild(fileClass);
                AddXmlElement(xmlDoc, fileClass, "ClassName", setting.ClassName);
                AddXmlElement(xmlDoc, fileClass, "Default", setting.ClassDefault);
                lastClassName = setting.ClassName;
            }

            if (fileClass != null)
            {
                XmlElement fileTypes = xmlDoc.CreateElement("FileTypes");
                fileClass.AppendChild(fileTypes);
                AddXmlElement(xmlDoc, fileTypes, "Filename", setting.FormatName);
                string pronomsListAsString = string.Join(", ", setting.PronomsList);
                AddXmlElement(xmlDoc, fileTypes, "Pronoms", pronomsListAsString);

                XmlElement defaultElement = xmlDoc.CreateElement("Default");
                AddXmlElement(xmlDoc, fileTypes, "DoNotConvert", setting.DoNotConvert ? "YES" : "NO");
                if (setting.DefaultType != setting.ClassDefault)
                {
                    defaultElement.InnerText = setting.DefaultType;
                }
                fileTypes.AppendChild(defaultElement);
            }
        }

        if(GlobalVariables.FolderOverride.Count > 0)
        {
            foreach (KeyValuePair<string, SettingsData> entry in GlobalVariables.FolderOverride)
            {
                XmlElement folderOverride = xmlDoc.CreateElement("FolderOverride");
                root.AppendChild(folderOverride);
                AddXmlElement(xmlDoc, folderOverride, "FolderPath", entry.Key);
                string pronomsListAsString = string.Join(", ", entry.Value.PronomsList);
                AddXmlElement(xmlDoc, folderOverride, "Pronoms", pronomsListAsString);
                AddXmlElement(xmlDoc, folderOverride, "ConvertTo", entry.Value.DefaultType);
                AddXmlElement(xmlDoc, folderOverride, "MergeImages", entry.Value.Merge ? "YES" : "NO");
            }
        }
        // Save the XML document to a file
        xmlDoc.Save(path);
    }

    /// <summary>
    /// Adds a new XML element to the parent element
    /// </summary>
    /// <param name="xmlDoc"> The xml document </param>
    /// <param name="parentElement"> the parent of this element </param>
    /// <param name="elementName"> the name of this element </param>
    /// <param name="value"> the value of this element</param>
    private static void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string? value)
    {
        XmlElement element = xmlDoc.CreateElement(elementName);
        if (!String.IsNullOrEmpty(value))
        {
            element.InnerText = value; 
        }
        else
        {
            element.InnerText = "";
        }
        parentElement.AppendChild(element);
    }
}