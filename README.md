# file-converter (W.I.P.)
> :memo: NOTE: This README is currently being updated, information found here might not reflect the current application.

![Static Badge](https://img.shields.io/badge/.net-8.0-blue)
![dotnet-badge](https://github.com/larsmhaugland/file-converter/actions/workflows/dotnet.yml/badge.svg?event=push)
[![License: AGPL v3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

A module-based .NET application that converts files and generates documentation for archiving.

This application provides a framework for different conversion libraries/software to work together. It aims to promote a comprehensive open-source solution for file conversion, as opposed to the many paid options, which allows for multi-step conversion between different external libraries. 

# Table of Contents 
- [Background](#background-)
- [Install](#install-)
  - [Dependencies](#dependencies-)
  	- [External libraries and software](#external-libraries-and-software)
  - [Installation for Windows](#installation-for-windows-)
  - [Installation for Linux](#installation-for-linux-)
  	-  [Installing Siegfried on Linux](#installing-siegfried-on-linux)
- [Usage](#usage-)
  - [Beta notes](#beta)	 
  - [GUI](#gui)
  - [CLI](#cli)
  - [Arguments](#arguments)
  - [Settings](#settings)
  - [Currently supported file formats](#currently-supported-file-formats)
  - [Documentation and logging](#documentation-and-logging)
  - [Adding a new converter or conversion path](#adding-a-new-converter-or-conversion-path)
- [Use cases](#use-cases)
- [Acknowledgments](#acknowledgments-)
- [Contributing](#contributing-)
- [Licensing](#licensing-)


# Background 📖
This project is part of a collaboration with [Innlandet County Archive](https://www.visarkiv.no/) and is a Bachelor's thesis project for a [Bachelor's in Programming](https://www.ntnu.edu/studies/bprog) at the [Norwegian University of Technology and Science (NTNU)](https://www.ntnu.edu/).

In Norway, the act of archiving is regulated by the Archives Act, which states that public bodies have a duty to register and preserve documents that are created as part of their activity [^1]. As society is becoming more digitized so is information, and the documents that were previously physical and stored physically are now digital and stored digitally. Innlandet County Archive is an inter-municipal archive cooperation, that registers and preserves documents from 48 municipalities. However, not all file types they receive are suitable for archiving as they run a risk of becoming obsolete. (For further reading see: [Obsolescence: File Formats and Software](https://dpworkshop.org/dpm-eng/oldmedia/obsolescence1.html)) Innlandet County Archive wished to streamline its conversion process into one application that could deal with a vast array of file formats. Furthermore, archiving is based on the principles of accessibility, accountability and integrity, which is why this application also provides documentation of all changes made to files.

Much like programmers and software developers, archivists believe in an open source world. Therefore it would only be right for this program to be open source. 

[^1]: Kultur- og likestillingsdepartementet. *Lov om arkiv [arkivlova].* URL: https://lovdata.no/dokument/NL/lov/1992-12-04-126?q=arkivloven (visited on 17th Jan. 2024).

# Install ⏬
To download the application source code simply run:
 ```sh
  git clone --recursive https://github.com/larsmhaugland/file-converter.git
```

Then open it in your .NET IDE of choice (We are using Microsoft Visual Studios) and build it. 
<br>Alternatively, you can build it from the command line using:
```sh
dotnet build
```

> :memo: NOTE: Cloning **with** the Git submodules is required for the application to work.
>If you did not clone the repository recursively or do not see the git submodules in your local repository we would suggest:
> ```sh
>   git submodule init
>   git submodule update
>```

## Dependencies 👪
|OS| Dependencies | Needed for? |
|---|---|---|
|Windows and Linux| [dotnet version 8.0](https://dotnet.microsoft.com/en-us/download) | Needed to build and run the program. |
| Windows and Linux| [Java JDK (only JRE also works)](https://www.oracle.com/java/technologies/downloads/)| Dependency for using the e-mail converter. |
| Windows| [LibreOffice, download the 7.6.6 version.] (https://www.libreoffice.org/download/download-libreoffice/?type=win-x86_64&version=7.6.6&lang=nb)| Required for converting office documents (Word, PowerPoint, Excel and OpenOffice). |
| Linux| Libreoffice should already be present on the system. This can be checked with "Soffice --version". Otherwise, download from link above.||
| Windows and Linux| [wkhtmltopdf version 0.12.6](https://wkhtmltopdf.org/downloads.html)| Needed for converting emails. |
| Linux | [Siegfried](https://github.com/richardlehane/siegfried) | To identify files and keep track of the conversion process. |
> :memo: NOTE: If you are on Linux see [Installation for Linux](#installation-for-linux) for more info on Siegfried installation.

<br>

### Further download instructions for LibreOffice 
**WINDOWS**<br>
Libreoffice must be manually added to ```PATH``` on Windows for the program to convert office files. 

Open *Settings* -> *Home* -> *About* (scroll down on the left) -> *Advanced system settings* (on the right) -> *Environment variables.*<br>
Alternatively use the ```Windows key + R``` on the keyboard, then type in ```"sysdm.cpl"``` and hit enter. Thereafter, press *Advanced* and then *Environment variables.*

The deafult installation path to Libreoffice is ```"C:\Program Files\LibreOffice"```. The "program" folder must be added to ```PATH```, meaning the entry should be ```"C:\Program Files\LibreOffice\program"```.
To add this locate the ```PATH``` variable and highlight it. Press *Edit* -> *New* -> copy the path to the program folder -> press *Ok*. This adds it to the users environment variables, but it can also be added as a system wide environment variable. 
<br><br>
[wkhtmltopdf](https://wkhtmltopdf.org/downloads.html) must also be manually added to ```PATH```. For windows, it can be done as described above, just swap ```"C:\Program Files\LibreOffice\program"``` with ```"C:\Program Files\wkhtmltopdf\bin"```. 
<br><br>
**LINUX**<br>
For Linux the default installation directory is ```...``` One alternative for adding it as an environment variable is to open the file ```.bashrc``` using the command ```nano ~/.bashrc```. Navigate to the bottom of the file with the arrow keys and add this line at the end ```export PATH="$PATH:DefaultPathHere"```. Remember to save the file and exit. To apply the changes immediately run the command ```source ~/.bashrc```. Alternatively, log in and out. To verify, run the command ```echo $PATH``` and the path added should be at the end of the output from the command.
<br><br>

### External libraries and software
**Libraries**
- [iText7](https://github.com/itext/itext-dotnet) under the GNU Affero General Public License v3.0.
- [BouncyCastle.NetCore](https://github.com/chrishaly/bc-csharp) under the MIT License.
- [iText7 Bouncycastle Adapter](https://www.nuget.org/packages/itext7.bouncy-castle-adapter/8.0.2) under the GNU Affero General Public License v3.0.
- [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/) under the MIT License.
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) under the MIT License.
- [Avalonia](https://avaloniaui.net/) under the MIT License.

**Software**
- [GhostScript](https://www.ghostscript.com/index.html) under the GNU Affero General Public License v3.0.
- [LibreOffice](https://www.libreoffice.org/) under the Mozilla Public License 2.0.
- [wkhtmltopdf](https://wkhtmltopdf.org/) under the GNU Lesser General Public License v3.0.
- [email-outlook-message-perl](https://github.com/mvz/email-outlook-message-perl) under the GNU Affero General Public License v3.0.
- [Siegfried](https://www.itforarchivists.com/siegfried/) under the Apache License 2.0.

## Installation for Windows 🪟
## Installation for Linux 🐧
The application can be used for Linux by downloading from source code. (see [Install](#install))

The application has been tested on the following Linux images:
- Debian "bookworm" 12
- Ubuntu Jammy Jellyfish 22.04 LTS
- Fedora Workstation 39
- Arch (kernel: Linux 6.7.7-arch1-1)

Running it on other distributions or other versions should be possible as long as it supports [dotnet version 8.0](https://dotnet.microsoft.com/en-us/download).
> :memo: NOTE: Although running ***our*** application on other distributions should be fine it may reduce the amount of supported external libraries and software.

### Installing Siegfried on Linux
|<img width="750" alt="Screenshot of guided installation of Siegfried on Linux" src="https://github.com/larsmhaugland/file-converter/assets/117298604/b89dd844-73af-43d9-a056-b6cd417733a1">|
|:--:| 
|*Screenshot of guided installation of Siegfried on Linux*|

If you are using a *Debian*, *Arch* or *Red Hat* based distro the application will guide you through Siegfried installation if it isn't already installed. 

Please see the dependencies needed for installation below:
| Distro | Dependency |
|---|---|
| Ubuntu/Debian | curl |
| Arch Linux | curl <br> brew [^2] |
| Fedora/Red Hat | brew [^2] |

If you are not using on of these distros please see the [Siegfried GitHub](https://github.com/richardlehane/siegfried) for information on downloading Siegfried.

[^2]:*Homebrew on Linux* URL: https://docs.brew.sh/Homebrew-on-Linux (visited on 3rd Mar. 2024)

# Usage 🔆
Common usage (code block)

## Beta
Since the program is still in beta, there are some limitations or bugs in the software. This section will be updated throughout the development process as we fix or find problems.
The program is mostly tested in Windows, so Linux specific issues may not appear in list. 
- (Landscape oriented PDF or PDF/A -> PDF/A or other PDF version)
  	- There is a bug where iText7 doesn't recognize that a document is landscape oriented when converting from PDF->PDF/A. This results in the content of the file being cropped to portrait.
  	- UPDATE: May be resolved, more testing required.
- Parsing siegfried data from incomplete run
  	- The current version of the program cannot successfully recover siegfried data from an incomplete run
- Timeout
  	- Timeout for conversion is not yet implemented
- (Compressed files)
  	- Zip is the only tested format, but .tar .gz .7z and .rar is also supported in the current version. It seems stable, but it is best to double check results.
- HTML -> PDF
  	- This conversion does not work, even though the program may think it did based on the output file's pronom. Output file will be empty.
  
## GUI
Common usage GUI

## CLI
Cover options and common usage
```
$ cd C:\PathToFolder\bin\Debug\net8.0
$ .\file-converter-prog2900.exe 
```

## Arguments
> :memo: Note: All paths must be absolute or relative to executable.

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
$ .\example -s "C:\Users\user\custom_settings.xml"
$ .\example --settings "C:\Users\user\custom_settings.xml"
```

### Accept all queries in CLI
```
$ .\example -y
$ .\example --yes 
```


## Settings
Settings can be manually set in the ```Settings.xml``` file.

### Setting run time arguments
```xml  
    <Requester></Requester>                    <!-- Name of person requesting the conversion -->
    <Converter></Converter>                    <!-- Name of person doing the conversion -->
	<ChecksumHashing></ChecksumHashing>          <!-- SHA256 (standard) or MD5 -->
	<InputFolder></InputFolder>                  <!-- Specify input folder, default is "input" -->
	<OutputFolder></OutputFolder>                <!-- Specify output folder, default is "output" -->
	<MaxThreads></MaxThreads>	                   <!-- Write a number, deafult is cores*2 -->
	<Timeout></Timeout>			                     <!-- Timeout in minutes, default is 30min -->
	<MaxFileSize></MaxFileSize>		<!-- Max total input bytes per file for merged files, default is 1GB. Note: output file size may differ from total of input files -->
```

The first part of the XML file concerns arguments needed to run the program. The second part allows you to set up two things:
1. Global settings stating that file format ```x``` should be converted to file format ```y```.
2. Folder settings stating that file format ```x``` should be converted to file format ```y``` in the specific folder ```folder```.

### Global settings
```xml
<FileClass>
	<ClassName>pdf</ClassName>
	<Default>fmt/477</Default>        <!-- The target PRONOM code the class should be converted to -->
	<FileTypes>
		<Filename>pdf</Filename>
    <Pronoms>                             <!-- List of all PRONOMs that should be converted to the target PRONOM -->
			fmt/95,fmt/354,fmt/476,fmt/477 ,fmt/478 ,fmt/479 ,fmt/480
		</Pronoms>
		<Default></Default>
	</FileTypes>
</FileClass>
```

### Folder settings
```xml
	<FolderOverride>
		<FolderPath>apekatter</FolderPath>      <!-- Path after input folder example: /documents -->
		<Pronoms>fmt/41, fmt/42, fmt/43, fmt/44, x-fmt/398</Pronoms>
		<ConvertTo>fmt/14</ConvertTo>
		<MergeImages></MergeImages>             <!-- Yes, No -->
	</FolderOverride>
```

## Currently supported file formats 
<img width="900" src="https://github.com/larsmhaugland/file-converter/assets/117298604/4cdfac37-120b-49b0-8aed-f2d85ee2c0ce">


## Documentation and logging 
The ```.txt```log files use the following convention and is automatically generated each time the program is run:
```
Type	| (Error) Message | Format | Filetype | Filename
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

## Adding a new converter or conversion path
Adding new external converters or conversion paths is described in detail in the **[Adding a new converter](https://github.com/larsmhaugland/file-converter/blob/main/addingconverter.md) guide.**

# Use cases
Here are the use cases, please reflect and write something down for each use case about how understandable the README/source code was and how manageable it was to do the task.

Use case tasks:
+ Add a new conversion path route that converts a document from Word to PDF to PDF-A.
+ Change the settings, using the ```settings.xml``` so that Word documents get converted to PDF-A.
+ Change the settings, using the GUI, so that Word documents get converted to PDF-A.
+ Run the program in CLI with the proper options to specify the input and output directory.
+ Try to combine a set of images into one PDF.

Questions regarding use cases:
+ Were there any tasks you weren't able to complete? Was it because of a lack of understanding or due to a bug in the program?
+ Were there any tasks that you feel could be simplified?

Questions regarding README:
+ Was it clear how to download, install and build the program?
+ Did you encounter any instructions in the README that weren't correct?
+ Were there any sections you found vague/unhelpful?
+ Are there some sections you would have liked that weren't here?

# Acknowledgments 🌟
Our application makes use of several **external libraries and software** under their respective licenses, for further information see [External libraries and software](#external-libraries-and-software).
<br><br>
We would like to thank the **[Innlandet County Archive](https://www.visarkiv.no/)** for giving us such an interesting task for our bachelor thesis. You have provided clear guidelines and invaluable feedback to us in the beta phase of our application.
<br><br>
Our bachelor thesis would also not have been possible without our **[supervisor Giorgio Trumpy](https://www.ntnu.no/ansatte/giorgio.trumpy)**. Thank you for keeping us on track, taking initiative in connecting us with archivists and librarians and for delivering meaningful and constructive feedback.


# Contributing 🌍
> ❗TODO:Explain how one can contribute.

## Contributors
This project exists thanks to these wonderful people:<br>
<a href="https://github.com/larsmhaugland/file-converter/graphs/contributors)https://github.com/larsmhaugland/file-converter/graphs/contributors"><img src="https://contrib.rocks/image?repo=larsmhaugland/file-converter"/></a>

**Bachelor students from NTNU:**
- Aleksander Solhaug
- Philip Alexander Sundt
- Lars Martin Haugland
- Aurora Skomsvold

# Licensing 📄
This project is licensed under the GNU Affero General Public License v3.0. as listed on https://spdx.org/licenses/ 
