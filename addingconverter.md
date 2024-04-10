# Adding a new converter guide
This guide explains in detail how to:

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; **1. [Add a new converter](#adding-a-new-converter)** <br>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; **2. [Add a new conversion path](#adding-a-new-conversion-path-(multistep-conversion))**
# Adding a new converter
Adding new external converters or conversion paths is described in detail in the [Adding a new converter]() guide.
All source code for external converters is based on the same parent ```Converter``` class, located in ```\ConversionTools\Converter.cs```.

**Converter class**
```csharp
	public string? Name { get; set; } // Name of the converter
	public string? Version { get; set; } // Version of the converter
	public string? NameAndVersion { get; set;}
	public bool DependenciesExists { get; set;}
	public Dictionary<string, List<string>>? SupportedConversions { get; set; }
	public List<string> SupportedOperatingSystems { get; set; } = new List<string>();

	public virtual void ConvertFile(string fileinfo, string pronom){ }
	public virtual void CombineFiles(string []files, string pronom){ }
```

All fields shown in the code block above **must** be included in the subclass for the new external converter to work properly. If you are adding a *library-based* converter we would suggest having a look at ```iText7.cs``` for examples on how to structure the subclass.
For external converters where you want to *parse arguments and use an executable in CLI* we would suggest looking at ```GhostScript.cs```.

> :memo: NOTE: If you are adding an **executable** file that you want to use it needs to be included in the ```.csproj``` file as such to be loaded properly at runtime:
>```xml
><ItemGroup>
>	<None Update="PathToExecutableFile">
>	   <CopyToOutputDirectory>Always</CopyToOutputDirectory>
>	</None>
></ItemGroup>
>```
>This will make the executable file available at the path ```file-converter\bin\Debug\net8.0\PathToExecutableFile```.

All subclasses of ```Converter``` follow the same commenting scheme for consistency and ease when maintaining/debugging the application. It should state that it is a subclass of the ```Converter```class and which conversions it supports. Other functionalities of the converter, such as combining images, can be added after.

**Commenting scheme**
```csharp
/// <summary>
/// iText7 is a subclass of the Converter class.                                                     <br></br>
///                                                                                                  <br></br>
/// iText7 supports the following conversions:                                                       <br></br>
/// - Image (jpg, png, gif, tiff, bmp) to PDF 1.0-2.0                                                <br></br>
/// - Image (jpg, png, gif, tiff, bmp) to PDF-A 1A-3B                                                <br></br>
/// - HTML to PDF 1.0-2.0                                                                            <br></br>
/// - PDF 1.0-2.0 to PDF-A 1A-3B                                                                     <br></br>                                                                          
///                                                                                                  <br></br>
/// iText7 can also combine the following file formats into one PDF (1.0-2.0) or PDF-A (1A-3B):      <br></br>
/// - Image (jpg, png, gif, tiff, bmp)                                                               <br></br>
///                                                                                                  <br></br>
/// </summary>
```

To add the converter to the list of converters, add the line ```converters.Add(new NameOfConverter());``` in the ```AddConverter``` class. Assuming that the source code written for the converter is correct, and the settings are set correctly, the application should now use the new converter for the conversions it supports. 
```csharp
    public List<Converter> GetConverters()
    {
        List<Converter> converters = new List<Converter>();
        converters.Add(new iText7());
        converters.Add(new GhostscriptConverter());
        converters.Add(new LibreOfficeConverter());
	/*Add a new converter here!*/
        var currentOS = Environment.OSVersion.Platform.ToString();
        converters.RemoveAll(c => c.SupportedOperatingSystems == null ||
                                  !c.SupportedOperatingSystems.Contains(currentOS));
        return converters;
    }
```
# Adding a new conversion path (Multistep conversion)
Multistep conversion means that one can combine the functionality of several converters to convert a file to a file type that would not have been possible if you were using only one of the converters. For example, LibreOffice can convert Word documents to PDF and iText7 can convert PDF documents to PDF-A. Multistep conversion means that the functionalities can be combined so that a Word document can be converted to a PDF-A document. 

To add a new multistep conversion you need to add a route in the ```initMap``` function in ```ConversionManager.cs``` following this convention:

```csharp
foreach (string pronom in ListOfPronoms)
{
	foreach (string otherpronom in ListOfPronoms)
	{
		if (pronom != otherpronom){
			ConversionMap.Add(new KeyValuePair<string, string>(pronom, otherpronom), [pronom, helppronom , otherpronom]); 
	}}
```
```pronom``` is the pronom you want to convert from, while ```otherpronom``` is the pronom you want to convert to. ```ConversionMap``` works as a route so any ```helppronom``` is a stepping stone in that route from ```pronom``` to ```otherpronom```. You can add as many stepping stones as you want but they have to be added in the correct order from left to right.
