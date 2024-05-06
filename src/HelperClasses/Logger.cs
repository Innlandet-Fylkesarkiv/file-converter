using System.Text.Json;
using System.DirectoryServices.AccountManagement;

namespace FileConverter.HelperClasses
{
    public class Logger
	{
		private static Logger? instance;
		private static readonly object LockObject = new object();
		string LogPath;         // Path to log file
		string DocPath;         // Path to documentation file

		List<JsonData> JsonFiles = new List<JsonData>();
		Dictionary<string, Dictionary<string, List<JsonDataMerge>>> JsonMergedFiles = new Dictionary<string, Dictionary<string, List<JsonDataMerge>>>();
		List<JsonDataOutputNotSupported> JsonNotSupportedFiles = new List<JsonDataOutputNotSupported>();
		List<JsonDataOutputNotSet> JsonOutputNotSetFiles = new List<JsonDataOutputNotSet>();

		public bool ErrorHappened { get; set; }             // True if an error message has been written
															// Configure JSON serializer options for pretty-printing
		JsonSerializerOptions Options = new JsonSerializerOptions
		{
			WriteIndented = true,
		};

		/// <summary>
		/// Metadata for the documentaion file
		/// </summary>
		public static class JsonRoot
		{
			public static string Requester { get; set; } = ""; // the person requesting the converting
			public static string Converter { get; set; } = ""; // the person that converts
			public static string? Hashing { get; set; } // the hashing algorithm used
		}

		/// <summary>
		/// Data for the documentation file when a file is supported and target PRONOM is set in ConversionSettings
		/// </summary>
		public class JsonData
		{
			public string? Filename { get; set; }
			public string? OriginalPronom { get; set; }
			public string? OriginalChecksum { get; set; }
			public long OriginalSize { get; set; }

			public string? TargetPronom { get; set; }
			public string? NewPronom { get; set; }
			public string? NewChecksum { get; set; }
			public long NewSize { get; set; }
			public List<string>? Converter { get; set; }
			public bool IsConverted { get; set; }
		}

		/// <summary>
		/// Class that holds the data for files that are not set in ConversionSettings
		/// </summary>
		public class JsonDataOutputNotSet
		{
			public string? Filename { get; set; }
			public string? OriginalPronom { get; set; }
			public string? OriginalChecksum { get; set; }
			public long OriginalSize { get; set; }
		}

		/// <summary>
		/// Class that holds the data for files that are not supported
		/// </summary>
		public class JsonDataOutputNotSupported
		{
			public string? Filename { get; set; }
			public string? OriginalPronom { get; set; }
			public string? OriginalChecksum { get; set; }
			public long OriginalSize { get; set; }
			public string? TargetPronom { get; set; }
		}

		/// <summary>
		/// Class that holds the data for files that are merged or a result from a merge
		/// </summary>
		public class JsonDataMerge
		{
			public string? Filename { get; set; }
			public string? Pronom { get; set; }
			public string? Checksum { get; set; }
			public long Size { get; set; }

			public List<string>? Tool { get; set; }
			public bool ShouldMerge { get; set; }
			public bool IsMerged { get; set; }
			public string? MergedTo { get; set; }
		}



		/// <summary>
		/// When logger is created, it sets the correct hashing algorithm and creates the log file
		/// </summary>
		private Logger()
		{
			string currentDirectory = Directory.GetCurrentDirectory();
			string path = Path.Combine(currentDirectory, "logs");
			switch (GlobalVariables.ChecksumHash)
			{
				case HashAlgorithms.SHA256:
					JsonRoot.Hashing = "SHA256";
					break;
				case HashAlgorithms.MD5:
					JsonRoot.Hashing = "MD5";
					break;
			}

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			DateTime currentDateTime = DateTime.Now;
			string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HHmmss");
			path += "/";
			LogPath = path + "log " + formattedDateTime + ".txt";
			// Write the specified text asynchronously to a new file.
			using (StreamWriter outputFile = new StreamWriter(LogPath))
			{
				outputFile.WriteAsync("Type: | (Error) Message | Pronom Code | Mime Type | Filename\n");
			}
		}

