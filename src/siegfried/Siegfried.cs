using SharpCompress.Archives;
using SharpCompress.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileConverter.HelperClasses;

namespace FileConverter.Siegfried
{
	public class SiegfriedJSON
	{
		[JsonPropertyName("siegfried")]
		public string siegfriedVersion = "";
		[JsonPropertyName("scandate")]
		public string scandate = "";
		[JsonPropertyName("files")]
		public SiegfriedFile[] files = [];
	}

	public class SiegfriedFile
	{
		[JsonPropertyName("filename")]
		public string filename = "";
		[JsonPropertyName("filesize")]
		public long filesize = 0;
		[JsonPropertyName("modified")]
		public string modified = "";
		[JsonPropertyName("errors")]
		public string errors = "";
		public string hash = "";
		[JsonPropertyName("matches")]
		public SiegfriedMatches[] matches = [];
	}
	public class SiegfriedMatches
	{
		[JsonPropertyName("ns")]
		public string ns = "";
		[JsonPropertyName("id")]
		public string id = "";
		[JsonPropertyName("format")]
		public string format = "";
		[JsonPropertyName("version")]
		public string version = "";
		[JsonPropertyName("mime")]
		public string mime = "";
		[JsonPropertyName("class")]
		public string class_ = "";
		[JsonPropertyName("basis")]
		public string basis = "";
		[JsonPropertyName("warning")]
		public string warning = "";
	}

	public class Siegfried
	{
		private static Siegfried? instance;
		public string? Version = null;
		public string? ScanDate = null;
		public string OutputFolder = "siegfried/JSONoutput";
		private string ExecutableName = OperatingSystem.IsLinux() ? "sf" : "sf.exe";
		private string HomeFolder = "siegfried/";
		private string PronomSignatureFile = "default.sig";      //"pronom64k.sig";
		public int Multi = 64;
		public int groupSize = 256;
		private static readonly object lockObject = new object();
		public List<List<string>> CompressedFolders;
		public ConcurrentBag<FileInfo2> Files = new ConcurrentBag<FileInfo2>();
		public static Siegfried Instance
		{
			get
			{
				if (instance == null)
				{
					lock (lockObject)
					{
						if (instance == null)
						{
							instance = new Siegfried();
						}
					}
				}
				return instance;
			}
		}

