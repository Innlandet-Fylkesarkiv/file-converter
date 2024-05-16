using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using Org.BouncyCastle.Asn1.Ocsp;
using iText.StyledXmlParser.Css.Selector.Item;

namespace FileConverter.HelperClasses
{
    public class Logger
	{
		private static Logger? instance;
		private static readonly object LockObject = new object();
		readonly string LogPath;         // Path to log file

		List<JsonData> JsonFiles = new List<JsonData>();
		readonly Dictionary<string, Dictionary<string, List<JsonDataMerge>>> JsonMergedFiles = new Dictionary<string, Dictionary<string, List<JsonDataMerge>>>();
		readonly List<JsonDataOutputNotSupported> JsonNotSupportedFiles = new List<JsonDataOutputNotSupported>();
		readonly List<JsonDataOutputNotSet> JsonOutputNotSetFiles = new List<JsonDataOutputNotSet>();

		public bool ErrorHappened { get; set; }             // True if an error message has been written
															// Configure JSON serializer options for pretty-printing
		readonly JsonSerializerOptions Options = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
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
			public string? OriginalFilePath { get; set; }
			public string? NewFilePath { get; set; }
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

			// Create directory for log files if not present
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			DateTime currentDateTime = DateTime.Now;
			string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HHmmss");
			// Set path to logfile with current date and time
			path += "/";
			LogPath = path + "log " + formattedDateTime + ".txt";
			// Write the specified text asynchronously to a new file.
			using (StreamWriter outputFile = new StreamWriter(LogPath))
			{
				outputFile.WriteAsync("Type: | (Error) Message | Pronom Code | Mime Type | Filename\n");
			}
		}

		/// <summary>
		/// Makes sure that only one instance of the logger is created using the singleton pattern
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
		static private void WriteLog(string message, string filepath)
		{
			// Make sure not two threads try to write to the logfile concurrently
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
			ErrorHappened =  error;
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
            string docPath = GlobalVariables.ParsedOptions.Output + Path.DirectorySeparatorChar + "documentation.json";
			foreach (FileInfo2 file in files)
			{
				if (file.ShouldMerge)
				{
					AddToMergeJSON(file);
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
						OriginalFilePath = file.OriginalFilePath,
						NewFilePath = file.FilePath,
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
			WriteLog(json, docPath);
		}

		/// <summary>
		/// Adds a file to the JSON structure that holds information about merged files
		/// </summary>
		/// <param name="file">The file that should be</param>
		private void AddToMergeJSON(FileInfo2 file)
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

            if (JsonMergedFiles.TryGetValue(parentDir, out var mergedFiles))
            {
                if (mergedFiles.TryGetValue(jsonData.MergedTo, out var dataList))
                {
                    dataList.Add(jsonData);
                }
				else if (mergedFiles.TryGetValue(jsonData.Filename, out var dataList2))
				{
                    //Add the result of the merge to the beginning of the list
                    dataList2.Insert(0,jsonData);
				}
				else if (jsonData.MergedTo == "")
				{
					//If the file is result of a merge, but the entry is not set, create the entry
					mergedFiles.Add(jsonData.Filename, new List<JsonDataMerge>());
					mergedFiles[jsonData.Filename].Add(jsonData);
				}
                else
                {
                    mergedFiles.Add(jsonData.MergedTo, new List<JsonDataMerge>());
                    mergedFiles[jsonData.MergedTo].Add(jsonData);
                }
            }
            else
            {
                JsonMergedFiles.Add(parentDir, new Dictionary<string, List<JsonDataMerge>>());
                JsonMergedFiles[parentDir].Add(jsonData.MergedTo, new List<JsonDataMerge>());
                JsonMergedFiles[parentDir][jsonData.MergedTo].Add(jsonData);
            }
        }
		/// <summary>
		/// Sets the requester and converter if they are not set in the ConversionSettings
		/// </summary>
		public static void SetRequesterAndConverter()
		{
            if (JsonRoot.Requester == null || JsonRoot.Requester == "")
			{
				AskRequesterOrConverter(true);
			}
			if (JsonRoot.Converter == null || JsonRoot.Converter == "")
			{
                AskRequesterOrConverter(false);
            }
		}

		/// <summary>
		/// Asks the user about the requester or converter 
		/// </summary>
		/// <param name="Requester"> if it is about requester or converter </param>
		private static void AskRequesterOrConverter(bool Requester)
		{
			string person = Requester ? "requester" : "converter";
			string requesterOrConverter;
            string user = Environment.UserName;

            if (OperatingSystem.IsWindows())
            {
                user = UserPrincipal.Current.DisplayName;
            }
            if (!GlobalVariables.ParsedOptions.AcceptAll)
            {
				// Ask the user for the requester or converter
                Console.WriteLine("No data found in ConversionSettings and username '{0}' was detected, do you want to set it as '{1}' in the documentation? (Y/N)", user, person);
                var response = GlobalVariables.ParsedOptions.AcceptAll ? "Y" : Console.ReadLine()!;
                if (response.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    requesterOrConverter = user;
                }
                else
                {
                    Console.WriteLine("Who is requesting the converting?");
                    requesterOrConverter = Console.ReadLine()!;
                }
            }
            else
            {
                requesterOrConverter = user;
            }

			if (Requester)
			{
                JsonRoot.Requester = requesterOrConverter;
            }
            else
			{
                JsonRoot.Converter = requesterOrConverter;
            }
        }
	}
}
