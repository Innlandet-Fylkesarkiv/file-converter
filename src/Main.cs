using CommandLine;
using Org.BouncyCastle.Asn1;
using System.Diagnostics;
using FileConverter.HelperClasses;
using FileConverter.LinuxSpecifics;
using FileConverter.Managers;
using SF = FileConverter.Siegfried;
using FileConverter.Siegfried;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace FileConverter
{
	public class Options
	{
		[Option('i', "input", Required = false, HelpText = "Specify input directory", Default = "input")]
		public string Input { get; set; } = "";
		[Option('o', "output", Required = false, HelpText = "Specify output directory", Default = "output")]
		public string Output { get; set; } = "";
		[Option('s', "ConversionSettings", Required = false, HelpText = "Specify ConversionSettings file", Default = "ConversionSettings.xml")]
		public string ConversionSettings { get; set; } = "";

		[Option('y', "yes", Required = false, HelpText = "Accept all queries", Default = false)]
		public bool AcceptAll { get; set; } = false;
	}
    static class Program
	{
		static void Main(string[] args)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			PrintHelper.OldCol = Console.ForegroundColor;
			if (GlobalVariables.Debug)
			{
				Console.WriteLine("Running in debug mode...");
			}
			
			Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
			{
				GlobalVariables.ParsedOptions = options;
			});

			string conversionSettingsPath = GlobalVariables.ParsedOptions.ConversionSettings;
			CheckConversionSettingsFile(conversionSettingsPath);
			CheckInputOutputFolders(conversionSettingsPath);

			Console.Title = "FileConverter";

			Logger logger = Logger.Instance;

			FileManager fileManager = FileManager.Instance;
			SF.Siegfried sf = SF.Siegfried.Instance;
			ReloadPreviousRunOrInitNewFiles(sf, fileManager, logger);
			

			//Set up folder override after files have been copied over
			ConversionSettings.SetUpFolderOverride(conversionSettingsPath);
            while (fileManager.Files.IsEmpty)
            {
				var exit = ResolveInputNotFound();
				if (exit)
				{
                    ExitProgram(0);
                }
                FileConverter.ConversionSettings.ReadConversionSettings(conversionSettingsPath);
				InitFiles();
			}

			char input = ' ';
			string prevInputFolder = GlobalVariables.ParsedOptions.Input;

			do
			{
				if (prevInputFolder != GlobalVariables.ParsedOptions.Input)
				{
					PrintHelper.PrintLn("Input folder changed, reidentifying files...", GlobalVariables.WARNING_COL);
					InitFiles();
				}
				input = GlobalVariables.ParsedOptions.AcceptAll ? 'Y' : 'X';
				Logger.SetRequesterAndConverter();
				fileManager.DisplayFileList();
				PrintHelper.PrintLn("Requester: {0}\nConverter: {1}\nMaxThreads: {2}\nTimeout in minutes: {3}",
					GlobalVariables.INFO_COL, Logger.JsonRoot.Requester, Logger.JsonRoot.Converter, GlobalVariables.MaxThreads, GlobalVariables.Timeout);

				Console.Write("Do you want to proceed with these Settings (Y (Yes) / N (Exit program) / R (Reload) / G (Change in GUI): ");
				GetUserInputAndAct(ref input, ref prevInputFolder, conversionSettingsPath);
			} while (input != 'Y' || fileManager.Files.IsEmpty);

			ConversionManager cm = ConversionManager.Instance;
			RunConversion(fileManager, cm, sf, logger);
			Console.WriteLine("Compressing folders...");
			sf.CompressFolders();

			if (Logger.Instance.ErrorHappened)
			{
				PrintHelper.PrintLn("One or more errors happened during runtime, please check the log file for more information.", GlobalVariables.ERROR_COL);
			}
			else
			{
				PrintHelper.PrintLn("No errors happened during runtime. See documentation.json file in output dir.", GlobalVariables.SUCCESS_COL);
			}
			ExitProgram(0);
		}

		/// <summary>
		/// Function to wait for input and then exit program with a specified exit code
		/// </summary>
		/// <param name="exitCode">Exit code to return</param>
		static void ExitProgram(int exitCode)
		{
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();  //Keep console open
			Environment.Exit(exitCode);
        }

        static bool ResolveInputNotFound()
        {
			PrintHelper.PrintLn("Input folder not found / Input folder empty!", GlobalVariables.ERROR_COL);
			Console.WriteLine("Do you want to:  N (Exit program) / R (Reload ConversionSettings file) / G (Change ConversionSettings in GUI)");
			char input = ' ';
			string validInput = "NnRrGg";
			while (!validInput.Contains(input))
			{
				var r = Console.ReadKey();
				input = r.KeyChar;
				input = char.ToUpper(input);
			}
			Console.WriteLine();

			if (input == 'R')
			{
				Console.WriteLine("Change ConversionSettings file and hit enter when finished (Remember to save file)");
				Console.ReadLine();
			}
			else if (input == 'G')
			{
				AwaitGUI().Wait();
			}
			else
			{
				return true;
			}
			return false;
		}

		static void InitFiles()
		{
			FileManager.Instance.Files.Clear();
			SF.Siegfried.Instance.Files.Clear();
			SF.Siegfried.Instance.CompressedFolders.Clear();
			Console.WriteLine("Copying files from {0} to {1}...", GlobalVariables.ParsedOptions.Input, GlobalVariables.ParsedOptions.Output);
			//Copy files
			SF.Siegfried.CopyFiles(GlobalVariables.ParsedOptions.Input, GlobalVariables.ParsedOptions.Output);
			Console.WriteLine("Identifying files...");
			//Identify and unpack files
			FileManager.Instance.IdentifyFiles();
			ConversionManager.Instance.InitFileMap();
		}

		/// <summary>
		/// Method to get the path of the GUI executable
		/// </summary>
		/// <returns>Path to GUI executable or null if not found</returns>
		static string? GetGUIPath()
		{
			string filename = OperatingSystem.IsLinux() ? "ChangeConverterSettings.dll" : "ChangeConverterSettings.exe";
			string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), filename, SearchOption.AllDirectories);
			if (files.Length > 0)
			{
				return files[0];
			}
			return null;
		}

		/// <summary>
		/// Method to start and await the GUI process
		/// </summary>
		/// <returns>Awaitable Task</returns>
		async static Task AwaitGUI()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			try
			{
				var GUIPath = GetGUIPath();
				if (GUIPath == null)
				{
					Console.WriteLine("Could not find GUI executable");
					return;
				}
				if (OperatingSystem.IsLinux())
				{
					startInfo.FileName = "dotnet";
					startInfo.Arguments = GUIPath;
				}
				else
				{
					startInfo.FileName = GUIPath;
				}

				if (startInfo.FileName == "" || startInfo.FileName == "dotnet ")
				{
					Console.WriteLine("Could not find GUI executable");
					return;
				}
				var process = Process.Start(startInfo);
				Console.WriteLine("Press any key in terminal or close GUI window to continue");

				// Discard any existing input in the console buffer
				while (Console.KeyAvailable)
				{
					Console.ReadKey(intercept: true); // Read and discard each character
				}

				// Monitor user input and process status
				while (process != null && !process.HasExited)
				{
					// Check if a key is available (user typed a character)
					if (Console.KeyAvailable)
					{
						// Read the key without blocking
						ConsoleKeyInfo key = Console.ReadKey(intercept: true);

						if (key.Key != ConsoleKey.Escape)
						{
							// Exit the loop and return from the method
							process.Kill();
							process.WaitForExit();
							break;
						}
					}
					// Delay for a short duration
					await Task.Delay(100);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error while starting GUI: " + e.Message);
			}
		}
        static void CheckConversionSettingsFile(string conversionSettingsPath)
        {
            if (!OperatingSystem.IsLinux())
            {
                while (!File.Exists(conversionSettingsPath) && Directory.GetCurrentDirectory() != Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()))
                {
                    Directory.SetCurrentDirectory("..");
                }
                if (!File.Exists(conversionSettingsPath))
                {
                    PrintHelper.PrintLn("Could not find ConversionSettings file. Please make sure that the ConversionSettings file is in the root directory of the program.", GlobalVariables.ERROR_COL);
                    ExitProgram(1);
                }
            }
            else
            {
                LinuxSetup.Setup();
            }
            Console.WriteLine("Reading ConversionSettings from '{0}'...", conversionSettingsPath);
            FileConverter.ConversionSettings.ReadConversionSettings(conversionSettingsPath);
        }


		static private void CheckInputOutputFolders(string conversionSettingsPath)
		{
            //Check if input and output folders exist
            while (!Directory.Exists(GlobalVariables.ParsedOptions.Input))
            {
                PrintHelper.PrintLn("Input folder '{0}' not found!", GlobalVariables.ERROR_COL, GlobalVariables.ParsedOptions.Input);
                var exit = ResolveInputNotFound();
                FileConverter.ConversionSettings.ReadConversionSettings(conversionSettingsPath);
                if (exit)
                {
                    ExitProgram(0);
                }
            }

            if (!Directory.Exists(GlobalVariables.ParsedOptions.Output))
            {
                PrintHelper.PrintLn("Output folder '{0}' not found! Creating...", GlobalVariables.WARNING_COL, GlobalVariables.ParsedOptions.Output);
                Directory.CreateDirectory(GlobalVariables.ParsedOptions.Output);
            }
        }
		static private void ReloadPreviousRunOrInitNewFiles(SF.Siegfried sf, FileManager fileManager, Logger logger)
		{
            try
            {
                //Check if user wants to use files from previous run
                //sf.AskReadFiles(); Removed for stability

                //Check if files were added from previous run
                if (!sf.Files.IsEmpty)
                {
                    //Import files from previous run
                    Console.WriteLine("Checking files from previous run...");
                    fileManager.ImportFiles(sf.Files.ToList());
                    var compressedFiles = sf.IdentifyCompressedFilesJSON(GlobalVariables.ParsedOptions.Input);
                    fileManager.ImportCompressedFiles(compressedFiles);
                }
                else
                {
                    InitFiles();
                }
            }
            catch (Exception e)
            {
                PrintHelper.PrintLn("[FATAL] Could not identify files: " + e.Message, GlobalVariables.ERROR_COL);
                logger.SetUpRunTimeLogMessage("Main: Error when copying/unpacking/identifying files: " + e.Message, true);
                ExitProgram(1);
            }
        }
        static private void RunConversion(FileManager fileManager, ConversionManager cm, SF.Siegfried sf, Logger logger)
		{
            try
            {
                Console.WriteLine("Checking for naming conflicts...");
                fileManager.CheckForNamingConflicts();
                Console.WriteLine("Starting Conversion manager...");
                cm.ConvertFiles().Wait();
                //Delete siegfrieds json files
                sf.ClearOutputFolder();
            }
            catch (Exception e)
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
        }
		static private void GetUserInputAndAct(ref char input, ref string prevInputFolder, string conversionSettingsPath)
		{
            string validInput = "YyNnRrGg";
            while (!validInput.Contains(input))
            {
                var r = Console.ReadKey();
                input = r.KeyChar;
                input = char.ToUpper(input);
            }
            Console.WriteLine();
            prevInputFolder = GlobalVariables.ParsedOptions.Input;
            switch (input)
            {
                case 'Y':   //Proceed with conversion
                    break;
                case 'N':   //Exit program
                    ExitProgram(0);
                    break;
                case 'R':   //Change ConversionSettings and reload manually
                    Console.WriteLine("Edit ConversionSettings file and hit enter when finished (Remember to save file)");
                    Console.ReadLine();
                    FileConverter.ConversionSettings.ReadConversionSettings(conversionSettingsPath);
                    ConversionSettings.SetUpFolderOverride(conversionSettingsPath);
                    break;
                case 'G':   //Change ConversionSettings and reload in GUI
                    AwaitGUI().Wait();
                    FileConverter.ConversionSettings.ReadConversionSettings(conversionSettingsPath);
                    ConversionSettings.SetUpFolderOverride(conversionSettingsPath);
                    break;
                default: break;
            }
        }
	}
}
