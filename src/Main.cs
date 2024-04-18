using CommandLine;
using Org.BouncyCastle.Asn1;
using System.Diagnostics;
using FileConverter.HelperClasses;
using FileConverter.LinuxSpecifics;
using FileConverter.Managers;
using SF = FileConverter.Siegfried;

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
	class Program
	{
		static void Main(string[] args)
		{

			Stopwatch sw = new Stopwatch();
			sw.Start();
			PrintHelper.OldCol = Console.ForegroundColor;
			if (GlobalVariables.debug)
			{
				Console.WriteLine("Running in debug mode...");
			}

			Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
			{
				GlobalVariables.parsedOptions = options;
			});
			string ConversionSettingsPath = GlobalVariables.parsedOptions.ConversionSettings;
			if (!OperatingSystem.IsLinux())
			{
				//Look for ConversionSettings file in parent directories as long as ConversionSettings file is not found and we are not in the root directory
				while (!File.Exists(ConversionSettingsPath) && Directory.GetCurrentDirectory() != Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()))
				{
					Directory.SetCurrentDirectory("..");
				}
				if (!File.Exists(ConversionSettingsPath))
				{
					PrintHelper.PrintLn("Could not find ConversionSettings file. Please make sure that the ConversionSettings file is in the root directory of the program.", GlobalVariables.ERROR_COL);
					goto END;
				}
			}
			else
			{
				LinuxSetup.Setup();
			}

			ConversionSettings ConversionSettings = ConversionSettings.Instance;
			Console.WriteLine("Reading ConversionSettings from '{0}'...", ConversionSettingsPath);
			ConversionSettings.ReadConversionSettings(ConversionSettingsPath);

			//Check if input and output folders exist
			while (!Directory.Exists(GlobalVariables.parsedOptions.Input))
			{

				PrintHelper.PrintLn("Input folder '{0}' not found!", GlobalVariables.ERROR_COL, GlobalVariables.parsedOptions.Input);
				var exit = ResolveInputNotFound();
				ConversionSettings.ReadConversionSettings(ConversionSettingsPath);
				if (exit)
				{
					goto END;
				}
			}

			if (!Directory.Exists(GlobalVariables.parsedOptions.Output))
			{
				Console.WriteLine("Output folder '{0}' not found! Creating...", GlobalVariables.parsedOptions.Output);
				Directory.CreateDirectory(GlobalVariables.parsedOptions.Output);
			}

			//Only maximize and center the console window if the OS is Windows
			Console.Title = "FileConverter";

			Logger logger = Logger.Instance;

			FileManager fileManager = FileManager.Instance;
			SF.Siegfried sf2 = SF.Siegfried.Instance;
			//TODO: Check for malicous input files
			try
			{
				//Check if user wants to use files from previous run
				//sf.AskReadFiles();
				//Check if files were added from previous run
				if (!sf2.Files.IsEmpty)
				{
					//Import files from previous run
					Console.WriteLine("Checking files from previous run...");
					fileManager.ImportFiles(sf2.Files.ToList());
					var compressedFiles = sf2.IdentifyCompressedFilesJSON(GlobalVariables.parsedOptions.Input);
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
				goto END;
			}

			//Set up folder override after files have been copied over
			ConversionSettings.SetUpFolderOverride(ConversionSettingsPath);
			while (fileManager.Files.Count < 1)
			{
				var exit = ResolveInputNotFound();
				if (exit)
				{
					goto END;
				}
				ConversionSettings.ReadConversionSettings(ConversionSettingsPath);
				InitFiles();
			}

			char input = ' ';
			string validInput = "YyNnRrGg";
			string prevInputFolder = GlobalVariables.parsedOptions.Input; ;

			do
			{
				if (prevInputFolder != GlobalVariables.parsedOptions.Input)
				{
					PrintHelper.PrintLn("Input folder changed, reidentifying files...", GlobalVariables.WARNING_COL);
					InitFiles();
				}
				input = GlobalVariables.parsedOptions.AcceptAll ? 'Y' : 'X';
				logger.AskAboutReqAndConv();
				fileManager.DisplayFileList();
				PrintHelper.PrintLn("Requester: {0}\nConverter: {1}\nMaxThreads: {2}\nTimeout in minutes: {3}",
					GlobalVariables.INFO_COL, Logger.JsonRoot.Requester, Logger.JsonRoot.Converter, GlobalVariables.maxThreads, GlobalVariables.timeout);

				Console.Write("Do you want to proceed with these Settings (Y (Yes) / N (Exit program) / R (Reload) / G (Change in GUI): ");
				while (!validInput.Contains(input))
				{
					var r = Console.ReadKey();
					input = r.KeyChar;
					input = char.ToUpper(input);
				}
				Console.WriteLine();
				prevInputFolder = GlobalVariables.parsedOptions.Input;
				switch (input)
				{
					case 'Y':   //Proceed with conversion
						break;
					case 'N':   //Exit program
						goto END;
					case 'R':   //Change ConversionSettings and reload manually
						Console.WriteLine("Edit ConversionSettings file and hit enter when finished (Remember to save file)");
						Console.ReadLine();
						ConversionSettings.ReadConversionSettings(ConversionSettingsPath);
						ConversionSettings.SetUpFolderOverride(ConversionSettingsPath);
						break;
					case 'G':   //Change ConversionSettings and reload in GUI
						AwaitGUI().Wait();
						ConversionSettings.ReadConversionSettings(ConversionSettingsPath);
						ConversionSettings.SetUpFolderOverride(ConversionSettingsPath);
						break;
					default: break;
				}
			} while (input != 'Y' || fileManager.Files.Count == 0);

			ConversionManager cm = ConversionManager.Instance;
			try
			{
				fileManager.CheckForNamingConflicts();
				Console.WriteLine("Starting Conversion manager...");
				cm.ConvertFiles();
				//Delete siegfrieds json files
				sf2.ClearOutputFolder();
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
			Console.WriteLine("Compressing folders...");
			sf2.CompressFolders();

			if (Logger.Instance.ErrorHappened)
			{
				PrintHelper.PrintLn("One or more errors happened during runtime, please check the log file for more information.", GlobalVariables.ERROR_COL);
			}
			else
			{
				PrintHelper.PrintLn("No errors happened during runtime. See documentation.json file in output dir.", GlobalVariables.SUCCESS_COL);
			}

		END:
			sw.Stop();
			Console.WriteLine("Time elapsed: {0}", sw.Elapsed);
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();  //Keep console open
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
			Console.WriteLine("Copying files from {0} to {1}...", GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
			//Copy files
			SF.Siegfried.Instance.CopyFiles(GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
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
	}
}
