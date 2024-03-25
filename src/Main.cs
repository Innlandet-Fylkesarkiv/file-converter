﻿using CommandLine;
using iText.Kernel.Pdf;
using iText.Layout.Splitting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

public enum PrintSortBy
{
	Count,
	TargetPronom,
	CurrentPronom,
}

public static class GlobalVariables
{
	public static Options parsedOptions = new Options();
	//Map with all specified conversion formats, to and from
	public static Dictionary<string, string> FileSettings = new Dictionary<string, string>(); // the key is pronom code 
	// Map with info about what folders have overrides for specific formats
	public static Dictionary<string, SettingsData> FolderOverride = new Dictionary<string, SettingsData>(); // the key is a foldername
	public static HashAlgorithms checksumHash = HashAlgorithms.SHA256;
	public static int maxThreads = Environment.ProcessorCount*2;
	public static int timeout = 30;
    public static double maxFileSize = 1000000000;      //1GB
    public const int MAX_RETRIES = 3; //Maximum number of attempts in case of a failed conversion
	public const ConsoleColor INFO_COL = ConsoleColor.Cyan;
	public const ConsoleColor ERROR_COL = ConsoleColor.Red;
	public const ConsoleColor WARNING_COL = ConsoleColor.Yellow;
	public const ConsoleColor SUCCESS_COL = ConsoleColor.Green;
	public static readonly PrintSortBy SortBy = PrintSortBy.Count;
	
	public static void Reset()
	{
		FileSettings.Clear();
		FolderOverride.Clear();
		//Set to default values (will be overwritten in Settings.cs if specified by user)
		checksumHash = HashAlgorithms.SHA256;
		maxThreads = Environment.ProcessorCount * 2;
		timeout = 30;
	}
}
public class Options
{
	[Option('i', "input", Required = false, HelpText = "Specify input directory", Default = "input")]
	public string Input { get; set; } = "";
	[Option('o', "output", Required = false, HelpText = "Specify output directory", Default = "output")]
	public string Output { get; set; } = "";
	[Option('s', "settings", Required = false, HelpText = "Specify settings file", Default = "Settings.xml")]
	public string Settings { get; set; } = "";
}
class Program
{ 
	static void Main(string[] args)
	{
        bool debug = true;
		string settingsPath = "";
        Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
        {
            GlobalVariables.parsedOptions = options;
        });

        if (GlobalVariables.parsedOptions.Settings == "Settings.xml")
        {
            settingsPath = debug ? "Settings_Testing.xml" : "Settings.xml";
        }

		//Only maximize and center the console window if the OS is Windows
		Console.Title = "FileConverter";
		//MaximizeAndCenterConsoleWindow();
		if (!OperatingSystem.IsLinux())
		{
			//Look for settings file in parent directories as long as settings file is not found and we are not in the root directory
			while (!File.Exists(settingsPath) && Directory.GetCurrentDirectory() != Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()))
			{
				Directory.SetCurrentDirectory("..");
			}
			if (!File.Exists(settingsPath))
			{
				Console.ForegroundColor = GlobalVariables.ERROR_COL;
				Console.WriteLine("Could not find settings file. Please make sure that the settings file is in the root directory of the program.");
				return;
			}
		}
		else
		{
			LinuxSetup.Setup();
		}
		Settings settings = Settings.Instance;
		Console.WriteLine("Reading settings from '{0}'...",settingsPath);
		settings.ReadSettings(settingsPath);
		Logger logger = Logger.Instance;

		FileManager fileManager = FileManager.Instance;
		Siegfried sf = Siegfried.Instance;
		
		//TODO: Check for malicous input files
		try
		{
			//Check if user wants to use files from previous run
			//sf.AskReadFiles();
			//Check if files were added from previous run
			if (!sf.Files.IsEmpty)
			{
				//Import files from previous run
				Console.WriteLine("Checking files from previous run...");
				fileManager.ImportFiles(sf.Files.ToList());
				var compressedFiles = sf.IdentifyCompressedFilesJSON(GlobalVariables.parsedOptions.Input);
				fileManager.ImportCompressedFiles(compressedFiles);
			}
			else
			{
				Console.WriteLine("Copying files from {0} to {1}...",GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
				//Copy files
				sf.CopyFiles(GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
				Console.WriteLine("Identifying files...");
				//Identify and unpack files
				fileManager.IdentifyFiles();
			}
		} catch (Exception e)
		{
            Console.ForegroundColor = GlobalVariables.ERROR_COL;
            Console.WriteLine("[FATAL] Could not identify files: " + e.Message);
			logger.SetUpRunTimeLogMessage("Main: Error when copying/unpacking/identifying files: " + e.Message, true);
			return;
		}
		ConversionManager cm = ConversionManager.Instance;
		//Set up folder override after files have been copied over
        settings.SetUpFolderOverride(settingsPath);

        if (fileManager.Files.Count > 0)
		{			
			string input;
			do
			{
                logger.AskAboutReqAndConv();
				//settings.AskAboutEmptyDefaults();
                fileManager.DisplayFileList();
				var oldColor = Console.ForegroundColor;
				Console.ForegroundColor = GlobalVariables.INFO_COL;
				Console.WriteLine("Requester: {0}",Logger.JsonRoot.requester);
				Console.WriteLine("Converter: {0}",Logger.JsonRoot.converter);
				Console.WriteLine("MaxThreads: {0}", GlobalVariables.maxThreads);
				Console.WriteLine("Timeout in minutes: {0}", GlobalVariables.timeout);
				Console.ForegroundColor = oldColor;
				Console.Write("Do you want to proceed with these settings (Y (Yes) / N (Exit program) / R (Reload) / G (Change in GUI): ");
				string ?r = Console.ReadLine();
				r = r?.ToUpper() ?? " ";
				input = r;
				if (input == "R")
				{
					Console.WriteLine("Change settings file and hit enter when finished (Remember to save file)");
					Console.ReadLine();
					settings.ReadSettings(settingsPath);
					settings.SetUpFolderOverride(settingsPath);
				}
				if (input == "G")
				{
					//TODO: Start GUI
					Console.WriteLine("Not implemented yet...");
					settings.ReadSettings(settingsPath);
					settings.SetUpFolderOverride(settingsPath);
				}
			} while (input != "Y" && input != "N");
			if (input == "N")
			{
				return;
			}

			try
			{
				fileManager.CheckForNamingConflicts();
                Console.WriteLine("Converting files...");
                cm.ConvertFiles();
				//Delete siegfrieds json files
				sf.ClearOutputFolder();
			} catch (Exception e)
			{
				Console.WriteLine("Error while converting " + e.Message);
				logger.SetUpRunTimeLogMessage("Main: Error when converting files: " + e.Message, true);
			}
			finally
			{
				Console.WriteLine("Conversion finished:");
				fileManager.ConversionFinished = true;
				fileManager.DisplayFileList();
				Console.WriteLine("Documenting conversion...");
				fileManager.DocumentFiles();
			}
			Console.WriteLine("Compressing folders...");
			sf.CompressFolders();

			if(Logger.Instance.errorHappened)
			{
                Console.WriteLine("One or more errors happened during runtime, please check the log file for more information.");
            } else
			{
				Console.WriteLine("No errors happened during runtime. See documentation.json file in output dir.");
			}
		}
	}
}
