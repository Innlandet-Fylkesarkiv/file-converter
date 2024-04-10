﻿using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Mozilla;
using SharpCompress;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

public class FileManager
{
    private static FileManager? instance;
    private static readonly object lockObject = new object();
	public ConcurrentDictionary<Guid,FileInfo> Files;	// List of files to be converted
	private static readonly object identifyingFiles = new object(); // True if files are being identified
	public bool ConversionFinished = false; // True if conversion is finished
    private FileManager()
    {
        Files = new ConcurrentDictionary<Guid, FileInfo>();
    }
    public static FileManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new FileManager();
                    }
                }
            }
            return instance;
        }
    }
  
    public void IdentifyFiles()
    {
		lock (identifyingFiles)
		{
			Siegfried sf = Siegfried.Instance;
			Logger logger = Logger.Instance;
			//TODO: Can maybe run both individually and compressed files at the same time
			//Identifying all uncompressed files
			List<FileInfo>? files = sf.IdentifyFilesIndividually(GlobalVariables.parsedOptions.Input)!.Result; //Search for files in output folder since they are copied there from input folder
			if (files != null)
			{
				//TODO: Should be more robust
				//Change path from input to output directory
				foreach (FileInfo file in files)
				{
					//Replace first occurence of input path with output path
					file.FilePath = file.FilePath.Replace(GlobalVariables.parsedOptions.Input, GlobalVariables.parsedOptions.Output);
					Guid id = Guid.NewGuid();
					file.Id = id;
					Files.TryAdd(id, file);
				}

			}
			else
			{
				logger.SetUpRunTimeLogMessage("Error when discovering files / No files found", true);
			}

			//Identifying all compressed files
			List<FileInfo>? compressedFiles = sf.IdentifyCompressedFilesJSON(GlobalVariables.parsedOptions.Input)!;

			foreach (var file in compressedFiles)
			{
				Guid id = Guid.NewGuid();
				file.Id = id;
                Files.TryAdd(id, file);
			}

            //Remove all compressed files from the list
            var compressedExtensions = new List<string> { ".zip", ".tar", ".tar.gz", ".tar.bz2", ".7z", ".rar" };
            var entriesToRemove = Files.Where(kvp => compressedExtensions.Contains(Path.GetExtension(kvp.Value.FilePath))).ToList();
            foreach (var kvp in entriesToRemove)
            {
                Files.TryRemove(kvp.Key, out _);
            }
        }
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="files"></param>
    public void ImportFiles(List<FileInfo> files)
    {
        try
        {
            //Remove files that no longer exist
            files.RemoveAll(file => !File.Exists(file.FilePath));
            //Remove files that are not in the input directory
            files.RemoveAll(file => !FileExistsInDirectory(GlobalVariables.parsedOptions.Input, file.FilePath));
            //Remove all duplicates in file list
            files = files.Distinct().ToList();

            files.ForEach(file =>
            {
                if (!FileExistsInDirectory(GlobalVariables.parsedOptions.Output, file.FilePath))
                {
                    var newPath = Path.Combine(GlobalVariables.parsedOptions.Output, Path.GetFileName(file.FilePath));
                    File.Copy(file.FilePath, newPath);
                }
                Guid id = Guid.NewGuid();
                file.Id = id;
                // Check for duplicate OriginalChecksum
                if (!Files.Values.Any(f => f.OriginalChecksum == file.OriginalChecksum))
                {
                    Files.TryAdd(id, file);
                }
            });

        }
        catch (Exception e)
        {
            Logger logger = Logger.Instance;
            logger.SetUpRunTimeLogMessage("Error when copying files: " + e.Message, true);
        }
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="files"></param>
	public void ImportCompressedFiles(List<FileInfo> files)
	{
        try
        {
            //Remove files that no longer exist
            files.RemoveAll(file => !File.Exists(file.FilePath));
        }
        catch (Exception e)
        {
            Logger logger = Logger.Instance;
            logger.SetUpRunTimeLogMessage("Error when copying files: " + e.Message, true);
        }
        files.ForEach(file =>
		{
            Guid id = Guid.NewGuid();
            file.Id = id;
            Files.TryAdd(id, file);
        });
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="directoryPath"></param>
	/// <param name="fileName"></param>
	/// <returns></returns>
    private static bool FileExistsInDirectory(string directoryPath, string fileName)
    {
		// Get the relative path of the file
		int index = fileName.Contains(GlobalVariables.parsedOptions.Input) ? fileName.IndexOf(GlobalVariables.parsedOptions.Input) : fileName.IndexOf(GlobalVariables.parsedOptions.Output);
		string path = fileName.Substring(index);
        try
        {
            // Check if the directory exists
            if (Directory.Exists(directoryPath))
            {
                // Check if parent directory exists
                if (!Directory.Exists(Directory.GetParent(path).FullName))
                {
                    return false;
                }
                // Search for the file with the specified pattern in the directory and its subdirectories
                string[] files = Directory.GetFiles(directoryPath, Path.GetFileName(path), SearchOption.AllDirectories);

                // If any matching file is found, return true
                return files.Length > 0;
            }
			else
			{
				   return false;
			}
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks for potential conflicts in file naming after conversion. <br></br>
    /// Will resolve conflicts by renaming files in this order: <br></br>
    /// 1. Add the original extension to the file name <br></br>
    /// 2. Add a number to the file name <br></br>
    /// </summary>
    public void CheckForNamingConflicts()
	{
        var directoriesWithFiles = Files
										.GroupBy(kv => Path.GetDirectoryName(kv.Value.FilePath) ?? "")
										.ToDictionary(
											g => g.Key,
											g => g.Select(kv => kv.Value).ToList()
										);

        filterNonDuplicates(directoriesWithFiles);

        //If no filenames are duplicates, no need to check more
        if (directoriesWithFiles.Count == 0)
		{
            return;
        }

		foreach(var fileGroup in directoriesWithFiles.Values)
		{
			foreach(var file in fileGroup)
			{
				var lastDot = file.FilePath.LastIndexOf('.');
				if(lastDot == -1)
				{
                    Logger.Instance.SetUpRunTimeLogMessage("CheckForNamingConflicts: Error when renaming files: No extension found", true, filename: file.FilePath);
                    continue;
                }
				//Add the original extension to the file name
				var newName = string.Format("{0}_{1}{2}",file.FilePath.Substring(0,lastDot), Path.GetExtension(file.FilePath).ToUpper().TrimStart('.'), Path.GetExtension(file.FilePath));
				file.RenameFile(newName);
			}
		}
		filterNonDuplicates(directoriesWithFiles);
        //If no filenames are duplicates, no need to check more
        if (directoriesWithFiles.Count == 0)
		{
			return;
		}

		//Add number to the file name
		foreach (var fileGroup in directoriesWithFiles.Values)
		{
			foreach (var file in fileGroup)
			{
                var lastDot = file.FilePath.LastIndexOf('.');
				if(lastDot == -1)
				{
					lastDot = file.FilePath.Length;
				}
                //Add a number to the file name
                var newName = string.Format("{0}_{1}{2}", file.FilePath.Substring(0, lastDot), fileGroup.IndexOf(file), Path.GetExtension(file.FilePath));
                file.RenameFile(newName);
            }
		}
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="dict"></param>
	private void filterNonDuplicates(Dictionary<string,List<FileInfo>> dict)
	{
        // Remove groups with only one file name
        var filteredFiles = dict
            .Where(kv => kv.Value.Count > 1)
            .ToDictionary(kv => kv.Key, kv => kv.Value);


        //Remove the files that are not duplicates
        filteredFiles.ForEach(kv =>
        {
            kv.Value.RemoveAll(f => kv.Value
                .Count(x => Path.GetFileNameWithoutExtension(x.FilePath) == Path.GetFileNameWithoutExtension(f.FilePath)) == 1);
        });
        //Remove the keys that have no values
        filteredFiles = filteredFiles.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="files"></param>
	public void AddFiles(List<FileInfo> files)
	{
		foreach (var file in files)
		{
            Guid id = Guid.NewGuid();
            file.Id = id;
            Files.TryAdd(id, file);
        }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="file"></param>
	public void AddFiles(FileInfo file)
	{
		AddFiles(new List<FileInfo> { file });
	}


	/// <summary>
	/// Prints out a grouped list of all identified input file formats and target file formats with pronom codes and full name. <br></br>
	/// Also gives a count of how many files are in each group.
	/// </summary>
	private class FileInfoGroup
	{
        public string CurrentPronom { get; set; }
		public string CurrentFormatName { get; set; }
        public string TargetPronom { get; set; }
		public string TargetFormatName { get; set; }
        public int Count { get; set; }
    }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="pronom"></param>
	/// <returns></returns>
	int ParsePronom(string pronom)
	{
        if (pronom.Contains('/'))
		{
            return int.Parse(pronom.Split('/')[1]);
        }
        return int.MaxValue;
    }

	/// <summary>
	/// 
	/// </summary>
	public void TestDuplicateFilenames()
	{
		var dict = new Dictionary<string, List<FileInfo>>();
		foreach (var file in Files.Values)
		{
			var key = Path.GetFileName(file.FilePath);
            if (dict.ContainsKey(key))
			{
				dict[key].Add(file);
            }
            else
			{
				dict[key] = new List<FileInfo> { file };
            }
        }
		foreach (var entry in dict)
		{
			if(entry.Value.Count > 1)
			{
				foreach (var file in entry.Value)
				{
					Console.WriteLine("{0,50} | {1,80}", entry.Key, file.FilePath);
				}
				Console.WriteLine();
			}
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public void DisplayFileList()
	{

		//Get converters supported formats
		var converters = AddConverters.Instance.GetConverters();
		bool macroDetected = false;

		string notSupportedString = " (Not supported)"; //Needs to have a space in front to extract the pronom code from the string
		string notSetString = "Not set";
		Dictionary<KeyValuePair<string, string>, int> fileCount = new Dictionary<KeyValuePair<string, string>, int>();
		foreach (FileInfo file in Files.Values)
		{

			//Skip files that should be merged or should not be displayed
			if (Settings.ShouldMerge(file))
			{
				continue;
			}
			else if (!file.Display)
			{
				continue;
			}

			string currentPronom = file.NewPronom != "" ? file.NewPronom : file.OriginalPronom;
			string? targetPronom = Settings.GetTargetPronom(file);
			bool supported = false;
			
			if (file.OriginalPronom == "fmt/523" || file.OriginalPronom == "fmt/487" || file.OriginalPronom == "fmt/445")
			{
				macroDetected = true;
            }
			
            //Check if the conversion is supported by any of the converters
            if (targetPronom != null) { 
				converters.ForEach(c =>
				{
					//Check if the conversion is directly supported by the converter or if it is supported by the ConversionManager through different converters
					if (c.SupportsConversion(currentPronom, targetPronom) || ConversionManager.Instance.SupportsConversion(currentPronom, targetPronom))
					{
						supported = true;
						return;
					}
				});
			}
			//If no supported format is found, set the overrideFormat to notSetString
			if (targetPronom == null)
			{
				targetPronom = notSetString;
				file.OutputNotSet = true;
			}
			else if (!supported && targetPronom != currentPronom)
			{
				targetPronom = targetPronom + notSupportedString;
				file.NotSupported = true;
			}
			//Add new entry in dictionary or add to count if entry already exists
			KeyValuePair<string, string> key = new KeyValuePair<string, string>(currentPronom, targetPronom);
			if (fileCount.ContainsKey(key))
			{
				fileCount[key]++;
			}
			else
			{
				fileCount[key] = 1;
			}
		}

		if (macroDetected)
		{
            PrintHelper.PrintLn("One or more macro files detected in '{0}' folder.", GlobalVariables.WARNING_COL, GlobalVariables.parsedOptions.Input);
        }

		var formatList = new List<FileInfoGroup>();
		foreach (KeyValuePair<KeyValuePair<string, string>, int> entry in fileCount)
		{
			formatList.Add(new FileInfoGroup { CurrentPronom = entry.Key.Key, TargetPronom = entry.Key.Value, Count = entry.Value });
		}

		//Find the longest format name for current and target formats
		int currentMax = 0;
		int targetMax = 0;
		foreach (var format in formatList)
		{
			format.CurrentFormatName = PronomHelper.PronomToFullName(format.CurrentPronom);
			format.TargetFormatName = PronomHelper.PronomToFullName(format.TargetPronom);
			if (format.TargetPronom.Contains(notSupportedString))
			{
				var split = format.TargetPronom.Split(" ")[0];
				format.TargetFormatName = PronomHelper.PronomToFullName(split) + notSupportedString;
				format.TargetPronom = split;
			}
			if (format.CurrentFormatName.Length > currentMax)
			{
				currentMax = format.CurrentFormatName.Length;
			}
			if (format.TargetFormatName.Length > targetMax)
			{
				targetMax = format.TargetFormatName.Length;
			}
		}

		//Adjust length to be at least as big as the column name
		currentMax = Math.Max(currentMax, "Full name".Length);
		targetMax = Math.Max(targetMax, "Full name".Length);

		//Sort list
		switch (GlobalVariables.SortBy)
		{
			//Sort by the count of files with the same settings
			//TODO: Ask archive if they want the not set and not supported files to be at the bottom or in between the other files
			case PrintSortBy.Count:
				formatList = formatList.OrderBy(x => x.TargetPronom == notSetString || x.TargetFormatName.Contains(notSupportedString))
					.ThenByDescending(x => x.Count)
					.ThenBy(x => ParsePronom(x.CurrentPronom))
					.ToList();
				break;
			//Sort by the current or target pronom code with count as a tiebreaker
			case PrintSortBy.CurrentPronom:
			case PrintSortBy.TargetPronom:
				bool current = GlobalVariables.SortBy == PrintSortBy.CurrentPronom; //True if sorting by current pronom
				formatList = formatList
					.OrderBy(x => ParsePronom(current ? x.CurrentPronom : x.TargetPronom))
					.ThenByDescending(x => x.Count) //Tiebreaker is count
					.ToList();
				break;
		}

		var firstFormatTitle = ConversionFinished ? "Actual pronom" : "Input pronom";
		var secondFormatTitle = ConversionFinished ? "Target pronom" : "Output pronom";

		//Print the number of files per pronom code
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = GlobalVariables.INFO_COL;
		Console.WriteLine("\n{0,13} - {1,-" + currentMax + "} | {2,13} - {3,-" + targetMax + "} | {4,6}", firstFormatTitle, "Full name", secondFormatTitle, "Full name", "Count");

		foreach (var format in formatList)
		{
			//Set color based on the status for the format
			Console.ForegroundColor = oldColor;
			if (format.TargetFormatName.Contains(notSupportedString))
			{
				Console.ForegroundColor = GlobalVariables.ERROR_COL;
			}
			else if (format.TargetPronom == "Not set")
			{
				Console.ForegroundColor = GlobalVariables.WARNING_COL;
			}

			if (format.TargetPronom != format.CurrentPronom || format.TargetFormatName.Contains(notSupportedString))
			{
				Console.WriteLine("{0,13} - {1,-" + currentMax + "} | {2,13} - {3,-" + targetMax + "} | {4,6}", format.CurrentPronom, format.CurrentFormatName, format.TargetPronom, format.TargetFormatName, format.Count);
			}
			else
			{
				PrintStrikeThrough(format, currentMax, targetMax);
			}
		}

		//Sum total from all entries in fileCount where key. is not "Not set" or "Not supported"
		int total = formatList.Where(x => x.TargetPronom != notSetString && !x.TargetFormatName.Contains(notSupportedString)).Sum(x => x.Count);
		//Sum total from all entries in fileCount where the input pronom is the same as the output pronom
		int totalFinished = formatList.Where(x => x.CurrentPronom == x.TargetPronom).Sum(x => x.Count);
		//Print totals to user
		Console.ForegroundColor = GlobalVariables.INFO_COL;
		Console.WriteLine("\nNumber of files: {0,-10}", Files.Count);
		Console.WriteLine("Number of files with supported output specified: {0,-10}", total);
		Console.WriteLine("Number of files not at target pronom: {0,-10}", total - totalFinished);
		//Get a list of all directories that will be merged
		List<(string, string)> dirsToBeMerged = new List<(string, string)>();
		foreach (var entry in GlobalVariables.FolderOverride)
		{
			if (entry.Value.Merge)
			{
				dirsToBeMerged.Add((entry.Key, entry.Value.DefaultType));
			}
		}
		//Print out the directories that will be or have been merged
		if (dirsToBeMerged.Count > 0)
		{
			int maxLength = 0;
			foreach (var dir in dirsToBeMerged)
			{
				if (dir.Item1.Length > maxLength)
				{
					maxLength = dir.Item1.Length;
				}
			}
			//Print plan for merge
			if (!ConversionFinished)
			{
				Console.WriteLine("Some folders will be merged (output pronom):");
				foreach (var dir in dirsToBeMerged)
				{
					var relPath = Path.Combine(GlobalVariables.parsedOptions.Output, dir.Item1);
					var totalFiles = Directory.Exists(relPath) ? Directory.GetFiles(relPath).Count() : -1;
					Console.WriteLine("\t{0,-" + maxLength + "} | {1} files ({2})", dir.Item1, totalFiles, dir.Item2);
				}
			}
			else    //Check result of merge
			{
				List<string> mergedDirs = new List<string>();
				foreach (var file in Files.Values)
				{
					var parent = Path.GetRelativePath(GlobalVariables.parsedOptions.Output, Directory.GetParent(file.FilePath)?.ToString() ?? "");
					//Check if file was merged, only add the parent directory once
					if (!mergedDirs.Contains(parent) && dirsToBeMerged.Any(tuple => tuple.Item1 == parent) && file.IsMerged)
					{
						mergedDirs.Add(parent);
					}
				}
				//Get the directories that were not merged
				var notMerged = dirsToBeMerged.Where(tuple => !mergedDirs.Contains(tuple.Item1)).ToList();
				//Print out the result of the merge
				Console.WriteLine("{0}/{1} folders were merged:", mergedDirs.Count, dirsToBeMerged.Count);
				Console.ForegroundColor = GlobalVariables.SUCCESS_COL;
				foreach (var dir in mergedDirs)
				{
					Console.WriteLine("\t{0}", dir);
				}
				Console.ForegroundColor = GlobalVariables.ERROR_COL;
				foreach (var dir in notMerged)
				{
					Console.WriteLine("\t{0}", dir);
				}
			}
		}

		Console.ForegroundColor = oldColor;

	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
    public List<FileInfo> GetFiles()
	{
		lock (identifyingFiles)
		{
			return Files.Values.ToList();
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public FileInfo? GetFile(Guid id)
	{
		if (!Files.ContainsKey(id)) return null;
		return Files[id];
	}

	/// <summary>
	/// 
	/// </summary>
	public void DocumentFiles()
	{
		Logger logger = Logger.Instance;
		List<FileInfo> files = new List<FileInfo>();
		foreach (var file in Files)
		{
            files.Add(file.Value);
        }
		logger.SetUpDocumentation(files);
	}

    //******************************************************      Helper functions for formatting output      ***************************************************************//
    /// <summary>
	/// Creates a strikethrough string with the specified length <br></br>
	/// The padding for length is spaces on the left (negative length) or right (positive length) side of the text
	/// </summary>
	/// <param name="text">string that should be formatted</param>
	/// <param name="length">total length of string after padding (either before or after)</param>
	/// <returns>formatted string</returns>
	static string StrikeThrough(string text, int length)
    {
        return (length > 0) 
						? (new string(' ',length - text.Length) + $"\u001b[9m{text}\u001b[0m") //Padding to front of string
						: ($"\u001b[9m{text}\u001b[0m") + (new string(' ', (-length) - text.Length)); // Padding to end of string
    }

	/// <summary>
	/// Prints a GlobalVariables.SUCCESS_COL colored line for DisplayFiles() with a strikethrough for the target format name and pronom, to be used if the current and target pronom are the same
	/// </summary>
	/// <param name="f">FileInfoGroup that should be printed</param>
	/// <param name="currentMax">Maximum length of current format name</param>
	/// <param name="targetMax">Maximum length of target format name</param>
    static void PrintStrikeThrough(FileInfoGroup f, int currentMax, int targetMax)
	{
		Console.ForegroundColor = GlobalVariables.SUCCESS_COL;
		string targetPronom = StrikeThrough(f.TargetPronom, 13);
		string targetFormatName = StrikeThrough(f.TargetFormatName, -targetMax);
		Console.Write("{0,13} - {1,-" + currentMax + "} | {2} ", f.CurrentPronom, f.CurrentFormatName, targetPronom);
		Console.ForegroundColor = GlobalVariables.SUCCESS_COL;
		Console.Write("- {0,-" + targetMax + "} ",targetFormatName);
        Console.ForegroundColor = GlobalVariables.SUCCESS_COL;
        Console.Write("| {0,6}\n",f.Count);
	}
}