		private Siegfried()
		{
			Logger logger = Logger.Instance;
			CompressedFolders = new List<List<string>>();
			//Look for Siegfried files
			if (OperatingSystem.IsWindows())
			{
				GetExecutable();
				var found = Path.Exists(ExecutableName);
				logger.SetUpRunTimeLogMessage("SF Siegfried executable " + (found ? "" : "not ") + "found", !found);
				if (!found)
				{
					Console.WriteLine("Cannot find Siegfried executable");
				}
				found = Path.Exists(HomeFolder + PronomSignatureFile);
				logger.SetUpRunTimeLogMessage($"SF Pronom signature file '{PronomSignatureFile}' " + (found ? "" : "not ") + "found", !found);
				if (!found)
				{
					Console.WriteLine("Cannot find Pronom signature file");
				}
			}
			else if (OperatingSystem.IsLinux())
			{
				ProcessStartInfo startInfo = new ProcessStartInfo();
				startInfo.FileName = "/bin/bash";
				startInfo.Arguments = "-c \" " + "sf -version" + " \"";
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;
				startInfo.CreateNoWindow = true;
				startInfo.RedirectStandardError = true;

				try
				{
					Process process = new Process();
					process.StartInfo = startInfo;
					process.Start();
					process.WaitForExit();
					string output = process.StandardOutput.ReadToEnd();
					if (!output.Contains("siegfried"))
					{
						throw new FileNotFoundException("Cannot find Siegfried on Linux");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		void GetExecutable()
		{
			if (!OperatingSystem.IsWindows())
			{
				return;
			}
			string filename = "sf.exe";
			string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), filename, SearchOption.AllDirectories);
			if (files.Length > 0)
			{
				ExecutableName = files[0];
				HomeFolder = Path.GetDirectoryName(ExecutableName) + "/";
			}
		}
		public void AskReadFiles()
		{
			//Check if json files exist
			if (Directory.Exists(OutputFolder) && Directory.GetFiles(OutputFolder, "*.*", SearchOption.AllDirectories).Count() > 0)
			{
				char input;
				do
				{
					Console.Write("Siegfried data found, do you want to parse it? (Y/N): ");
					input = char.ToUpper(Console.ReadKey().KeyChar);
				} while (input != 'Y' && input != 'N');
				Console.WriteLine();
				if (input == 'Y')
				{
					ReadFromFiles();
				}
			}
		}

		public void ClearOutputFolder()
		{
			if (Directory.Exists(OutputFolder))
			{
				try
				{
					Directory.Delete(OutputFolder, true);
				}
				catch
				{
					Logger logger = Logger.Instance;
					logger.SetUpRunTimeLogMessage("SF Could not delete Siegfried output folder", true);
				}
			}
		}

		public static string HashEnumToString(HashAlgorithms hash)
		{
			switch (hash)
			{
				case HashAlgorithms.MD5:
					return "md5";
				default:
					return "sha256";
			}
		}

		private void ReadFromFiles()
		{
			if (instance == null)
			{
				instance = new Siegfried();
			}
			//TODO: Compressed files are not handled correctly here
			var paths = Directory.GetFiles(OutputFolder, "*.*", SearchOption.AllDirectories);
			using (ProgressBar progressBar = new ProgressBar(paths.Length))
			{
				for (int i = 0; i < paths.Length; i++)
				{
					var parsedData = ParseJSONOutput(paths[i], true);
					if (parsedData == null)
						return; //TODO: Check error and possibly continue

					if (instance.Version == null || instance.ScanDate == null)
					{
						instance.Version = parsedData.siegfriedVersion;
						instance.ScanDate = parsedData.scandate;
					}

					foreach (var f in parsedData.files)
					{
						instance.Files.Add(new FileInfo2(f));
					}
				}
			}
		}

		/// <summary>
		/// Returns the pronom id of a specified file
		/// </summary>
		/// <param name="path">Path to file</param>
		/// <param name="hash">True if file should be hashed</param>
		/// <returns>Parsed SiegfriedFile or null</returns>
		public SiegfriedFile? IdentifyFile(string path, bool hash)
		{
			// Wrap the file path in quotes
			string wrappedPath = "\"" + path + "\"";
			string options;

			if (OperatingSystem.IsWindows())
			{
				options = String.Format("-coe -home {0} -json {1} -sig {2} ", HomeFolder, hash ? "-hash " + HashEnumToString(GlobalVariables.checksumHash) : "", PronomSignatureFile);
			}
			else
			{
				options = String.Format("-coe -json {0}", hash ? "-hash " + HashEnumToString(GlobalVariables.checksumHash) : "");
			}

			// Define the process start info
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = $"{ExecutableName}", // or any other command you want to run
				Arguments = options + wrappedPath,
				RedirectStandardInput = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			string error = "";
			string output = "";
			try
			{
				// Create the process
				using (Process process = new Process { StartInfo = psi })
				{
					process.Start();

					output = process.StandardOutput.ReadToEnd();
					error = process.StandardError.ReadToEnd();

					process.WaitForExit();
				}
			}
            catch (Exception e)
			{
                Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyFile: " + e.Message, true);
            }
			//TODO: Check error and possibly continue
			if (error.Length > 0)
			{
				Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyFile: " + error, true);
			}
			var parsedData = ParseJSONOutput(output, false);
			if (parsedData == null || parsedData.files == null)
				return null;

			if (parsedData.files.Length > 0)
			{
				return parsedData.files[0];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns a SiegfriedFile list of a specified file array
		/// </summary>
		/// <param name="paths">Array of file paths to </param>
		/// <returns>Pronom id or null</returns>
		public List<FileInfo2>? IdentifyList(string[] paths)
		{
			Logger logger = Logger.Instance;
			var files = new List<FileInfo2>();

			if (paths.Length < 1)
			{
				return null;
			}
			string[] tempPaths = new string[paths.Length];
			// Wrap the file paths in quotes
			for (int i = 0; i < paths.Length; i++)
			{
				//tempPaths[i] = Path.GetFullPath(paths[i]);
				tempPaths[i] = "\"" + paths[i] + "\"";
			}
			string wrappedPaths = String.Join(" ", tempPaths);
			string options;
			if (OperatingSystem.IsWindows())
			{
				options = String.Format("-home {0} -json {1} -sig {2} -multi {3} ", HomeFolder, "" /*"-hash " + HashEnumToString(GlobalVariables.checksumHash)*/, PronomSignatureFile, Multi);
			}
			else
			{
				options = String.Format("-json {0}", "-hash " + HashEnumToString(GlobalVariables.checksumHash));
			}

			string outputFile = Path.Combine(OutputFolder, Guid.NewGuid().ToString() + ".json");
			string? parentDir = Directory.GetParent(outputFile)?.FullName;

			//Create output file
			try
			{
				if (parentDir != null && !Directory.Exists(parentDir))
				{
					Directory.CreateDirectory(parentDir);
				}
				if (parentDir != null)
				{
					File.Create(outputFile).Close();
				}
				else
				{
					Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: parentDir is null " + outputFile, true);
				}
			}
			catch (Exception e)
			{
				Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: could not create output file " + e.Message, true);
			}
			
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = $"{ExecutableName}", // or any other command you want to run
				Arguments = options + wrappedPaths,
				RedirectStandardInput = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			string error = "";
			try
			{
				// Create the process
				using (Process process = new Process { StartInfo = psi })
				{
					// Create the StreamWriter to write to the file
					using (StreamWriter sw = new StreamWriter(outputFile))
					{
						// Set the output stream for the process
						process.OutputDataReceived += (sender, e) => { if (e.Data != null) sw.WriteLine(e.Data); };

						// Start the process
						process.Start();

						// Begin asynchronous read operations for output and error streams
						process.BeginOutputReadLine();
						error = process.StandardError.ReadToEnd();

						// Wait for the process to exit
						process.WaitForExit();
					}
				}
			}
            catch (Exception e)
			{
                Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: " + e.Message, true);
            }
			//TODO: Check error and possibly continue
			if (error.Length > 0)
			{
				//Remove \n from error message
				error = error.Replace("\n", " - ");
				Logger.Instance.SetUpRunTimeLogMessage("SF IdentifyList: " + error, true);
			}
			var parsedData = ParseJSONOutput(outputFile, true);
			if (parsedData == null)
				return null; //TODO: Check error and possibly continue
			if (Version == null || ScanDate == null)
			{
				Version = parsedData.siegfriedVersion;
				ScanDate = parsedData.scandate;
			}
			for (int i = 0; i < parsedData.files.Length; i++)
			{
				var file = new FileInfo2(parsedData.files[i]);
				file.FilePath = paths[i];
				file.OriginalFilePath = Path.GetFileName(file.FilePath);
				var pathWithoutInput = file.FilePath.Replace(GlobalVariables.parsedOptions.Input, "");
				file.ShortFilePath = Path.Combine(pathWithoutInput.Replace(GlobalVariables.parsedOptions.Output, ""));
				while (file.ShortFilePath[0] == '\\')
				{
					//Remove leading backslashes
					file.ShortFilePath = file.ShortFilePath.Substring(1);
				}
				files.Add(file);
			}
			return files;
		}

		/// <summary>
		/// Identifies all files in input directory and returns a List of FileInfo objects.
		/// </summary>
		/// <param name="input">Path to root folder for search</param>
		/// <returns>A List of identified files</returns>
		public Task<List<FileInfo2>>? IdentifyFilesIndividually(string input)
		{
			Logger logger = Logger.Instance;
			var files = new ConcurrentBag<FileInfo2>();
			List<string> filePaths = new List<string>(Directory.GetFiles(input, "*.*", SearchOption.AllDirectories));
			ConcurrentBag<string[]> filePathGroups = new ConcurrentBag<string[]>(GroupPaths(filePaths));

			Parallel.ForEach(filePathGroups, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, filePaths =>
			{
				var output = IdentifyList(filePaths);
				if (output == null)
				{
					logger.SetUpRunTimeLogMessage("SF IdentifyFilesIndividually: could not identify files", true);
					return; //Skip current group
				}
				//TODO: Check if all files were identified

				foreach (var f in output)
				{
					files.Add(f);
				}
			});

			return Task.FromResult(files.ToList());
		}

		/// <summary>
		/// Less parallelised version of IdentifyFilesIndividually, but ensures that the given files are correctly updated and the result is tied to the same file ID.
		/// </summary>
		/// <param name="input">Path to root folder for search</param>
		/// <returns>A List of identified files</returns>
		public Task<List<FileInfo2>>? IdentifyFilesIndividually(List<FileInfo2> inputFiles)
		{
			Logger logger = Logger.Instance;
			var files = new ConcurrentBag<FileInfo2>();

			Parallel.ForEach(inputFiles, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, file =>
			{
				//Skip files that should be merged (they may not exist anymore and are documented in other methods)
				if (file.ShouldMerge || file.IsDeleted)
				{
					return;
				}
				var result = IdentifyFile(file.FilePath, true);
				if (result == null)
				{
					logger.SetUpRunTimeLogMessage("SF IdentifyFilesIndividually: could not identify file", true, filename: file.FilePath);
					return; //Skip current file
				}
				var newFile = new FileInfo2(result);
				newFile.Id = file.Id;
				files.Add(newFile);
			});

			return Task.FromResult(files.ToList());
		}

		public List<string[]> GroupPaths(List<string> paths)
		{
			var filePathGroups = new List<string[]>();
			var tmpGroup = new List<string>();
			var currLength = 0;
			var currNumFiles = 0;
			foreach ( var path in paths)
			{
				if(currLength > 13600 || currNumFiles >= groupSize)
				{
					filePathGroups.Add(tmpGroup.ToArray());
					tmpGroup.Clear();
					currLength = 0;
					currNumFiles = 0;
				}
				tmpGroup.Add(path);
				currLength += path.Length;
				currNumFiles++;
			}
			if (tmpGroup.Count > 0)
			{
                filePathGroups.Add(tmpGroup.ToArray());
            }
			return filePathGroups;
		}

		public List<FileInfo2> IdentifyCompressedFilesJSON(string input)
		{
			Logger logger = Logger.Instance;
			UnpackCompressedFolders();
			var fileBag = new ConcurrentBag<FileInfo2>();

			//For eaccompressed folder, identify all files
			Parallel.ForEach(CompressedFolders, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, folders =>
			{
				foreach (string folder in folders)
				{
					//Identify all file paths in compressed folder and group them
					var pathWithoutExt = folder.LastIndexOf('.') > 0 ? folder.Substring(0, folder.LastIndexOf('.')) : folder;
					var paths = Directory.GetFiles(pathWithoutExt, "*.*", SearchOption.TopDirectoryOnly);
					var filePathGroups = GroupPaths(new List<string>(paths));
					//Identify all files in each group
					Parallel.ForEach(filePathGroups, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, paths =>
					{
						var files = IdentifyList(paths);
						if (files != null)
						{
							foreach (FileInfo2 file in files)
							{
								fileBag.Add(file);
							}
						}
						else
						{
							logger.SetUpRunTimeLogMessage("SF IdentifyCompressedFilesJSON: " + folder + " could not be identified", true);
						}
					});
				}
			});
			return fileBag.ToList();
		}

		public SiegfriedJSON? ParseJSONOutput(string json, bool readFromFile)
		{
			try
			{
				SiegfriedJSON siegfriedJson;
				FileStream? file = null;
				if (readFromFile)
				{
					file = File.OpenRead(json);
				}

				if (readFromFile && file == null)
				{
					Logger.Instance.SetUpRunTimeLogMessage("SF ParseJSON: file not found", true);
					return null;
				}

				using (JsonDocument document = readFromFile ? JsonDocument.Parse(file!) : JsonDocument.Parse(json))
				{
					// Access the root of the JSON document
					JsonElement root = document.RootElement;

					// Deserialize JSON into a SiegfriedJSON object
					siegfriedJson = new SiegfriedJSON
					{
						siegfriedVersion = root.GetProperty("siegfried").GetString() ?? "",
						scandate = root.GetProperty("scandate").GetString() ?? "",
						files = root.GetProperty("files").EnumerateArray()
							.Select(fileElement => ParseSiegfriedFile(fileElement))
							.ToArray()
					};
				}
				if (readFromFile && file != null)
				{
					file.Close();
				}
				return siegfriedJson;
			}
			catch (Exception e)
			{
				Console.WriteLine("Siegfried had an error parsing JSON data, this is most likely because a fatal error happened in Siegfried: " + e.Message);
				Logger.Instance.SetUpRunTimeLogMessage("SF ParseJSON: " + e.Message, true);
				return null;
			}
		}

		public static SiegfriedFile ParseSiegfriedFile(JsonElement fileElement)
		{
			string hashMethod = HashEnumToString(GlobalVariables.checksumHash);
			JsonElement jsonElement;
			return new SiegfriedFile
			{
				filename = fileElement.GetProperty("filename").GetString() ?? "",

				hash = fileElement.TryGetProperty(hashMethod, out jsonElement) ? fileElement.GetProperty(hashMethod).GetString() ?? "" : "",
				filesize = fileElement.GetProperty("filesize").GetInt64(),
				modified = fileElement.GetProperty("modified").GetString() ?? "",
				errors = fileElement.GetProperty("errors").GetString() ?? "",
				matches = fileElement.GetProperty("matches").EnumerateArray()
					.Select(matchElement => ParseSiegfriedMatches(matchElement))
					.ToArray()
			};
		}

		public static SiegfriedMatches ParseSiegfriedMatches(JsonElement matchElement)
		{
			return new SiegfriedMatches
			{
				ns = matchElement.GetProperty("ns").GetString() ?? "",
				id = matchElement.GetProperty("id").GetString() ?? "",
				format = matchElement.GetProperty("format").GetString() ?? "",
				version = matchElement.GetProperty("version").GetString() ?? "",
				mime = matchElement.GetProperty("mime").GetString() ?? "",
				class_ = matchElement.GetProperty("class").GetString() ?? "",
				basis = matchElement.GetProperty("basis").GetString() ?? "",
				warning = matchElement.GetProperty("warning").GetString() ?? ""
			};
		}

		/// <summary>
		/// Copies all files (while retaining file structure) from a source directory to a destination directory
		/// </summary>
		/// <param name="source">source directory</param>
		/// <param name="destination">destination directory</param>
		public void CopyFiles(string source, string destination)
		{
			string[] files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
			List<string> retryFiles = new List<string>();
			Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, file =>
			// (string file in files)
			{
				string relativePath = file.Replace(source, "");
				string outputPath = destination + relativePath;
				string outputFolder = outputPath.Substring(0, outputPath.LastIndexOf(Path.DirectorySeparatorChar));
				//TODO: THIS BEHAVIOUR SHOULD BE DOCUMENTED
				//If file already exists in target destination, skip it
				if (File.Exists(outputPath))
				{
					return;
				}
				if (!Directory.Exists(outputFolder))
				{
					Directory.CreateDirectory(outputFolder);
				}
				try
				{
					File.Copy(file, outputPath, true);
				}
				catch (IOException)
				{
					Console.WriteLine("Could not open file '{0}', it may be used in another process");
					retryFiles.Add(file);
				}
			});
			if (retryFiles.Count > 0)
			{
				Console.WriteLine("Some files could not be copied, close the processes using them and hit enter");
				Console.ReadLine();
				Parallel.ForEach(retryFiles, new ParallelOptions { MaxDegreeOfParallelism = GlobalVariables.maxThreads }, file =>
				{
					string relativePath = file.Replace(source, "");
					string outputPath = destination + relativePath;
					string outputFolder = outputPath.Substring(0, outputPath.LastIndexOf('\\'));

					if (!Directory.Exists(outputFolder))
					{
						Directory.CreateDirectory(outputFolder);
					}
					try
					{
						File.Copy(file, outputPath, true);
					}
					catch (Exception e)
					{
						Logger.Instance.SetUpRunTimeLogMessage("SF CopyFiles: " + e.Message, true);
					}
				});
			}
		}

		/// <summary>
		/// Compresses all folders in output directory
		/// </summary>
		public void CompressFolders()
		{
			//Identify original compression formats and compress the folders
			CompressedFolders.ForEach(folders =>
			{
				//Compress in reverse order to avoid compressing a folder that contains an uncompressed folder that should be compressed
				folders.Reverse();
				foreach (string folder in folders)
				{
					var extention = Path.GetExtension(folder);
					//Switch for different compression formats
					switch (extention)
					{
						case ".zip":
							CompressFolder(folder, ArchiveType.Zip);
							break;
						case ".tar":
							CompressFolder(folder, ArchiveType.Tar);
							break;
						case ".gz":
							CompressFolder(folder, ArchiveType.GZip);
							break;
						case ".rar":
							CompressFolder(folder, ArchiveType.Rar);
							break;
						case ".7z":
							CompressFolder(folder, ArchiveType.SevenZip);
							break;
						default:
							//Do nothing
							break;
					}
				}
			});
		}

		/// <summary>
		/// Unpacks all compressed folders in output directory
		/// </summary>
		public void UnpackCompressedFolders()
		{
			//Identify all files in output directory
			var compressedFoldersOutput = GetCompressedFolders(GlobalVariables.parsedOptions.Output);
			var compressedFoldersInput = GetCompressedFolders(GlobalVariables.parsedOptions.Input);

			// Remove root path from all paths in compressedFoldersInput
			var inputWithoutRoot = compressedFoldersInput.Select(file =>
			{
				int index = file.IndexOf(GlobalVariables.parsedOptions.Input);
				return index >= 0 ? file.Substring(0, index) + file.Substring(index + GlobalVariables.parsedOptions.Input.Length) : file;
			}).ToList();

			// Remove root path from all paths in compressedFoldersOutput
			var outputWithoutRoot = compressedFoldersOutput.Select(file =>
			{
				int index = file.IndexOf(GlobalVariables.parsedOptions.Output);
				return index >= 0 ? file.Substring(0, index) + file.Substring(index + GlobalVariables.parsedOptions.Output.Length) : file;
			}).ToList();

			//Remove all folders that are not in input directory
			foreach (string folder in outputWithoutRoot)
			{
				if (!inputWithoutRoot.Contains(folder))
				{
					compressedFoldersOutput.Remove(GlobalVariables.parsedOptions.Output + folder);
				}
			}

			ConcurrentBag<List<string>> allUnpackedFolders = new ConcurrentBag<List<string>>();
			//In Parallel: Unpack compressed folders recursively and delete the compressed folder
			compressedFoldersOutput.ForEach(root =>
			{
				allUnpackedFolders.Add(UnpackRecursively(root));
			});

			foreach (List<string> folder in allUnpackedFolders)
			{
				CompressedFolders.Add(folder);
			}
		}

		private List<string> UnpackRecursively(string root)
		{
			List<string> unpackedFolders = new List<string>();

			UnpackFolder(root);
			unpackedFolders.Add(root);

			var extractedFolder = root.LastIndexOf('.') > 0 ? root.Substring(0, root.LastIndexOf('.')) : root;
			var subFolders = GetCompressedFolders(extractedFolder);
			foreach (string folder in subFolders)
			{
				var path = Path.Combine(extractedFolder, Path.GetFileName(folder));
				unpackedFolders.AddRange(UnpackRecursively(path));
			}
			return unpackedFolders;
		}

		private List<string> GetCompressedFolders(string dir)
		{
			if (!Directory.Exists(dir))
			{
				return new List<string>();
			}
			var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).ToList();
			var compressedFolders = files.FindAll(file => file.EndsWith(".zip") || file.EndsWith(".tar") || file.EndsWith(".gz") || file.EndsWith(".rar") || file.EndsWith(".7z"));
			return compressedFolders;
		}

		/// <summary>
		/// Compresses a folder to a specified format and deletes the unpacked folder
		/// </summary>
		/// <param name="archiveType">Format for compression</param>
		/// <param name="path">Path to folder to be compressed</param>
		private void CompressFolder(string path, ArchiveType archiveType)
		{
			try
			{
				string fileExtension = Path.GetExtension(path);
				string pathWithoutExtension = path.Replace(fileExtension, ""); //TODO: This might not work for paths with multiple file extensions
				using (var archive = ArchiveFactory.Create(archiveType))
				{
					archive.AddAllFromDirectory(pathWithoutExtension);
					archive.SaveTo(path, CompressionType.None);
				}
				// Delete the unpacked folder
				Directory.Delete(pathWithoutExtension, true);
			}
			catch (Exception e)
			{
				Logger.Instance.SetUpRunTimeLogMessage("SF CompressFolder " + e.Message, true);
			}
		}

		/// <summary>
		/// Unpacks a compressed folder regardless of format
		/// </summary>
		/// <param name="path">Path to compressed folder</param>
		private void UnpackFolder(string path)
		{
			try
			{
				// Get path to folder without extention
				string pathWithoutExtension = path.LastIndexOf('.') > 0 ? path.Substring(0, path.LastIndexOf('.')) : path;
				// Ensure the extraction directory exists
				if (!Directory.Exists(pathWithoutExtension))
				{
					Directory.CreateDirectory(pathWithoutExtension);
				}
				try
				{
					// Extract the contents of the compressed file
					using (var archive = ArchiveFactory.Open(path))
					{
						foreach (var entry in archive.Entries)
						{
							if (!entry.IsDirectory)
							{
								entry.WriteToDirectory(pathWithoutExtension, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
							}
						}
					}
					File.Delete(path);
				}
				catch (CryptographicException)
				{
					Logger.Instance.SetUpRunTimeLogMessage("SF UnpackFolder " + path + " is encrypted", true);
				}
				catch (Exception e)
				{
					Logger.Instance.SetUpRunTimeLogMessage("SF UnpackFolder " + e.Message, true);
				}//TODO: Delete the compressed folder
			}
			catch (Exception e)
			{
				Logger.Instance.SetUpRunTimeLogMessage("SF UnpackFolder " + e.Message, true);
			}
		}
	}
}
