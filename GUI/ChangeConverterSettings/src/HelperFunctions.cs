using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ChangeConverterSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class HelperFunctions
{
    /// <summary>
    /// Opens up a explorer window and prompts the user to select a folder
    /// </summary>
    /// <param name="window"> the window it opens up from</param>
    /// <returns> a list of all folders selected </returns>
    public static async Task<List<String>> SelectFolderInExplorer(Window window)
    {
        if (window == null)
        {
            return [];
        }
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null)
        {
            return [];
        }

        // Start async operation to open the dialog.
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder"
        });

        if (folders.Count > 0)
        {
            // Get the paths of selected folders
            var folderPaths = folders.Select(folder => folder.Path.ToString()).ToList();
            for(int i = 0; i < folderPaths.Count; i++)
            {
                if (folderPaths[i].StartsWith("file:///"))
                {
                    folderPaths[i] = folderPaths[i].Substring(8); // Remove "file:///" from the start of paths
                }
                if(GlobalVariables.Input == null)
                {
                    continue;
                }
                if (folderPaths[i].Contains(GlobalVariables.Input))
                {
                    char[] separator = { '/', '\\' };
                    List<string> newPath = folderPaths[i].Split(separator).ToList();
                    bool isFound = false;
                    while (!isFound)
                    {
                        if(string.Compare(newPath.First(), GlobalVariables.Input)==0)
                        {
                            isFound = true;
                        }
                        newPath.RemoveAt(0);
                    }
                    folderPaths[i] = string.Join("/", newPath);
                }
                if (GlobalVariables.FolderOverride.ContainsKey(folderPaths[i]))
                {
                    ShowWarningPopup("The selected folder is already a part of the folder override", window);
                    folderPaths.Remove(folderPaths[i]);
                }
            }
            return folderPaths;
        }
        return [];
    }

    /// <summary>
    /// Shows a warning popup
    /// </summary>
    /// <param name="warning"> the waring message </param>
    /// <param name="window"> which window it should pop up at, normally main </param>
    private static void ShowWarningPopup(string warning, Window window)
    {
        var warningWindow = new Window
        {
            Title = "Warning",
            Content = new TextBlock
            {
                Text = warning,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        warningWindow.ShowDialog(window);
    }
} 