		/// <summary>
		/// Makes sure that only one instance of the logger is created
		/// </summary>
		public static Logger Instance
		{
			get
			{
				if (instance == null)
				{
					lock (LockObject)
					{
						if (instance == null)
						{
							instance = new Logger();
						}
					}
				}
				return instance;
			}
		}

		/// <summary>
		/// writes a log to a file
		/// </summary>
		/// <param name="message"> The message to be logged </param>
		/// <param name="filepath"> The filepath to the logfile </param>
		private void WriteLog(string message, string filepath)
		{
			lock (LockObject)
			{
				// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-write-text-to-a-file    

				// Write the specified text asynchronously to a new file.
				using (StreamWriter outputFile = new StreamWriter(filepath, true))
				{
					outputFile.Write(message);
					outputFile.Flush();
				}
			}
		}

		/// <summary>
		/// sets up status and error messages to the correct format.
		/// </summary>
		/// <param name="message"> the message to be sent </param>
		/// <param name="error"> true if it is an error </param>
		/// <param name="pronom"> Optional: the pronom of the file </param>
		/// <param name="mime"> Optional: mime of the file </param>
		/// <param name="filename"> Optional: the filename </param>
		/// <returns> returns the message in the correct format </returns>
		public void SetUpRunTimeLogMessage(string message, bool error, string pronom = "N/A", string mime = "N/A", string filename = "N/A")
		{
			ErrorHappened = ErrorHappened ? true : error;
			string errorM = "Message: ";
			if (error) { errorM = "Error: "; }
			string formattedMessage = errorM + " | " + message + " | " + pronom + " | " + mime + " | " + filename + "\n";
			WriteLog(formattedMessage, LogPath);
		}

		/// <summary>
		/// Sets up how the final documentation file should be printed
		/// </summary>
		/// <param name="files"> list containing fileinfo about all files </param>
		public void SetUpDocumentation(List<FileInfo2> files)
		{
			//TODO: Comment: Maybe find better place to put the file and set docPath earlier
			string path = GlobalVariables.ParsedOptions.Output + "/";
			DocPath = path + "documentation.json";
			using (StreamWriter outputFile = new StreamWriter(DocPath))
			{
				outputFile.WriteAsync("\n");
			}
			foreach (FileInfo2 file in files)
			{
				if (file.ShouldMerge)
				{
					var jsonData = new JsonDataMerge
					{
						Filename = file.FilePath,
						Pronom = file.OriginalPronom,
						Checksum = file.OriginalChecksum,
						Size = file.OriginalSize,
						Tool = file.ConversionTools,
						ShouldMerge = file.ShouldMerge,
						IsMerged = file.IsMerged,
						MergedTo = file.NewFileName
					};
					var parentDir = Path.GetDirectoryName(file.FilePath) ?? "";

					if (JsonMergedFiles.ContainsKey(parentDir))
					{
						if (JsonMergedFiles[parentDir].ContainsKey(jsonData.MergedTo))
						{
							JsonMergedFiles[parentDir][jsonData.MergedTo].Add(jsonData);
						}
						else
						{
							JsonMergedFiles[parentDir].Add(jsonData.MergedTo, new List<JsonDataMerge>());
							JsonMergedFiles[parentDir][jsonData.MergedTo].Add(jsonData);
						}
					}
					else
					{
						JsonMergedFiles.Add(parentDir, new Dictionary<string, List<JsonDataMerge>>());
						JsonMergedFiles[parentDir].Add(jsonData.MergedTo, new List<JsonDataMerge>());
						JsonMergedFiles[parentDir][jsonData.MergedTo].Add(jsonData);
					}
				}
				else if (file.NotSupported)
				{
					var jsonData = new JsonDataOutputNotSupported
					{
						Filename = file.FilePath,
						OriginalPronom = file.OriginalPronom,
						OriginalChecksum = file.OriginalChecksum,
						OriginalSize = file.OriginalSize,
						TargetPronom = ConversionSettings.GetTargetPronom(file)
					};
					JsonNotSupportedFiles.Add(jsonData);
				}
				else if (file.OutputNotSet)
				{
					var jsonData = new JsonDataOutputNotSet
					{
						Filename = file.FilePath,
						OriginalPronom = file.OriginalPronom,
						OriginalChecksum = file.OriginalChecksum,
						OriginalSize = file.OriginalSize
					};
					JsonOutputNotSetFiles.Add(jsonData);
				}
				else
				{
					var jsonData = new JsonData
					{
						Filename = file.FilePath,
						OriginalPronom = file.OriginalPronom,
						OriginalChecksum = file.OriginalChecksum,
						OriginalSize = file.OriginalSize,
						TargetPronom = ConversionSettings.GetTargetPronom(file),
						NewPronom = file.NewPronom,
						NewChecksum = file.NewChecksum,
						NewSize = file.NewSize,
						Converter = file.ConversionTools,
						IsConverted = file.IsConverted
					};
					JsonFiles.Add(jsonData);
				}
			}

			JsonFiles = JsonFiles.OrderByDescending(x => x.IsConverted).ToList();

			// Create an anonymous object with "requester" and "converter" properties
			var metadata = new
			{
				JsonRoot.Requester,
				JsonRoot.Converter,
				JsonRoot.Hashing
			};
			var FilesWrapper = new
			{
				ConvertedFiles = JsonFiles,
				NotSupported = JsonNotSupportedFiles,
				OutputNotSet = JsonOutputNotSetFiles,
				MergedFiles = JsonMergedFiles
			};

			// Create an anonymous object with a "Files" property
			var jsonDataWrapper = new
			{
				Metadata = metadata,
				Files = FilesWrapper
			};

			// Serialize the wrapper object
			string json = JsonSerializer.Serialize(jsonDataWrapper, Options);

			// Send it to writelog to print it out there
			WriteLog(json, DocPath);
		}

