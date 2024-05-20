using ConversionTools.Converters;

namespace ConversionTools
{
    /// <summary>
    /// Class that is responsible for fetching the converters
    /// </summary>
    class AddConverters
    {
        List<Converter>? Converters = null;
        private static AddConverters? instance;
        private static readonly object lockObject = new object();

        /// <summary>
        /// Get all available converters
        /// </summary>
        /// <returns>a list of the converters </returns>
        public List<Converter> GetConverters()
        {
            if (Converters == null)
            {
                Converters = new List<Converter>();
                // Add the IText7 converter first, since it is used in the constructor of other converters
                Converters.Add(new IText7());
                Converters.AddRange(new List<Converter>
                {
                    new GhostscriptConverter(),
                    new LibreOfficeConverter(),
                    new EmailConverter()
                });

                //Remove converters that are not supported on the current operating system
                var currentOS = Environment.OSVersion.Platform.ToString();
                Converters.RemoveAll(c => c.SupportedOperatingSystems == null ||
                                          !c.SupportedOperatingSystems.Contains(currentOS) ||
                                          !c.DependenciesExists);
            }
        
            return Converters;
        }
    
        /// <summary>
        /// makes sure that only one instance of the class is created, following the singleton pattern
        /// </summary>
        public static AddConverters Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new AddConverters();
                        }
                    }
                }
                return instance;
            }
        }
    }
}