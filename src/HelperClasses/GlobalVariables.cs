﻿using FileConverter;

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
        public static Options parsedOptions = new Options();
        //Map with all specified conversion formats, to and from
        public static Dictionary<string, string> FileConversionSettings = new Dictionary<string, string>(); // the key is pronom code 
                                                                                                  // Map with info about what folders have overrides for specific formats
        public static Dictionary<string, ConversionSettingsData> FolderOverride = new Dictionary<string, ConversionSettingsData>(); // the key is a foldername
        public static HashAlgorithms checksumHash = HashAlgorithms.SHA256;
        public static int maxThreads = Environment.ProcessorCount * 2;
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
