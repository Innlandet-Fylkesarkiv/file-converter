using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml;
using System.IO;
using Avalonia.Media;
using System.Globalization;
using System.Reflection;
using Avalonia.Layout;
using Avalonia.Input;
using Tmds.DBus.Protocol;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Linq;
using Avalonia.Controls.Shapes;

namespace ChangeConverterSettings
{
    /// <summary>
    /// Class that contains main global variables
    /// </summary>
    public static class GlobalVariables
    {
        public static string Input = "";
        public static string Output = "";
        //List of all settingsdata
        public static List <SettingsData> FileSettings = new List<SettingsData>();
        // Map with info about what folders have overrides for specific formats
        public static Dictionary<string, SettingsData> FolderOverride = new Dictionary<string, SettingsData>(); // the key is a foldername
        public static int maxThreads = Environment.ProcessorCount * 2;
        public static string checksumHash = "SHA256";
        public static string requester = "";
        public static string converter = "";
        public static int timeout = 5;
        public static long maxFileSize = (1 * 1024);

        public static string defaultText = "Default";
        public static List<string> supportedHashes = new List<string> { "MD5", "SHA256" };
        public static string defaultSettingsPath = "../Settings.xml";
    }

    /// <summary>
    /// Class that contains all the components that are created
    /// </summary>
    public static class ComponentLists
    {
        public static List<TextBlock> formatNames = new List<TextBlock>();
        public static List<ComboBox> formatDropDowns = new List<ComboBox>();
        public static List<TextBox> outputPronomCodeTextBoxes = new List<TextBox>();
        public static List<TextBox> outputNameTextBoxes = new List<TextBox>();
        public static List<CheckBox> doNotConvertCheckBoxes = new List<CheckBox>();
        // Map with info about what Format/ClassName has what output PRONOM code
        public static Dictionary<string, (string,bool)> outputTracker = new Dictionary<string, (string,bool)>();
    }

    public partial class MainWindow : Window
    {
        /// <summary>
        /// Constructor for the main window
        /// </summary>
        public MainWindow()
        {
            while (!File.Exists("ConversionSettings.xml") && Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()) != Directory.GetCurrentDirectory())
                Directory.SetCurrentDirectory("../");
            GlobalVariables.defaultSettingsPath = Directory.GetCurrentDirectory() + "/ConversionSettings.xml";
            InitializeComponent();
            Settings settings = Settings.Instance;
            settings.ReadAllSettings(GlobalVariables.defaultSettingsPath);
            WriteSettingsToScreen();
            Console.WriteLine(GlobalVariables.FileSettings);
            FolderOverride folderOverride = new FolderOverride(this);
            folderOverride.WriteFolderOverrideToScreen();
            folderOverride.SetUpInnerGrid("");
        }

        /// <summary>
        /// Initializes the main window
        /// </summary>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void InputLostFocus(object sender, RoutedEventArgs args)
        {
            GlobalVariables.FolderOverride.Clear();
            TextBox? inputTextBox = this.FindControl<TextBox>("Input");
            if (inputTextBox != null)
            {
                GlobalVariables.Input = inputTextBox.Text;
            }
            Settings settings = Settings.Instance;
            settings.SetUpFolderOverride(GlobalVariables.defaultSettingsPath);
            FolderOverride folderOverride = new FolderOverride(this);
            var FolderOverrideGrid = this.FindControl<Grid>("FolderOverrideGrid");
            if (FolderOverrideGrid != null)
            {
                for (int i = 2; i < FolderOverrideGrid.Children.Count; i++)
                {
                    if (FolderOverrideGrid.Children[i] is Grid || FolderOverrideGrid.Children[i] is Rectangle)
                    {
                        FolderOverrideGrid.Children.RemoveAt(i);
                        i--;
                    }
                }
            }
            folderOverride.WriteFolderOverrideToScreen();
            folderOverride.SetUpInnerGrid("");
        }

        /// <summary>
        /// When the user presses the save button, the current values are written to the settings file
        /// </summary>
        /// <param name="sender"> the save button being pressed </param>
        /// <param name="args"> arguments (unused but neccesary to be able to call it) </param>
        public void SaveButton(object sender, RoutedEventArgs args)
        {
            FolderOverride folderOverride = new FolderOverride(this);
            folderOverride.SaveFolderOverride();
            SaveMainSettings();
        }

