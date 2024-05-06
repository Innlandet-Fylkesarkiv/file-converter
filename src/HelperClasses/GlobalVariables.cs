using FileConverter;

namespace FileConverter.HelperClasses
{
    public enum PrintSortBy
    {
        Count,
        TargetPronom,
        CurrentPronom,
    }
    public static class GlobalVariables
    {
        private static Options parsedOptions = new Options();

        public static Options ParsedOptions
        {
            get { return parsedOptions; }
            set { parsedOptions = value; }
        }
        //Map with all specified conversion formats, to and from
        private static Dictionary<string, string> fileConversionSettings = new Dictionary<string, string>(); // the key is pronom code 
        public static Dictionary<string, string> FileConversionSettings   // Map with info about what folders have overrides for specific formats
        {
            get { return fileConversionSettings; }
            set { fileConversionSettings = value; }
        } 

        public static Dictionary<string, ConversionSettingsData> folderOverride = new Dictionary<string, ConversionSettingsData>(); // the key is a foldername
        public static Dictionary<string, ConversionSettingsData> FolderOverride
        {
            get { return folderOverride; }
            set {  folderOverride = value; }
        }

        private static HashAlgorithms checksumHash = HashAlgorithms.SHA256;
        public static HashAlgorithms ChecksumHash
        {
            get { return checksumHash; }
            set { checksumHash = value; }
        }

        private static int maxThreads = Environment.ProcessorCount * 2;
        public static int MaxThreads
        {
            get { return maxThreads; }
            set { maxThreads = value; }
        }

        public static int timeout = 20;
        public static double maxFileSize = 1 * 1024 * 1024 * 1024;      //1GB
        public const int MAX_RETRIES = 3; //Maximum number of attempts in case of a failed conversion
        public const ConsoleColor INFO_COL = ConsoleColor.Cyan;
        public const ConsoleColor ERROR_COL = ConsoleColor.Red;
        public const ConsoleColor WARNING_COL = ConsoleColor.Yellow;
        public const ConsoleColor SUCCESS_COL = ConsoleColor.Green;
        public static readonly PrintSortBy SortBy = PrintSortBy.Count;
        public static bool debug = false;

        public static void Reset()
        {
            FileConversionSettings.Clear();
            FolderOverride.Clear();
            //Set to default values (will be overwritten in ConversionSettings.cs if specified by user)
            checksumHash = HashAlgorithms.SHA256;
            maxThreads = Environment.ProcessorCount * 2;
            timeout = 5;
        }
    }
}
