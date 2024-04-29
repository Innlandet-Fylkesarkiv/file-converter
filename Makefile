# Project details
MAINPROJ = file-converter-prog2900.csproj
GUIPROJ = GUI\ChangeConverterSettings\ChangeConverterSettings.csproj
MAINPROJ_SLN = file-converter-prog2900.sln
GUIPROJ_SLN = GUI\ChangeConverterSettings.sln

WIN_OUTPUT_DIR = Windows
WIN_OUTPUT_GUI = ../../Windows/GUI
LIN_OUTPUT_DIR = Linux
LIN_OUTPUT_GUI = ../../Linux/GUI

WIN_RELEASE_ARCHIVE = $(WIN_OUTPUT_DIR).tar.gz
LIN_RELEASE_ARCHIVE = $(LIN_OUTPUT_DIR).tar.gz

# Define targets
test:
	dotnet test $(MAINPROJ)
	dotnet test $(GUIPROJ)

clean:
	dotnet clean $(MAINPROJ)
	dotnet clean $(GUIPROJ)
	rm -rf $(OUTPUT_DIR) $(OUTPUT_GUI)

sign_main: 	
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "$(WIN_OUTPUT_DIR)\file-converter-prog2900.exe"
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "$(LIN_OUTPUT_DIR)\file-converter-prog2900.dll"

sign_GUI:
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "Windows/GUI/ChangeConverterSettings.exe"
	signtool sign /f "Cert.pfx" /fd SHA1 /p prog2900 /t http://timestamp.digicert.com /v "Linux/GUI/ChangeConverterSettings.dll"

build_win:
	dotnet restore $(MAINPROJ_SLN)
	dotnet restore $(GUIPROJ_SLN)
	msbuild $(MAINPROJ) /p:Configuration=Release /p:OutputPath=$(WIN_OUTPUT_DIR)
	msbuild $(GUIPROJ) /p:Configuration=Release /p:OutputPath=$(WIN_OUTPUT_GUI)
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\ConversionTools\*.*" "$(WIN_OUTPUT_DIR)\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "GhostscriptBinaryFiles\gs10.02.1\*.*" "$(WIN_OUTPUT_DIR)\ConversionTools"
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\siegfried\*.*" "$(WIN_OUTPUT_DIR)\siegfried"
	echo D | xcopy /Y "ConversionSettings.xml" $(WIN_OUTPUT_DIR)
	echo D | xcopy /Y "README.md" $(WIN_OUTPUT_DIR)
	echo D | xcopy /Y "LICENSE" $(WIN_OUTPUT_DIR)

build_linux:
	dotnet restore $(MAINPROJ_SLN)
	dotnet restore $(GUIPROJ_SLN)
	msbuild $(MAINPROJ) /p:Configuration=Release /p:OutputPath=$(LIN_OUTPUT_DIR)
	msbuild $(GUIPROJ) /p:Configuration=Release /p:OutputPath=$(LIN_OUTPUT_GUI)
	echo D | xcopy /Y /S /EXCLUDE:exclude.txt "src\ConversionTools\*.*" "$(LIN_OUTPUT_DIR)\ConversionTools"
	echo D | xcopy /Y "ConversionSettings.xml" $(LIN_OUTPUT_DIR)
	echo D | xcopy /Y "README.md" $(LIN_OUTPUT_DIR)
	echo D | xcopy /Y "LICENSE" $(LIN_OUTPUT_DIR)

zip_release:
	tar -czf "$(WIN_RELEASE_ARCHIVE)" "$(WIN_OUTPUT_DIR)"
	tar -czf "$(LIN_RELEASE_ARCHIVE)" "$(LIN_OUTPUT_DIR)"
	
release: build_win build_linux sign_main sign_GUI
	$(MAKE) zip_release