namespace FileConverter.HelperClasses
{
    public enum PrintSortBy
    {
        Count,
        TargetPronom,
        CurrentPronom,
    }
    /// <summary>
    /// Class with all globally accessible variables in the program
    /// </summary>
    public static class GlobalVariables
    {
        public static Options ParsedOptions { get; set; } = new Options();

        //Map with all specified conversion formats, to and from
        public static Dictionary<string, string> FileConversionSettings { get; set; } = new Dictionary<string, string>(); // the key is pronom code 
                                                                                    // Map with info about what folders have overrides for specific formats

        public static Dictionary<string, ConversionSettingsData> FolderOverride { get; set; } 
                                                            = new Dictionary<string, ConversionSettingsData>(); // the key is a foldername

        public static HashAlgorithms ChecksumHash { get; set; } = HashAlgorithms.SHA256;

        public static int MaxThreads { get; set; } = Environment.ProcessorCount * 2;
        public static int Timeout { get; set; } = 20;
        public static double MaxFileSize { get; set; } = 1 * 1024 * 1024 * 1024;      //1GB
        public const int MAX_RETRIES = 3; //Maximum number of attempts in case of a failed conversion
        public const ConsoleColor INFO_COL = ConsoleColor.Cyan;
        public const ConsoleColor ERROR_COL = ConsoleColor.Red;
        public const ConsoleColor WARNING_COL = ConsoleColor.Yellow;
        public const ConsoleColor SUCCESS_COL = ConsoleColor.Green;
        public static readonly PrintSortBy SortBy = PrintSortBy.Count;
        public static bool Debug { get; set; } = false;

        /// <summary>
        /// Resets all global variables to default values
        /// </summary>
        public static void Reset()
        {
            FileConversionSettings.Clear();
            FolderOverride.Clear();
            // Set to default values (will be overwritten in ConversionSettings.cs if specified by user)
            ChecksumHash = HashAlgorithms.SHA256;
            MaxThreads = Environment.ProcessorCount * 2;
            Timeout = 5;
        }
    }
}