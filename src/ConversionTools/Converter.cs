using FileConverter.Managers;
using FileConverter.HelperClasses;
using SF = FileConverter.Siegfried;
/// <summary>
/// Parent class for all converters
/// </summary>
/// 
namespace ConversionTools
{
    public class Converter
    {
        public string Name { get; set; } = ""; // Name of the converter
        public string Version { get; set; } = ""; // Version of the converter
        public string NameAndVersion { get; set; } = ""; // Name and version of the converter
        public Dictionary<string, List<string>> SupportedConversions { get; set; } = new Dictionary<string, List<string>>(); // Supported conversions for the converter
        public List<string> SupportedOperatingSystems { get; set; } = new List<string>(); // Supported operating systems for the converter
        public bool DependenciesExists { get; set; } = false;    // Whether the required dependencies for the converter are available on the system
        public Dictionary<string, List<string>> BlockingConversions { get; set; } = new Dictionary<string, List<string>>();  // Conversions that are blocking (can't be multithreaded)
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);      // Semaphore to handle locked sections

        /// <summary>
        /// Converter constructor
        /// </summary>
        public Converter()
        { }

        /// <summary>
        /// Get the supported operating systems for the converter
        /// </summary>
        /// <returns> A list of supported OS </returns>
        public virtual List<string> GetSupportedOS()
        {
            return new List<string>();
        }

        /// <summary>
        /// Get a list of supported conversions for the converter
        /// </summary>
        /// <returns> A dictionary with string originalPRONOM and a list of PRONOMS it can be converted to </returns>
        public virtual Dictionary<string, List<string>>? GetListOfSupportedConvesions()
        {
            return new Dictionary<string, List<string>>();
        }
        public virtual Dictionary<string, List<string>> GetListOfBlockingConversions()
        {
            return new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Set the name and version of the converter
        /// </summary>
        public virtual void SetNameAndVersion()
        {
            GetVersion();
            NameAndVersion = Name + " " + Version;
        }

        /// <summary>
        /// Get the version of the converter
        /// </summary>
        public virtual void GetVersion() { }

        /// <summary>
        /// Wrapper for the ConvertFile method that also handles the timeout
        /// </summary>
        /// <param name="file">File that should be converted</param>
        async virtual public Task ConvertFile(FileToConvert file)
        {            
            var timeout = TimeSpan.FromMinutes(GlobalVariables.timeout);
            try
            {
                if (ConversionContainsLock(file))
                {
                    semaphore.Wait();
                }

                await ConvertFileWithTimeout(file, timeout);
            }
            catch (TimeoutException)
            {
                Logger.Instance.SetUpRunTimeLogMessage("ConvertFile: Conversion timed out", true, filename: file.FilePath);
            }
            finally
            {
                if (ConversionContainsLock(file))
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Convert a file to a new format
        /// </summary>
        /// <param name="fileinfo">The file to be converted</param>
        /// <param name="pronom">The file format to convert to</param>
        async public virtual Task ConvertFile(FileToConvert file, string pronom)
        { }

        /// <summary>
        /// Checks if the converter supports the conversion of a file from one format to another
        /// </summary>
        /// <param name="originalPronom">The pronom code of the current file format</param>
        /// <param name="targetPronom">The pronom code of the target file format</param>
        /// <returns>True if the converter supports it, otherwise False</returns>
        public bool SupportsConversion(string originalPronom, string targetPronom)
        {
            if (SupportedConversions != null && SupportedConversions.TryGetValue(originalPronom, out var targetList))
            {
                return targetList.Contains(targetPronom);
            }
            return false;
        }

        /// <summary>
        /// Method to call another method with a timeout
        /// </summary>
        /// <param name="file">File to convert</param>
        /// <param name="timeout">Time before timeout</param>
        private async Task ConvertFileWithTimeout(FileToConvert file, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => ConvertFile(file, file.Route.First()), cancellationTokenSource.Token);

            // Wait for either the task to complete or the timeout to elapse
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout));

            if (completedTask == task)
            {
                // Method completed within the timeout duration
                cancellationTokenSource.Cancel();
            }
            else
            {
                cancellationTokenSource.Cancel();
                // Timeout occurred
                throw new TimeoutException("Method execution timed out.");
            }
        }

        /// <summary>
        /// Check if a conversion leads to a locked section
        /// </summary>
        /// <param name="file">Conversion that should be checked</param>
        /// <returns>True if it contains a locked section</returns>
        bool ConversionContainsLock(FileToConvert file)
        {
            if (BlockingConversions.TryGetValue(file.CurrentPronom, out var conversionLocks))
            {
                return conversionLocks.Contains(file.Route.First());
            }
            return false;
        }

