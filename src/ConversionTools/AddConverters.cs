using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
class AddConverters
{
    List<Converter> converters = null;


    public List<Converter> GetConverters()
    {
        if (converters == null)
        {
            converters = new List<Converter>();
            converters.Add(new iText7());
            converters.Add(new GhostscriptConverter());
            converters.Add(new LibreOfficeConverter());
            converters.Add(new EmailConverter());
            //Remove converters that are not supported on the current operating system
            var currentOS = Environment.OSVersion.Platform.ToString();
            converters.RemoveAll(c => c.SupportedOperatingSystems == null ||
                                      !c.SupportedOperatingSystems.Contains(currentOS) ||
                                      !c.DependeciesExists);
        }
        
        return converters;
    }
    private static AddConverters? instance;
    private static readonly object lockObject = new object();
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


