using SharpCompress;
using System.Collections.Concurrent;
using ConversionTools.Converters;
using ConversionTools;
using FileConverter.HelperClasses;
using SF = FileConverter.Siegfried;

namespace FileConverter.Managers
{
	public class FileToConvert
	{
		public string FilePath { get; set; }            //From FileInfo
		public string CurrentPronom { get; set; }       //From FileInfo
		public string TargetPronom { get; set; }        //From Dictionary
		public List<string> Route { get; set; }         //From Dictionary
		public bool IsModified { get; set; } = false;   //True if file has been worked on
		public bool Failed { get; set; } = false;       //True if the file has failed conversion without throwing an exception
		public bool addedDuringRun { get; set; } = false; //True if the file has been added while the conversion was running
		public Guid Id { get; set; }   //Unique identifier for the file

		public FileToConvert(FileInfo2 file)
		{
			FilePath = file.FilePath;
			CurrentPronom = file.OriginalPronom;
			TargetPronom = ConversionSettings.GetTargetPronom(file) ?? CurrentPronom;
			Route = new List<string>();
			Id = file.Id;
		}
		public FileToConvert(string path, Guid id, string target)
		{
			FilePath = path;
			CurrentPronom = "";
			TargetPronom = target;
			Route = new List<string>();
			IsModified = false;
			Id = id;
			Route.Add(target);
		}
	}

	public class ConversionManager
	{
		public ConcurrentDictionary<KeyValuePair<string, string>, List<string>> ConversionMap { get; set; } = new ConcurrentDictionary<KeyValuePair<string, string>, List<string>>();
		public ConcurrentDictionary<Guid, FileInfo2> FileInfoMap { get; set; } = new ConcurrentDictionary<Guid, FileInfo2>();
		public ConcurrentDictionary<Guid, FileToConvert> WorkingSet { get; set; } = new ConcurrentDictionary<Guid, FileToConvert>();
		public Dictionary<string, string> WorkingSetMap { get; set; } = new Dictionary<string, string>();
		private static ConversionManager? instance;
		private static readonly object lockObject = new object();
		readonly List<Converter> Converters;

		/// <summary>
		/// initializes the map for how to reach each format
		/// </summary>
		private void InitMap()
		{
			EmailConverter emailConverter = new EmailConverter();
            List<string> supportedConversionsEmail = new(emailConverter.SupportedConversions?.Keys ?? Enumerable.Empty<string>());
			string pdfA2B = "fmt/477";
			string pdfPronomForEmail = "fmt/18";
			string emlConversionPronom = "fmt/950";

			foreach (FileInfo2 file in FileManager.Instance.Files.Values)
			{
				// MSG to PDFA-2B via eml and PDF 1.4
				if (ConversionSettings.GetTargetPronom(file) == pdfA2B && emailConverter.MSGPronoms.Contains(file.OriginalPronom)
																		&& supportedConversionsEmail.Contains(file.OriginalPronom))
				{
					ConversionMap.TryAdd(new KeyValuePair<string, string>(file.OriginalPronom, pdfA2B), [emlConversionPronom, pdfPronomForEmail, pdfA2B]);
				}
				// MSG to PDF 1.4 via eml
				if (ConversionSettings.GetTargetPronom(file) == pdfPronomForEmail && emailConverter.MSGPronoms.Contains(file.OriginalPronom)
																		&& supportedConversionsEmail.Contains(file.OriginalPronom))
				{
					ConversionMap.TryAdd(new KeyValuePair<string, string>(file.OriginalPronom, pdfPronomForEmail), [emlConversionPronom, pdfPronomForEmail]);
				}
				//EML to PDFA-2B via PDF 1.4
				if (ConversionSettings.GetTargetPronom(file) == pdfA2B && emailConverter.EMLPronoms.Contains(file.OriginalPronom)
																		&& supportedConversionsEmail.Contains(file.OriginalPronom))
				{
					ConversionMap.TryAdd(new KeyValuePair<string, string>(file.OriginalPronom, pdfA2B), [pdfPronomForEmail, pdfA2B]);
				}
			}
		}

		public void InitFileMap()
		{
			foreach (FileInfo2 file in FileManager.Instance.Files.Values)
			{
				FileInfoMap.TryAdd(file.Id, file);
			}
		}

