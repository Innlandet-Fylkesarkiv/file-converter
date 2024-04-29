> [!NOTE]
>  **This README is currently being updated, information found here might not reflect the current application.**
<div align="center">
	
# file-converter (W.I.P.) 

<img src="https://github.com/larsmhaugland/file-converter/assets/117298604/e3965308-bfe4-4850-86c9-06dae7b493b3">


![Static Badge](https://img.shields.io/badge/.net-8.0-blue)
![dotnet-badge](https://github.com/larsmhaugland/file-converter/actions/workflows/dotnet.yml/badge.svg?event=push)
[![CodeFactor](https://www.codefactor.io/repository/github/larsmhaugland/file-converter/badge)](https://www.codefactor.io/repository/github/larsmhaugland/file-converter)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

A module-based .NET application that converts files and generates documentation for archiving.

This application provides a framework for different conversion libraries/software to work together. It aims to promote a comprehensive open-source solution for file conversion, as opposed to the many paid options, which allows for multi-step conversion between different external libraries. 
</div>

# 🗒️Table of Contents 
- [Background](#-background)
- [Install](#-install)
  - [Install from source](#install-from-source)
  - [Dependencies](#-dependencies)
  - [External libraries and software](#external-libraries-and-software)
  - [Installation for Windows](#-installation-for-windows)
  - [Installation for Linux](#-installation-for-linux)
  	-  [Installing Siegfried on Linux](#installing-siegfried-on-linux)
- [Usage](#-usage)
  - [Beta notes](#-beta)	 
  - [CLI](#cli)
  - [Arguments](#arguments)
  - [GUI](#gui)
  - [Settings](#settings)
  - [Currently supported file formats](#currently-supported-file-formats)
  - [Documentation and logging](#documentation-and-logging)
  - [Adding a new converter](#adding-a-new-converter)
  - [Adding a new conversion path (Multistep conversion)](#adding-a-new-conversion-path-multistep-conversion)
- [Further Development](#further-development)
- [Acknowledgments](#-acknowledgments)
- [Contributing](#-contributing)
- [Licensing](#-licensing)


# 📖 Background
This project is part of a collaboration with **[Innlandet County Archive](https://www.visarkiv.no/)** and is a Bachelor's thesis project for a [Bachelor's in Programming](https://www.ntnu.edu/studies/bprog) at the **[Norwegian University of Technology and Science (NTNU)](https://www.ntnu.edu/)**.

In Norway, the act of archiving is regulated by the Archives Act, which states that public bodies have a duty to register and preserve documents that are created as part of their activity [^1]. As society is becoming more digitized so is information, and the documents that were previously physical and stored physically are now digital and stored digitally. Innlandet County Archive is an inter-municipal archive cooperation, that registers and preserves documents from 48 municipalities. However, not all file types they receive are suitable for archiving as they run a risk of becoming obsolete. (For further reading see: [Obsolescence: File Formats and Software](https://dpworkshop.org/dpm-eng/oldmedia/obsolescence1.html)) Innlandet County Archive wished to streamline its conversion process into one application that could deal with a vast array of file formats. Furthermore, archiving is based on the principles of accessibility, accountability and integrity, which is why this application also provides documentation of all changes made to files.

Much like programmers and software developers, archivists believe in an open-source world. Therefore it would only be right for this program to be open source. 

[^1]: Kultur- og likestillingsdepartementet. *Lov om arkiv [arkivlova].* URL: https://lovdata.no/dokument/NL/lov/1992-12-04-126?q=arkivloven (visited on 17th Jan. 2024).

# ⏬ Install

## Install from source
To download the application source code run:
 ```sh
  git clone --recursive https://github.com/larsmhaugland/file-converter.git
```

Build it using ```mingw32 make``` (For further instructions see: [mingw Tutorial](https://nerdyelectronics.com/install-mingw-on-windows-for-make/)) from the command line using:
```sh
make build_win #Build for Windows
make build_linux #Build for Linux (not stable, use Windows build)
```

> [!WARNING]
> If you want to build using ```dotnet build``` or an IDE you need to build **both** ```file-converter-prog2900.csproj``` and ```GUI/ChangeConverterSettings/ChangeConverterSettings.csproj```.


> [!NOTE]
> Cloning **with** the Git submodules is required for the application to work.
>If you did not clone the repository recursively or do not see the git submodules in your local repository we would suggest:
> ```sh
>   git submodule init
>   git submodule update
>```

## 👪 Dependencies
|OS| Dependencies | Needed for? |
|---|---|---|
|Windows and Linux| **[dotnet version 8.0](https://dotnet.microsoft.com/en-us/download)** | Needed to build and run the program. |
| Windows and Linux| **[Java JDK](https://www.oracle.com/java/technologies/downloads/)** (Only JRE also works)| Dependency for using the e-mail converter. |
| Windows| **[LibreOffice](https://www.libreoffice.org/download/download-libreoffice/?type=win-x86_64&version=7.6.6&lang=nb)** ( download version 7.6.6)| Required for converting office documents (Word, PowerPoint, Excel and OpenOffice). |
| Linux| Libreoffice should be already present on Linux. This can be checked with ```Soffice --version```. Otherwise, download from the link above.||
| Linux | **[GhostScript.](https://ghostscript.com/docs/9.55.0/Install.htm)** Should be installed on most distros, which can be checked by running ```gs -version```. | Required for PostScript and PDF to image conversion. |
| Windows and Linux| **[wkhtmltopdf version 0.12.6](https://wkhtmltopdf.org/downloads.html)** | Needed for converting emails. |
| Linux | **[Siegfried](https://github.com/richardlehane/siegfried)** | To identify files and keep track of the conversion process. |
| Linux | **sudo apt-get install libemail-outlook-message-perl** | Needed to convert msg files on Linux |
> [!NOTE]
> If you are on Linux see [Installation for Linux](#-installation-for-linux) for more info on Siegfried installation.

<br>

### Further download instructions for LibreOffice 
#### :window: Windows
Libreoffice must be manually added to ```PATH``` on Windows for the program to convert office files. The deafult installation path to Libreoffice is ```"C:\Program Files\LibreOffice"```, but the entry needs to be ```"C:\Program Files\LibreOffice\program"```.

1. Open *Settings* -> *Home* -> *About* (scroll down on the left) -> *Advanced system Settings* (on the right) -> *Environment variables.*
> [!TIP]
> Alternatively use the ```Windows key + R``` on the keyboard, then type in ```"sysdm.cpl"``` and hit enter. Thereafter, press *Advanced* and then *Environment variables.*
2. Locate the ```PATH``` variable and highlight it. Press *Edit* -> *New* -> copy the path to the program folder -> press *Ok*. This adds it to the users environment variables.
   
<br><br>
**[wkhtmltopdf](https://wkhtmltopdf.org/downloads.html)** must also be manually added to ```PATH```. For windows, it can be done as described above, just swap ```"C:\Program Files\LibreOffice\program"``` with ```"C:\Program Files\wkhtmltopdf\bin"```. 

#### 🐧 Linux
For Linux the default installation directory is ```...``` <br>
To add it as an environment variable:
1. Open the file ```.bashrc``` using ```nano ~/.bashrc```.
2. Navigate to the bottom of the file with the arrow keys and add this line at the end ```export PATH="$PATH:DefaultPathHere"```. Remember to save the file and exit.
3.  To apply the changes immediately run the command ```source ~/.bashrc```. Alternatively, log in and out.
4.   To verify, run the command ```echo $PATH``` and the path added should be at the end of the output from the command.

## External libraries and software
### Libraries
- **[iText7](https://github.com/itext/itext-dotnet)** under the GNU Affero General Public License v3.0.
- **[BouncyCastle.NetCore](https://github.com/chrishaly/bc-csharp)** under the MIT License.
- **[iText7 Bouncycastle Adapter](https://www.nuget.org/packages/itext7.bouncy-castle-adapter/8.0.2)** under the GNU Affero General Public License v3.0.
- **[CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)** under the MIT License.
- **[SharpCompress](https://github.com/adamhathcock/sharpcompress)** under the MIT License.
- **[Avalonia](https://avaloniaui.net/)** under the MIT License.

### Software
- **[GhostScript](https://www.ghostscript.com/index.html)** under the GNU Affero General Public License v3.0.
- **[LibreOffice](https://www.libreoffice.org/)** under the Mozilla Public License 2.0.
- **[wkhtmltopdf](https://wkhtmltopdf.org/)** under the GNU Lesser General Public License v3.0.
- **[email-outlook-message-perl](https://github.com/mvz/email-outlook-message-perl)** under the GNU Affero General Public License v3.0.
- **[Rebex Mail Converter](https://www.rebex.net/mail-converter/)** under Freeware.
- **[email-to-pdf-converter](https://github.com/nickrussler/email-to-pdf-converter)** under the Apache License 2.0.
- **[Siegfried](https://www.itforarchivists.com/siegfried/)** under the Apache License 2.0.

## 🪟 Installation for Windows 
Download a pre-built binary from the [Releases](https://github.com/larsmhaugland/file-converter/releases) page and unzip to a location in your system. It is also possible to build your own binaries, (see [Install](#-install))

## 🐧 Installation for Linux 
Download a pre-built binary from the [Releases](https://github.com/larsmhaugland/file-converter/releases) page and unzip to a location in your system. It is also possible to build your own binaries, (see [Install](#-install))

The application has been tested on the following Linux images:
- Debian "bookworm" 12
- Ubuntu Jammy Jellyfish 22.04 LTS
- Fedora Workstation 39
- Arch (kernel: Linux 6.7.7-arch1-1)

Running it on other distributions or other versions should be possible *as long as* it supports [dotnet version 8.0](https://dotnet.microsoft.com/en-us/download).
> [!IMPORTANT]
> Although running ***our*** application on other distributions should be fine it may reduce the amount of supported external libraries and software.

### Installing Siegfried on Linux
If you are using a **Debian**, **Arch** or **Red Hat** based distro the application will guide you through Siegfried installation if it isn't already installed. 

<div align="center">
	<img width="750" alt="Screenshot of guided installation of Siegfried on Linux" src="https://github.com/larsmhaugland/file-converter/assets/117298604/b89dd844-73af-43d9-a056-b6cd417733a1">

*Screenshot of guided installation of Siegfried on Linux*

**Please see the dependencies needed for installation below:**
| Distro | Dependency |
|---|---|
| Ubuntu/Debian | curl |
| Arch Linux | curl <br> brew [^2] |
| Fedora/Red Hat | brew [^2] |

</div>

If you are **not** using one of these distros please see the **[Siegfried GitHub](https://github.com/richardlehane/siegfried)** for information on downloading Siegfried.

[^2]:*Homebrew on Linux* URL: https://docs.brew.sh/Homebrew-on-Linux (visited on 3rd Mar. 2024)

# 🚀 Usage 
![file-converter](https://github.com/larsmhaugland/file-converter/assets/117298604/2de5c420-968c-43e5-b355-8d89cf75012f)

<div align="center">
	<i> Main CLI application</i>
	<br>
</div>

## 🔨 Beta
Since the program is still in beta, there are some **limitations** or **bugs** in the software. This section will be updated throughout the development process as we fix or find problems.
The program is mostly tested in Windows, so Linux specific issues may not appear in list. 
- Parsing siegfried data from incomplete run
  	- The current version of the program cannot successfully recover siegfried data from an incomplete run 
- GUI
  	- Starting GUI from main program will crash the program on Linux
- Office conversion (Linux)
  	- Office conversion using LibreOffice does not work correctly
  
## CLI
To run in CLI navigate to the path of the executable in the terminal and run:
```
$ .\file-converter-prog2900.exe
```
Alternatively, one can run the program using ```dotnet run```

## Arguments
> [!NOTE]
> All paths must be *absolute* or *relative* to executable.

### Set custom input folder 
Default: *input*
```
$ .\example -i "C:\Users\user\Downloads
$ .\example --input "C:\Users\user\Downloads
```

### Set custom output folder
Default: *output*
```
$ .\example -o "C:\Users\user\Downloads
$ .\example --output "C:\Users\user\Downloads
```

### Set custom settings file 
<br>Default: *Settings.xml*
```
$ .\example -s "C:\Users\user\custom_Settings.xml"
$ .\example --settings "C:\Users\user\custom_Settings.xml"
```

### Accept all queries in CLI
```
$ .\example -y
$ .\example --yes 
```

## GUI
![gui](https://github.com/larsmhaugland/file-converter/assets/117298604/0fd96cea-d84c-4b83-91b6-737c74ba8baf)
<div align="center">
	<i>GUI-version of settings</i>
	<br>
</div>

The GUI provides a more user-friendly way of editing the settings of the application (see [Settings](#settings) for further information). Here one can set all the metadata for running the program and what PRONOM files should be converted to. A formats ```Default``` PRONOM is a list of all the PRONOM's belonging to that file format (i.e all PRONOM's associated with the PDF file format).

## Settings
Settings can be manually set in an ```xml``` file.

### Setting run time arguments
```xml  
    <Requester></Requester>                    <!-- Name of person requesting the conversion -->
    <Converter></Converter>                    <!-- Name of person doing the conversion -->
	<ChecksumHashing></ChecksumHashing>    <!-- SHA256 (standard) or MD5 -->
	<InputFolder></InputFolder>            <!-- Specify input folder, default is "input" -->
	<OutputFolder></OutputFolder>          <!-- Specify output folder, default is "output" -->
	<MaxThreads></MaxThreads>	       <!-- Write a number, deafult is cores*2 -->
	<Timeout></Timeout>       	       <!-- Timeout in minutes, default is 30min -->
	<MaxFileSize></MaxFileSize>	       <!-- Max total input bytes per file for merged files, default is 1GB. Note: output file size may differ from total of input files -->
```

The first part of the XML file concerns arguments needed to run the program. The second part allows you to set up two things:
1. Global Settings stating that file format ```x``` should be converted to file format ```y```.
2. Folder Settings stating that file format ```x``` should be converted to file format ```y``` in the specific folder ```folder```.

### Global Settings
```xml
<FileClass>
    <ClassName>pdf</ClassName>
    <Default>fmt/477</Default>  <!-- The target PRONOM code the class should be converted to -->
    <FileTypes>
           <Filename>pdf</Filename>
       <Pronoms>       <!-- List of all PRONOMs that should be converted to the target PRONOM -->
              fmt/95,fmt/354,fmt/476,fmt/477 ,fmt/478 ,fmt/479 ,fmt/480
       </Pronoms>
    <Default></Default>
    </FileTypes>
</FileClass>
```

### Folder Settings
```xml
<FolderOverride>
	<FolderPath>apekatter</FolderPath>      <!-- Path after input folder example: /documents -->
	<Pronoms>fmt/41, fmt/42, fmt/43, fmt/44, x-fmt/398</Pronoms>
	<ConvertTo>fmt/14</ConvertTo>
	<MergeImages></MergeImages>             <!-- Yes, No -->
</FolderOverride>
```

## Currently supported file formats 
<div align=center>
<img width="900" src="https://github.com/larsmhaugland/file-converter/assets/117298604/92f26f2b-2ad4-4179-ac6f-07fb4f7dc1ba">
</div>

For a more extensive **PRONOM-based** overview you can ***click*** on the following image to be taken to a codesandbox code snippet: <br>
[![Edit file-converter](https://github.com/larsmhaugland/file-converter/assets/117298604/e0b35b20-43e2-4081-908f-19cb885951e4)](https://codesandbox.io/p/sandbox/file-converter-ynh84y)

The code snippet is based on data from the following JSON file: **[Supported Conversions.](DevDocumentation/supportedconversions.json)** <br>
For a more extensive overview for **each external converter** see the following TXT file: **[Supported Conversions per converter.](DevDocumentation/supportedconversions.txt)**

## Documentation and logging 
The ```.txt```log files use the following convention and is automatically generated each time the program is run:
```
Type | (Error) Message | Format | Filetype | Filename
```
All log files can be found in the ```logs``` folder.

Additionally, a ```documentation.json``` file is created which lists all files and their respective data.
```json
{"Metadata": {
    "requester": "Name",
    "converter": "Name"
     "hashing": "SHA256"
  },
  "Files": [
    {
      "Filename": "output\\filename.pdf",
      "OriginalPronom": "fmt/14",
      "OriginalChecksum": "6c6458545d3a41967a5ef2f12b1b03ad6a6409641670f823635cfb766181f636",
      "OriginalSize": 513631,
      "TargetPronom": "fmt/477",
      "NewPronom": "fmt/477",
      "NewChecksum": "b462a8261d26ece8707fac7f6921cc0ddfb352165cb608a38fed92ed044a6a05",
      "NewSize": 519283,
      "Converter": [
	"iText7 8.0.3.0"
	],
      "IsConverted": true
    }]}
```

## Adding a new converter
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

> [!TIP]
> If you are adding an **executable** file that you want to use it needs to be included in the ```.csproj``` file as such to be loaded properly at runtime:
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
## Adding a new conversion path (Multistep conversion)
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

# Further Development
The ```PronomHelper.cs``` class has a static method ```string PronomToFullName(string pronom)``` to retrieve the full name of file formats based on data in the [British National Archives PRONOM lookup tool](https://www.nationalarchives.gov.uk/PRONOM/Format/proFormatSearch.aspx?status=new). The method was created using a small C++ program. As the British National Archives publishes more PRONOM PUIDs the method must be updated. The program is located [here](https://github.com/larsmhaugland/PRONOM-helper-creator), see the README in the repo for usage.

# 🌟 Acknowledgments
Our application makes use of several **external libraries and software** under their respective licenses, for further information see [External libraries and software](#external-libraries-and-software).
<br><br>
We would like to thank the **[Innlandet County Archive](https://www.visarkiv.no/)** for giving us such an interesting task for our bachelor thesis. You have provided clear guidelines and invaluable feedback to us in the beta phase of our application.
<br><br>
Our bachelor thesis would also not have been possible without our **[supervisor Giorgio Trumpy](https://www.ntnu.no/ansatte/giorgio.trumpy)**. Thank you for keeping us on track, taking the initiative to connect us with archivists and librarians and delivering meaningful and constructive feedback.


# 🌍 Contributing
> [!IMPORTANT]
> We are currently **not** open for contributors, due to this being part of a bachelor thesis. <br>
> Hopefully, we will be able to open up for contributors after the thesis has been approved.

## Contributors
This project exists thanks to these wonderful people:<br>
<a href="https://github.com/larsmhaugland/file-converter/graphs/contributors)https://github.com/larsmhaugland/file-converter/graphs/contributors"><img src="https://contrib.rocks/image?repo=larsmhaugland/file-converter"/></a>

**Bachelor students from NTNU:**
- Aleksander Solhaug
- Philip Alexander Sundt
- Lars Martin Haugland
- Aurora Skomsvold

# 📄 Licensing
This project is licensed under the GNU Affero General Public License v3.0. as listed on https://spdx.org/licenses/ 
