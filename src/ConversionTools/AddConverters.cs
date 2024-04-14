
class AddConverters
{
    List<Converter>? Converters = null;
    private static AddConverters? instance;
    private static readonly object lockObject = new object();

    public List<Converter> GetConverters()
    {
        if (Converters == null)
        {
            Converters = new List<Converter>();
            Converters.Add(new iText7());
            Converters.Add(new GhostscriptConverter());
            Converters.Add(new LibreOfficeConverter());
            Converters.Add(new EmailConverter());
            //Remove converters that are not supported on the current operating system
            var currentOS = Environment.OSVersion.Platform.ToString();
            Converters.RemoveAll(c => c.SupportedOperatingSystems == null ||
                                      !c.SupportedOperatingSystems.Contains(currentOS) ||
                                      !c.DependeciesExists);
        }
        
        return Converters;
    }
    
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