		/// <summary>
		/// Removes the entries in the ConversionMap where a part of the route is not supported by any converter present.
		/// </summary>
		void FilterConversionMap()
		{
			var toDelete = new List<KeyValuePair<string, string>>();
			Parallel.ForEach(ConversionMap, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, entry =>
			{
				bool supported = false;
				var route = entry.Value;
				var prev = entry.Key.Key;
				foreach (var pronom in route)
				{
                    supported = Converters.Any(c => c.SupportsConversion(prev, pronom));
                    if (!supported)
					{
						toDelete.Add(entry.Key);
						return;
					}
					prev = pronom;
				}
			});

			//Remove all entries that are not supported by any converter
			foreach (var key in toDelete)
			{
				ConversionMap.TryRemove(key, out _);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ConversionManager()
		{
			//Initialize conversion map
			InitMap();

			//Initialize converters
			Converters = AddConverters.Instance.GetConverters();
			List<string> existingConverters = ["Libreoffice", "iText7", "EmailConverter", "Ghostscript"];
			foreach (string converterName in existingConverters)
			{
				if (!Converters.Any(c => c.Name == converterName))
				{
					PrintHelper.PrintLn("Dependencies for '{0}' converter not present", GlobalVariables.WARNING_COL, converterName);
				}
			}

			//Initialize FileMap
			InitFileMap();
			FilterConversionMap();
		}

		public static ConversionManager Instance
		{
			get
			{
				lock (lockObject)
				{
                    if (instance == null)
					{
						instance = new ConversionManager();
					}
					return instance;
				}
			}
		}

		public bool SupportsConversion(string currentPronom, string targetPronom)
		{
			return ConversionMap.ContainsKey(new KeyValuePair<string, string>(currentPronom, targetPronom));
		}

		/// <summary>
		/// Updates the FileInfo list with new data after conversion
		/// </summary>
		/// <summary>
		/// Updates the FileInfo list with new data after conversion
		/// </summary>
		static void CheckConversion()
		{
			var files = FileManager.Instance.Files.Values.ToList();
			//Run siegfried on all files
			var f = SF.Siegfried.Instance.IdentifyFilesIndividually(files)?.Result;

			//If siegfried fails, log error message and return
			if (f == null)
			{
				Console.WriteLine("Could not identify files after conversion");
				Logger.Instance.SetUpRunTimeLogMessage("CM CheckConversion: Could not identify files", true);
				return;
			}
			Dictionary<Guid, FileInfo2> fDict = f.ToDictionary(x => x.Id, x => x);

			//Update FileInfoMap with new data
			files.ForEach(file =>
			{
                if (fDict.TryGetValue(file.Id, out var dictValue))
                {
                    file.UpdateSelf(dictValue);
                    file.IsConverted = ConversionSettings.GetTargetPronom(file) == file.NewPronom;
                }
            });
		}

		/// <summary>
		/// Responsible for managing the convertion and combining of all files
		/// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task ConvertFiles()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			int maxThreads = GlobalVariables.MaxThreads;
			Dictionary<string, List<FileInfo2>> mergingFiles = new Dictionary<string, List<FileInfo2>>();
			Console.WriteLine("Setting up working set...");
			//Initialize working set
			SetupWorkingSet(WorkingSet, mergingFiles);  //Initialize working set

			if (mergingFiles.Count > 0)
			{
				Console.WriteLine("Sending files to be merged");
			}
			Task combineTask = Task.Run(() => SendToCombineFiles(mergingFiles));           //Combine files 

			//Set max threads for the thread pool based on global variables
			ThreadPool.SetMinThreads(maxThreads, maxThreads);
			ThreadPool.SetMaxThreads(maxThreads, maxThreads);
			Console.WriteLine("Starting conversion...");
			//Repeat until all files have been converted/checked or there was no change during last run
			while (!WorkingSet.IsEmpty)
			{
				ConcurrentDictionary<Guid, CountdownEvent> countdownEvents = new ConcurrentDictionary<Guid, CountdownEvent>();
				//Reset the working set map for the next run
				WorkingSetMap.Clear();
				var totalQueued = 0;
				//Loop through working set
				WorkingSet.Values.ForEach(file =>
				{
					//Loop through converters
					foreach (Converter converter in Converters)
					{
						//Check if the converter supports the format of the file
						if (file.Route.Count < 1 || !converter.SupportsConversion(file.CurrentPronom, file.Route.First()))
						{
							continue;
						}
						totalQueued++;
						//Send file to converter
						SendToConverter(file, converter, countdownEvents);
						return;
					}
				});

				//Wait for all threads to finish
				AwaitConversion(countdownEvents);
				try
				{
					//Remove files that are finished on and update the rest
					UpdateWorkingSet(WorkingSet);
				}
				catch (Exception e)
				{
					Logger.Instance.SetUpRunTimeLogMessage("CM ConvertFiles: " + e.Message, true);
				}
			}
			//Wait for the combine task to finish
			if (!combineTask.IsCompleted)
			{
				Console.WriteLine("Waiting for combine task to finish...");
				combineTask.Wait();
			}

			Console.WriteLine("Checking conversion status...");

			//TODO: Maybe look into checking file when they are removed from WorkingSet, not just all files at the end
			//Update FileInfo list with new data after conversion
			CheckConversion();
		}

		/// <summary>
		/// Initialises the working set with files to be converted based on ConversionSettings
		/// </summary>
		/// <param name="ws">Working set to add files to</param>
		/// <param name="mf">Files that should be combined</param>
		void SetupWorkingSet(ConcurrentDictionary<Guid, FileToConvert> ws, Dictionary<string, List<FileInfo2>> mf)
		{
			foreach (FileInfo2 file in Managers.FileManager.Instance.Files.Values)
			{
				//Create a new FileToConvert object
				var newFile = new FileToConvert(file);

				//Check if the file should be overridden by a folder override
				bool addToWorkingSet = CheckInOverride(file, newFile, mf);

				//Use current and target pronom to create a key for the conversion map
				var key = new KeyValuePair<string, string>(newFile.CurrentPronom, newFile.TargetPronom);

                //If the conversion map contains the key, set the route to the value of the key
                if (ConversionMap.TryGetValue(key, out var value))
                {
                    newFile.Route = new List<string>(value);
                }
                //If the conversion map does not contain the key, set the route to the target pronom
                else if (newFile.CurrentPronom != newFile.TargetPronom)
				{
					newFile.Route.Add(newFile.TargetPronom);
				}
				else
				{
					continue;
				}
				file.Route = newFile.Route;
				//Add the file to the working set if it was not set to be merged
				if (addToWorkingSet)
				{
					//Try to add the file to the working set
					bool added = ws.TryAdd(file.Id, newFile);
					if (!added)
					{
						Logger.Instance.SetUpRunTimeLogMessage("CM ConvertFiles: Could not add file to working set: " + file.FilePath, true);
					}
				}
			}
		}

		/// <summary>
		/// Updates the data in the working set and removes files that are done or failed conversion after 3 attempts
		/// </summary>
		/// <param name="ws">Workingset to be updated</param>
		static void UpdateWorkingSet(ConcurrentDictionary<Guid, FileToConvert> ws)
		{
			ConcurrentBag<Guid> filesToRemove = new ConcurrentBag<Guid>();

			ws.Values.ForEach(file =>
			{
				//If the file was not modified or failed the conversion, remove it from the WorkingSet
				if ((!file.IsModified || file.Failed) && !file.addedDuringRun)
				{
					filesToRemove.Add(file.Id);
					return;
				}
				//Reset the IsModified flag
				file.IsModified = false;

				if (!file.addedDuringRun)
				{
					//Update the current pronom to the pronom it was converted to
					//This assumes that the Converter has correctly identified if the file was converted correctly or not
					file.CurrentPronom = file.Route.First();

					//Remove the first step in the route, if it was added during run removing
					//first route would delete the file before it gets converted
					file.Route.RemoveAt(0);
				}

				file.addedDuringRun = false;

				// Remove if there are no more steps in route
				if (file.Route.Count == 0)
				{
					filesToRemove.Add(file.Id);
				}
			});

			//Try to remove all items that were marked in the loop above
			foreach (var item in filesToRemove)
			{
				ws.TryRemove(item, out _); // Try to remove the item from ConcurrentBag
			}
		}

		/// <summary>
		/// Sends a file to a converter and adds a CountdownEvent to a countdownEvents dictionary to be waited for later
		/// </summary>
		/// <param name="f">File that should convert</param>
		/// <param name="c">Converter that will do the conversion</param>
		/// <param name="threads">Where the ThreadInfo will be added</param>
		/// <returns>True if the conversion succeeded, False if not</returns>
		void SendToConverter(FileToConvert f, Converter c, ConcurrentDictionary<Guid, CountdownEvent> countdownEvents)
		{
			var countdownEvent = new CountdownEvent(1);
			//Try to add a new CountdownEvent to the dictionary with the file path as key
			bool added = countdownEvents.TryAdd(f.Id, countdownEvent);
			if (!added)
			{
				Logger.Instance.SetUpRunTimeLogMessage("CM SendToConverter: Could not add countdown event: " + f.FilePath, true);
			}
			//Save return value of QueueUserWorkItem
			bool queued = ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					//Add the name of the converter to the file if the previous entry is not the same converter for documentation
					if (c.NameAndVersion != null &&
						(FileInfoMap[f.Id].ConversionTools.Count == 0 || FileInfoMap[f.Id].ConversionTools.Last() != c.NameAndVersion))
					{
						FileInfoMap[f.Id].ConversionTools.Add(c.NameAndVersion);
					}
					//Send file to converter
					Task.Run(async () =>
					{
						await c.ConvertFile(f);
						f.IsModified = true;
						if (!countdownEvent.IsSet)
						{
							countdownEvent.Signal();
						}
					});
				}
				catch (Exception e)
				{
					//Set success to false and log the error message if an exception was thrown
					Logger.Instance.SetUpRunTimeLogMessage("CM SendToConverter: Error when converting file: " + e.Message, true);
					if (!countdownEvent.IsSet)
					{
						countdownEvent.Signal();
					}
				}
			});
			if (!queued)
			{
				//Log an error message if the file could not be queued
				Logger.Instance.SetUpRunTimeLogMessage("CM SendToConverter: Could not queue user work item: " + f.FilePath, true);
				countdownEvent.Signal();
			}
		}

		/// <summary>
		/// Waits for all CountdownEvents in a dictionary to be signaled
		/// </summary>
		/// <param name="threads">The dictionary of ThreadInfos that should be waited for</param>
		/// <param name="total">The total number of thread jobs queued</param>
		static void AwaitConversion(ConcurrentDictionary<Guid, CountdownEvent> countdownEvents)
		{
			int total = countdownEvents.Count;
			var startTime = DateTime.Now;
			using (ProgressBar pb = new ProgressBar(total))
			{
				int numFinished = 0;
				while (numFinished < countdownEvents.Count)
				{
					numFinished = countdownEvents.Values.Count(c => c.IsSet);
					var elapsed = DateTime.Now - startTime;
					pb.Report((float)(numFinished) / (float)total, numFinished, elapsed);
					Thread.Sleep(1000);
				}
			}
		}

		/// <summary>
		/// Sends files to be combined
		/// </summary>
		/// <param name="mergingFiles">Dictionary with a List of all files that should be combined</param>
		static Task SendToCombineFiles(Dictionary<string, List<FileInfo2>> mergingFiles)
		{
			try
			{
				Parallel.ForEach(mergingFiles, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.MaxThreads }, entry =>
				{
					var converter = new IText7();
					var outputPronom = GlobalVariables.FolderOverride[entry.Key].DefaultType;
					converter.CombineFiles(entry.Value, outputPronom);
					foreach (FileInfo2 file in entry.Value)
					{
						file.ConversionTools.Add(converter.NameAndVersion);
					}
				});
			}
			catch (Exception e)
			{
				Logger.Instance.SetUpRunTimeLogMessage("CM SendToCombineFiles: " + e.Message, true);
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Checks if a file should be overridden by a folder override
		/// </summary>
		/// <param name="parentDirName"></param>
		/// <param name="file"></param>
		/// <param name="newFile"></param>
		/// <param name="mergingFiles"></param>
		/// <returns>True if the file should be converted, False if it should be merged</returns>
		static bool CheckInOverride(FileInfo2 file, FileToConvert newFile, Dictionary<string, List<FileInfo2>> mergingFiles)
		{
			//Check if the file is in a folder that should be overridden
			string? parentDirName = Path.GetDirectoryName(Path.GetRelativePath(GlobalVariables.ParsedOptions.Output, file.FilePath));
            if (parentDirName == null || !GlobalVariables.FolderOverride.TryGetValue(parentDirName, out _))
            {
                return true;
            }

            foreach (string pronom in GlobalVariables.FolderOverride[parentDirName].PronomsList)
			{
				if (file.OriginalPronom != pronom)
				{
					continue;
				}
				if (!GlobalVariables.FolderOverride[parentDirName].Merge)
				{
					newFile.TargetPronom = GlobalVariables.FolderOverride[parentDirName].DefaultType;
				}
				else
				{
                    // Check if the key exists in the dictionary
                    if (!mergingFiles.TryGetValue(parentDirName, out _))
                    {
                        // If the key does not exist, add it along with a new list
                        mergingFiles[parentDirName] = new List<FileInfo2>();
                    }
                    file.ShouldMerge = true;
					// Add the file to the list associated with the key
					mergingFiles[parentDirName].Add(file);
					return false;
				}
			}
			return true;
		}
	}
}