        private void SaveMainSettings()
        {
            CreateElements createElements = new CreateElements(this);
            createElements.UpdateState();

            TextBox? requesterTextBox = this.FindControl<TextBox>("Requester");
            if (requesterTextBox != null)
                GlobalVariables.requester = requesterTextBox.Text;
            TextBox? converterTextBox = this.FindControl<TextBox>("Converter");
            if (converterTextBox != null)
                GlobalVariables.converter = converterTextBox.Text;
            TextBox? inputTextBox = this.FindControl<TextBox>("Input");
            if (inputTextBox != null)
                GlobalVariables.Input = inputTextBox.Text;
            TextBox? outputTextBox = this.FindControl<TextBox>("Output");
            if (outputTextBox != null)
                GlobalVariables.Output = outputTextBox.Text;
            TextBox? threadsTextBox = this.FindControl<TextBox>("MaxThreads");
            if (threadsTextBox != null && int.TryParse(threadsTextBox.Text, out _))
                GlobalVariables.maxThreads = int.Parse(threadsTextBox.Text);
            else if (threadsTextBox != null && String.IsNullOrEmpty(threadsTextBox.Text))
                GlobalVariables.maxThreads = Environment.ProcessorCount * 2;
            ComboBox? checksumComboBox = this.FindControl<ComboBox>("Checksum");
            if (checksumComboBox != null && checksumComboBox.SelectedItem != null)
                GlobalVariables.checksumHash = checksumComboBox.SelectedItem.ToString();
            TextBox? timeoutTextBox = this.FindControl<TextBox>("Timeout");
            if (timeoutTextBox != null && timeoutTextBox.Text != null)
                if (!int.TryParse(timeoutTextBox.Text, out GlobalVariables.timeout)) { GlobalVariables.timeout = 5; }
            TextBox? maxFileSizeTextBox = this.FindControl<TextBox>("MaxFileSize");
            if (maxFileSizeTextBox != null)
            {
                if (long.TryParse(maxFileSizeTextBox.Text, out long fileSize))
                {
                    fileSize = fileSize * 1024 * 1024;
                    GlobalVariables.maxFileSize = fileSize;
                } else
                {
                    GlobalVariables.maxFileSize = 1024 * 1024;
                }
            }

            // Goes through all FileTypes and sets the default types and class defaults for every format
            foreach (var settingsData in GlobalVariables.FileSettings)
            {
                for (int i = 0; i < ComponentLists.formatDropDowns.Count; i++)
                {
                    ComboBox formatDropDown = ComponentLists.formatDropDowns[i];
                    int prevSelecIndex = formatDropDown.SelectedIndex;
                    for (int j = 0; j < formatDropDown.Items.Count; j++)
                    {
                        formatDropDown.SelectedIndex = j;
                        string? name = formatDropDown.SelectedItem.ToString();
                        string? text;
                        bool doNotConvert;
                        if (name != GlobalVariables.defaultText)
                        {
                            (text, doNotConvert) = ComponentLists.outputTracker[name];
                        }
                        else
                        {
                            (text, doNotConvert) = ComponentLists.outputTracker[settingsData.ClassName];
                        }
                        if (name != null && text != null)
                        {
                            if (settingsData.FormatName == name)
                            {
                                settingsData.DoNotConvert = doNotConvert;
                                settingsData.DefaultType = text;
                            }       
                            else if (name == GlobalVariables.defaultText && settingsData.ClassName == ComponentLists.formatNames[i].Text)
                                settingsData.ClassDefault = text;
                        }
                    }
                    ComponentLists.formatDropDowns[i].SelectedIndex = prevSelecIndex;
                }
            }
            Settings settings = Settings.Instance;
            settings.WriteSettings(GlobalVariables.defaultSettingsPath);
        }

        /// <summary>
        /// Reads the settings from the settings file and writes them to the screen
        /// </summary>
        private void WriteSettingsToScreen()
        {
            // Fills the standard TextBoxes with values from the settings file
            TextBox? requesterTextBox = this.FindControl<TextBox>("Requester");
            if (requesterTextBox != null)
                requesterTextBox.Text = GlobalVariables.requester;
            TextBox? converterTextBox = this.FindControl<TextBox>("Converter");
            if (converterTextBox != null)
                converterTextBox.Text = GlobalVariables.converter;
            TextBox? inputTextBox = this.FindControl<TextBox>("Input");
            if (inputTextBox != null)
                inputTextBox.Text = GlobalVariables.Input;
            TextBox? outputTextBox = this.FindControl<TextBox>("Output");
            if (outputTextBox != null)
                outputTextBox.Text = GlobalVariables.Output;
            TextBox? threadsTextBox = this.FindControl<TextBox>("MaxThreads");
            if (threadsTextBox != null)
                threadsTextBox.Text = GlobalVariables.maxThreads.ToString();
            TextBox? timeoutTextBox = this.FindControl<TextBox>("Timeout");
            if (timeoutTextBox != null)
                timeoutTextBox.Text = GlobalVariables.timeout.ToString();
            TextBox? maxFileSizeTextBox = this.FindControl<TextBox>("MaxFileSize");
            if (maxFileSizeTextBox != null)
            {
                maxFileSizeTextBox.Text = GlobalVariables.maxFileSize.ToString();
            }

            // Fills the checksum ComboBox with values from the settings file
            ComboBox? checksumComboBox = this.FindControl<ComboBox>("Checksum");
            if (checksumComboBox != null)
            {
                foreach (string hash in GlobalVariables.supportedHashes)
                {
                    checksumComboBox.Items.Add(hash);
                }
                checksumComboBox.SelectedItem = GlobalVariables.checksumHash;
            }

            // Sorts the settings list by format name
            GlobalVariables.FileSettings.Sort((x, y) => x.FormatName.CompareTo(y.FormatName));

            // Goes through the settings list and creates the elements on the screen
            for (int i = 0; i < GlobalVariables.FileSettings.Count; i++)
            {
                ComboBox? formatDropDown = this.FindControl<ComboBox>("formatDropDown" + (i + 1).ToString());
                if (formatDropDown == null)
                {
                    StackPanel? stackPanelMain = this.FindControl<StackPanel>("MainStackPanel");
                    if (stackPanelMain != null)
                    {
                        CreateElements creator = new CreateElements(i, stackPanelMain, this);
                    }
                }
            }
        }
    }
}