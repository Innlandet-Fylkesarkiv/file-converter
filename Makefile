# Project details
MAINPROJ = file-converter-prog2900.csproj
GUIPROJ = GUI\ChangeConverterSettings\ChangeConverterSettings.csproj
MAINPROJ_SLN = file-converter-prog2900.sln
GUIPROJ_SLN = GUI\ChangeConverterSettings.sln
OUTPUT_DIR = release
OUTPUT_GUI = ../../release/GUI

RELEASE_DIR = release


# Directories and files to copy
GHOSTSCRIPT_DIR = GhostscriptBinaryFiles/gs10.02.1
CONVERSION_TOOLS_DIR = src/ConversionTools
SIEGFRIED_DIR = src/siegfried

# Define variables
RELEASE_DIR := release
RELEASE_ARCHIVE := $(RELEASE_DIR).tar.gz

# Define targets
test:
	dotnet test $(MAINPROJ)
	dotnet test $(GUIPROJ)

clean:
	dotnet clean $(MAINPROJ)
	dotnet clean $(GUIPROJ)
	rm -rf $(OUTPUT_DIR) $(OUTPUT_GUI)

sign1: 	
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "release\file-converter-prog2900.exe"

sign2:
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "release\GUI\ChangeConverterSettings.exe"

build:
	dotnet restore $(MAINPROJ_SLN)
	dotnet restore $(GUIPROJ_SLN)
	msbuild $(MAINPROJ) /p:Configuration=Release /p:OutputPath=$(OUTPUT_DIR)
	msbuild $(GUIPROJ) /p:Configuration=Release /p:OutputPath=$(OUTPUT_GUI)
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\ConversionTools\*.*" "release\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "GhostscriptBinaryFiles\gs10.02.1\*.*" "release\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\siegfried\*.*" "release\siegfried"
	echo D | xcopy /Y "Settings.xml" "release"
	echo D | xcopy /Y "README.md" "release"

zip_release:
	tar -czf "$(RELEASE_ARCHIVE)" "$(RELEASE_DIR)"
	echo "Release archive created: $(RELEASE_ARCHIVE)"
	
release: build sign1 sign2
	echo "Creating release package: $(RELEASE_ARCHIVE)"
	$(MAKE) zip_release