﻿using CommandLine;
using System.Diagnostics;

public static class GlobalVariables
{
    public static Options parsedOptions = new Options();
    public static Dictionary<string, string> FileSettings = new Dictionary<string, string>();
	public static HashAlgorithms checksumHash;
}
public class Options
{
    [Option('i', "input", Required = false, HelpText = "Specify input directory", Default = "input")]
    public string Input { get; set; } = "";
    [Option('o', "output", Required = false, HelpText = "Specify output directory", Default = "output")]
    public string Output { get; set; } = "";

}
class Program
{ 
	
	static void Main(string[] args)
	{
		Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
		{
            GlobalVariables.parsedOptions = options;
			if (options.Input != null)
			{
				Console.WriteLine("Input: " + options.Input);
			}
			if (options.Output != null)
			{
				Console.WriteLine("Output: " + options.Output);
			}
		});

		if (GlobalVariables.parsedOptions == null)
			return;

		Directory.SetCurrentDirectory("../../../");
		
		Logger logger = Logger.Instance;

		FileManager fileManager = FileManager.Instance;
		Siegfried sf = Siegfried.Instance;
		//TODO: Check for malicous input files
		try
		{
			//Copy and unpack files
			sf.CopyFiles(GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
			//Identify files
			fileManager.IdentifyFiles();
		} catch (Exception e)
		{
			Console.WriteLine("Could not identify files: " + e.Message);
			logger.SetUpRunTimeLogMessage("Error when copying/unpacking/identifying files: " + e.Message, true);
			return;
		}
		Settings settings = Settings.Instance;
        ConversionManager cm = new ConversionManager();
		settings.ReadSettings("./Settings.xml");
        logger.AskAboutReqAndConv();
		
        if (fileManager.Files.Count > 0)
        {
			Console.WriteLine("Files identified: " + fileManager.Files.Count);
            cm.ConvertFiles();
			sf.CompressFolders();
            logger.SetUpDocumentation(fileManager.Files);
        }
    }
}
