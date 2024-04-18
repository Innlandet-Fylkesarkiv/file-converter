using Avalonia.Logging;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System;
using System.Xml;
using ChangeConverterSettings;
using System.Linq;

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
    public void ReadAllSettings(string pathToSettings)
    {
        ReadSettings(pathToSettings);
        SetUpFolderOverride(pathToSettings);
    }

    /// <summary>
    /// Reads settings from file
    /// </summary>
    /// <param name="pathToSettings"> the path to the settings file from working directory </param>
    private void ReadSettings(string pathToSettings)
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
            // Access the Meta elements
            XmlNode? requesterNode = root?.SelectSingleNode("Requester");
            XmlNode? converterNode = root?.SelectSingleNode("Converter");
            XmlNode? inputNode = root?.SelectSingleNode("InputFolder");
            XmlNode? outputNode = root?.SelectSingleNode("OutputFolder");
            XmlNode? maxThreadsNode = root?.SelectSingleNode("MaxThreads");
            XmlNode? timeout = root?.SelectSingleNode("Timeout");
            XmlNode? maxFileSize = root?.SelectSingleNode("MaxFileSize");

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
            string? checksumHashing = root?.SelectSingleNode("ChecksumHashing")?.InnerText;
            if (checksumHashing != null)
            {
                checksumHashing = checksumHashing.ToUpper().Trim();
                switch (checksumHashing)
                {
                    case "MD5": GlobalVariables.checksumHash = "MD5"; break;
                    default: GlobalVariables.checksumHash = "SHA256"; break;
                }
            }
            string? timeoutString = timeout?.InnerText;
            if (!String.IsNullOrEmpty(timeoutString))
            {
                GlobalVariables.timeout = timeoutString;
            }
            string? maxFileSizeString = maxFileSize?.InnerText;
            if (!String.IsNullOrEmpty(maxFileSizeString))
            {
                if (long.TryParse(maxFileSizeString, out long fileSize))
                {
                    fileSize = fileSize / 1024 / 1024;
                    GlobalVariables.maxFileSize = fileSize.ToString();
                }
            }

            // Access elements and attributes
            XmlNodeList? classNodes = root?.SelectNodes("FileClass");
            if (classNodes == null) { logger.SetUpRunTimeLogMessage("Could not find any classNodes", true, filename: pathToSettings); return; }
            foreach (XmlNode classNode in classNodes)
            {
                string? className = classNode?.SelectSingleNode("ClassName")?.InnerText; 
                string? defaultType = classNode?.SelectSingleNode("Default")?.InnerText;

                if (defaultType == null)
                {
                    //TODO: This should not be thrown, but rather ask the user for a default type
                    throw new Exception("No default type found in settings");
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
                        //string outerdefault = defaultType;
                        if (String.IsNullOrEmpty(innerDefault))
                        {
                            innerDefault = defaultType;
                        }
                        if(String.IsNullOrEmpty(extension))
                        {
                            logger.SetUpRunTimeLogMessage("Could not find fileName in settings", true);
                            extension = "unknown";
                        }
                        // Remove whitespace and split pronoms string by commas into a list of strings
                        List<string> pronomsList = new List<string>();
                        if (!string.IsNullOrEmpty(pronoms))
                        {
                            pronomsList.AddRange(pronoms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(pronom => pronom.Trim()));
                        }
                        SettingsData settings = new SettingsData
                        {
                            PronomsList = pronomsList,
                            DefaultType = innerDefault,
                            FormatName = extension,
                            ClassName = className ?? "unknown",
                            ClassDefault = defaultType,
                            DoNotConvert = doNotConvert == "YES",
                        };
                        if (settings.PronomsList.Count > 0)
                        {
                            GlobalVariables.FileSettings.Add(settings);
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
    /// <param name="pathToSettings"> relative path to settings file from working directory </param>
    public void SetUpFolderOverride(string pathToSettings)
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
                    string? folderPath = folderOverrideNode.SelectSingleNode("FolderPath")?.InnerText;
                    string? pronoms = folderOverrideNode.SelectSingleNode("Pronoms")?.InnerText;
                    string? merge = folderOverrideNode.SelectSingleNode("MergeImages")?.InnerText.ToUpper().Trim();

                    List<string> pronomsList = new List<string>();
                    if (!string.IsNullOrEmpty(pronoms))
                    {
                        pronomsList.AddRange(pronoms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(pronom => pronom.Trim()));
                    }

                    string? convertTo = folderOverrideNode.SelectSingleNode("ConvertTo")?.InnerText;
                    if (string.IsNullOrEmpty(convertTo))
                    {
                        logger.SetUpRunTimeLogMessage("Could not find convertTo in settings", true);
                        convertTo = "unknown";
                    }

                    SettingsData settings = new SettingsData
                    {
                        PronomsList = pronomsList,
                        DefaultType = convertTo,
                        Merge = merge == "YES",
                    };

                    bool folderPathEmpty = String.IsNullOrEmpty(folderPath);
                    bool pronomsEmpty = String.IsNullOrEmpty(pronoms);
                    bool convertToEmpty = String.IsNullOrEmpty(settings.DefaultType);

                    if (folderPathEmpty || pronomsEmpty || convertToEmpty)
                    {
                        logger.SetUpRunTimeLogMessage("something wrong with a folderOverride in settings", true);
                    }
                    else
                    {
                        string path = Path.Combine(GlobalVariables.Input,folderPath);
                        if (Directory.Exists(GlobalVariables.Input+"/"+folderPath))
                        {
                            GlobalVariables.FolderOverride[folderPath] = settings;
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
    /// Sets up and writes the xml file with the settings
    /// </summary>
    /// <param name="path"></param>
    public void WriteSettings(string path)
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
        AddXmlElement(xmlDoc, root, "Timeout", GlobalVariables.timeout);
        AddXmlElement(xmlDoc, root, "MaxFileSize", GlobalVariables.maxFileSize);

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
    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string? value)
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