using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class HelperFunctions
{
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
            }
            return folderPaths;
        }
        return [];
    }
}