        /// <summary>
        /// Combine multiple files into one file
        /// </summary>
        /// <param name="files">List of files that should be combined</param>
        public virtual void CombineFiles(List<FileInfo2> files, string pronom)
        { }

        /// <summary>
        /// Delete an original file, that has been converted, from the output directory
        /// </summary>
        /// <param name="filePath">The specific file to be deleted</param>
        static public void DeleteOriginalFileFromOutputDirectory(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("deleteOriginalFileFromOutputDirectory: " + e.Message, true, filename: filePath);
            }
        }

        /// <summary>
        /// Replaces a file in the list of files to convert
        /// </summary>
        /// <param name="newPath"> The new path of the file </param>
        /// <param name="f"> The specific file </param>
        static public void ReplaceFileInList(string newPath, FileToConvert f)
        {
            f.FilePath = newPath;
            var file = FileManager.Instance.GetFile(f.Id);
            if (file != null)
            {
                file.FilePath = newPath;
                file.OriginalFilePath = Path.GetFileName(newPath);
            }
            else
            {
                Logger.Instance.SetUpRunTimeLogMessage("replaceFileInList: File not found in FileManager", true, filename: f.FilePath);
            }
        }

        /// <summary>
        /// Check if a file has been converted and update the file list
        /// </summary>
        /// <param name="file">File that has been converted</param>
        /// <param name="newFilepath">Filepath to new file</param>
        /// <param name="newFormat">Target pronom code</param>
        /// <returns>True if the conversion succeeded, otherwise false</returns>
        static public bool CheckConversionStatus(string newFilepath, string newFormat, FileToConvert file)
        {
            try
            {
                var result = SF.Siegfried.Instance.IdentifyFile(newFilepath, false);
                if (result != null && result.matches[0].id == newFormat)
                {
                    DeleteOriginalFileFromOutputDirectory(file.FilePath);
                    ReplaceFileInList(newFilepath, file);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("CheckConversionStatus: " + e.Message, true);
            }
            return false;
        }

        /// <summary>
        /// Check the status of the conversion
        /// </summary>
        /// <param name="filePath"> Full path of the file </param>
        /// <param name="pronom"> The specific PRONOM code of the file </param>
        /// <returns> True or false depending on if conversion is done </returns>
        public bool CheckConversionStatus(string filePath, string pronom)
        {
            try
            {
                var result = SF.Siegfried.Instance.IdentifyFile(filePath, false);
                return result != null && result.matches[0].id == pronom;
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("CheckConversionStatus: " + e.Message, true);
            }
            return false;
        }

        /// <summary>
        /// Get the pronom code of a file
        /// </summary>
        /// <param name="filepath"> Full path of the file </param>
        /// <returns> String containing PRONOM code or null </returns>
        static public string? GetPronom(string filepath)
        {
            try
            {
                var result = SF.Siegfried.Instance.IdentifyFile(filepath, false);
                if (result != null && result.matches.Length > 0)
                {
                    return result.matches[0].id;
                }
            }
            catch (Exception e)
            {
                Logger.Instance.SetUpRunTimeLogMessage("GetPronom: " + e.Message, true);
            }
            return null;
        }

        /// <summary>
        /// Get the correct place where the process should be run depending on operating system
        /// </summary>
        /// <returns> String - Name of where the process should be run </returns>
        public static string GetPlatformExecutionFile()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix ? "bash" : "cmd.exe";
        }

        /// <summary>
        /// Checks if the folder with the soffice.exe executable exists in the PATH.
        /// </summary>
        /// <param name="executableName"> Name of the executable to have its folder in the PATH </param>
        /// <returns> Bool indicating if the directory containing the executable was found </returns>
        public static bool CheckPathVariableWindows(string executableName)
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? ""; // Get the environment variables as a string
            string[] paths = pathVariable.Split(Path.PathSeparator);          // Split them into individual entries

            foreach (string path in paths)                                    // Go through and check if found  
            {
                string fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Same function as for windows, but with small changes to facilitate for linux users
        /// </summary>
        /// <param name="executableName"> Name of the executable to have its folder in the PATH </param>
        /// <returns> Bool indicating if the directory containing the executable was found </returns>
        public static bool CheckPathVariableLinux(string executableName)
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? "";
            char pathSeparator = Path.PathSeparator;

            // Use : as the separator on Linux
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                pathSeparator = ':';
            }

            string[] paths = pathVariable.Split(pathSeparator);

            foreach (string path in paths)
            {
                string fullPath = Path.Combine(path, executableName);

                if (File.Exists(fullPath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}