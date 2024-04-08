# Makefile for C# Projects

# Project details
MAINPROJ = file-converter-prog2900.csproj
GUIPROJ = GUI\ChangeConverterSettings\ChangeConverterSettings.csproj
OUTPUT_DIR = bin/Release
OUTPUT_GUI = bin/Release/GUI

RELEASE_DIR = release


# Directories and files to copy
GHOSTSCRIPT_DIR = GhostscriptBinaryFiles/gs10.02.1
CONVERSION_TOOLS_DIR = src/ConversionTools
SIEGFRIED_DIR = src/siegfried

# Define variables
RELEASE_DIR := release
RELEASE_ARCHIVE := $(RELEASE_DIR).tar.gz



# Define targets
build:
	dotnet build $(MAINPROJ) --configuration Release --output $(OUTPUT_DIR)
	dotnet build $(GUIPROJ) --configuration Release --output $(OUTPUT_GUI)

test:
	dotnet test $(MAINPROJ)
	dotnet test $(GUIPROJ)

clean:
	dotnet clean $(MAINPROJ)
	dotnet clean $(GUIPROJ)
	rm -rf $(OUTPUT_DIR) $(OUTPUT_DIR_GUI) release


build_release: 
	dotnet build $(MAINPROJ) --configuration Release --output release
	dotnet build $(GUIPROJ) --configuration Release --output release/GUI

# Pack the built files into a release package
release: build_release
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\ConversionTools\*.*" "release\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "GhostscriptBinaryFiles\gs10.02.1\*.*" "release\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\siegfried\*.*" "release\siegfried"
	echo D | xcopy /Y "Settings.xml" "release"
	$(MAKE) zip_release

# Define target to create a tar.gz archive of the release directory
zip_release:
	tar -czf "$(RELEASE_ARCHIVE)" "$(RELEASE_DIR)"
	echo "Release archive created: $(RELEASE_ARCHIVE)"
	
# Default target
.DEFAULT_GOAL := release