		/// <summary>
		/// Asks the user about the requester and converter if they are not set in the ConversionSettings
		/// </summary>
		public void AskAboutReqAndConv()
		{
			if (JsonRoot.Requester == null || JsonRoot.Requester == "")
			{
				string requester = Environment.UserName;
				if (OperatingSystem.IsWindows())
				{
					requester = UserPrincipal.Current.DisplayName;
				}
				if (!GlobalVariables.ParsedOptions.AcceptAll)
				{
					Console.WriteLine("No data found in ConversionSettings and username '{0}' was detected, do you want to set it as requester in the documentation? (Y/N)", requester);
					var response = GlobalVariables.ParsedOptions.AcceptAll ? "Y" : Console.ReadLine()!;
					if (response.ToUpper() == "Y")
					{
						JsonRoot.Requester = requester;
					}
					else
					{
						Console.WriteLine("Who is requesting the converting?");
						JsonRoot.Requester = Console.ReadLine()!;
					}
				}
				else
				{
					JsonRoot.Requester = requester;
				}
			}

			if (JsonRoot.Converter == null || JsonRoot.Converter == "")
			{
				string converter = Environment.UserName;
				if (OperatingSystem.IsWindows())
				{
					converter = UserPrincipal.Current.DisplayName;
				}
				if (!GlobalVariables.ParsedOptions.AcceptAll)
				{
					Console.WriteLine("No data found in ConversionSettings and username '{0}' was detected, do you want to set it as converter in the documentation? (Y/N)", converter);
					var response = Console.ReadLine()!;
					if (response.ToUpper() == "Y")
					{
						JsonRoot.Converter = converter;
					}
					else
					{
						Console.WriteLine("Who is requesting the converting?");
						JsonRoot.Converter = Console.ReadLine()!;
					}
				}
				else
				{
					JsonRoot.Converter = converter;
				}
			}
		}
	}
}
