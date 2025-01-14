﻿using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Interactivity;
using ChangeConverterSettings;
using System.Reflection;
using Avalonia.Layout;
using Avalonia.Media;

public class FolderOverride
{
    Window mainWindow;
    public FolderOverride(Window _mainWindow)
    {
        mainWindow = _mainWindow;
    }

    /// <summary>
    /// Opens a folder picker dialog and adds the selected folder to the folder override
    /// </summary>
    /// <param name="caller"> the button calling this function </param>
    public async void SelectFolder(Button caller)
    {
        Console.WriteLine(GlobalVariables.FolderOverride);
        List<String> Folders = await HelperFunctions.SelectFolderInExplorer(mainWindow);
        if (Folders.Count == 0)
        {
            return;
        }
        Grid? mainGrid = mainWindow.FindControl<Grid>("FolderOverrideGrid");
        Grid? innerGrid = caller.Parent as Grid;
        if (mainGrid == null || innerGrid == null)
        {
            return;
        }
        foreach (string folder in Folders)
        {
            if (folder == null)
            {
                return;
            }

            TextBlock? folderName = innerGrid.Children.OfType<TextBlock>().FirstOrDefault();
            if (folderName == null)
            {
                return;
            }
            folderName.Text = folder;
            GlobalVariables.FolderOverride.Add(folder, new SettingsData());
        }
        if (IsLast(mainGrid, innerGrid))
        {
            SetUpInnerGrid("");
        }
    }

    /// <summary>
    /// Sets up a inner grid and a separator for the folder override
    /// </summary>
    /// <param name="foldername"> the folder name </param>
    public void SetUpInnerGrid(string foldername)
    {
        Grid? mainGrid = mainWindow.FindControl<Grid>("FolderOverrideGrid");
        if (mainGrid == null)
        {
            return;
        }
        int index = mainGrid.Children.Count / 2 + 1;

        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid innerGrid = CreateControl.CreateFolderOverrideGrids(index);
        Grid.SetRow(innerGrid, index);
        mainGrid.Children.Add(innerGrid);

        TextBlock folderName = CreateControl.CreateTextBlock(foldername);
        folderName.Name = "FolderName" + index;
        Grid.SetColumn(innerGrid, innerGrid.Children.Count);
        innerGrid.Children.Add(folderName);

        TextBox inputPRONOMs = CreateControl.CreateTextBox("", index, false);
        inputPRONOMs.Name = "InputPRONOMs" + index;
        inputPRONOMs.Watermark = "Input PRONOM codes in a list format \n e.g \"fmt/1, fmt/2\"";
        Grid.SetColumn(inputPRONOMs, innerGrid.Children.Count);
        innerGrid.Children.Add(inputPRONOMs);

        TextBox outputPRONOMs = CreateControl.CreateTextBox("", index, false);
        outputPRONOMs.Name = "OutputPRONOMs" + index;
        outputPRONOMs.Watermark = "PRONOM code of they should be converted to";
        Grid.SetColumn(outputPRONOMs, innerGrid.Children.Count);
        innerGrid.Children.Add(outputPRONOMs);

        CheckBox mergeFiles = CreateControl.CreateCheckBox("Merge Files");
        Grid.SetColumn(mergeFiles, innerGrid.Children.Count);
        innerGrid.Children.Add(mergeFiles);

        Button folderButton = CreateControl.CreateButton("Select Folders", index);
        Grid.SetColumn(folderButton, innerGrid.Children.Count);
        folderButton.Click += (sender, e) => FolderButton_Click(folderButton, e);
        innerGrid.Children.Add(folderButton);

        Button removeButton = CreateControl.CreateButton("Remove Override", index);
        removeButton.Click += (sender, e) => RemoveButton_Click(removeButton, e);
        Grid.SetColumn(removeButton, innerGrid.Children.Count);
        innerGrid.Children.Add(removeButton);

        Avalonia.Controls.Shapes.Rectangle separator = CreateControl.CreateSeparator();
        Grid.SetRow(separator, index);
        mainGrid.Children.Add(separator);
    }

    /// <summary>
    /// When the folder button is clicked, it opens a folder picker dialog
    /// </summary>
    /// <param name="sender"> the button being pressed </param>
    /// <param name="e"> Event arguments (not used, but necessary to be able to call it) </param>
    private void FolderButton_Click(object sender, RoutedEventArgs e)
    {
        switch (sender)
        {
            case null: return;
            case Button button: SelectFolder(button); break;
            default: return;
        }
    }

    /// <summary>
    /// When the remove button is pressed, the corresponding folder override is removed
    /// </summary>
    /// <param name="sender"> the button calling this function </param>
    /// <param name="e"> Event arguments (not used, but necessary to be able to call it) </param>
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        Button? caller = sender as Button;
        if (caller == null)
        {
            return;
        }

        Grid? innerGrid = caller.Parent as Grid;
        Grid? mainGrid = mainWindow.FindControl<Grid>("FolderOverrideGrid");
        if (innerGrid == null || mainGrid == null)
        {
            return;
        }

        int index = mainGrid.Children.IndexOf(innerGrid);

        if (IsLast(mainGrid, innerGrid))
        {
            SetUpInnerGrid("");
        }

        // remove the folder from the dictionary
        foreach (var child in innerGrid.Children)
        {
            if (child is TextBlock textBlock)
            {
                if (string.IsNullOrEmpty(textBlock.Text))
                {
                    continue;
                }
                GlobalVariables.FolderOverride.Remove(textBlock.Text);
            }
        }

        // remove the inner grid and the separator
        if (mainGrid.Children[index + 1] is Avalonia.Controls.Shapes.Rectangle separator)
        {
            mainGrid.Children.Remove(separator);
            mainGrid.Children.Remove(innerGrid);
            mainGrid.RowDefinitions.RemoveAt(index/2);
        }
    }

    /// <summary>
    /// Checks if the inner grid is the last one in the main grid
    /// </summary>
    /// <param name="mainGrid"> the main grid </param>
    /// <param name="innerGrid"> the inner grid being checked </param>
    /// <returns> true if it is the last one </returns>
    private static bool IsLast(Grid mainGrid, Grid innerGrid)
    {
        int index = mainGrid.Children.IndexOf(innerGrid);
        return index + 2 == mainGrid.Children.Count;
    }

    /// <summary>
    /// Saves the state to GlobalVariables.FolderOverride
    /// </summary>
    public void SaveFolderOverride()
    {
        Grid? mainGrid = mainWindow.FindControl<Grid>("FolderOverrideGrid");
        if (mainGrid == null)
        {
            return;
        }

        foreach (var child in mainGrid.Children)
        {
            if (child is not Grid innerGrid) continue;

            foreach (var innerchild in innerGrid.Children)
            {
                if (innerchild is not TextBlock textBlock || string.IsNullOrEmpty(textBlock.Text)) continue;

                if (!GlobalVariables.FolderOverride.TryGetValue(textBlock.Text, out SettingsData? value)) continue;

                TextBox? inputPRONOMsControl = innerGrid?.Children.OfType<TextBox>()
                    .FirstOrDefault(control => control.Name.StartsWith("InputPRONOMs"));
                if (inputPRONOMsControl == null) continue;
                List<string>? inputPRONOMs = inputPRONOMsControl.Text?.ToString().Replace(" ", "").Split(',').ToList();
                if (inputPRONOMs != null)
                {
                    value.PronomsList = inputPRONOMs;
                }

                TextBox? outputPRONOMsControl = innerGrid?.Children.OfType<TextBox>()
                                    .FirstOrDefault(control => control.Name.StartsWith("OutputPRONOMs"));
                if (outputPRONOMsControl == null) continue;
                string? outputPRONOMs = outputPRONOMsControl.Text?.ToString().Trim();
                if (outputPRONOMs != null)
                {
                    value.DefaultType = outputPRONOMs;
                }
                if (innerGrid.Children.OfType<CheckBox>().FirstOrDefault()?.IsChecked == true)
                {
                    value.Merge = true;
                }
            }
        }
    }

    /// <summary>
    /// Reads the folder override from settings and writes the content to the screen
    /// as multiple segments
    /// </summary>
    public void WriteFolderOverrideToScreen()
    {
        foreach (var folder in GlobalVariables.FolderOverride)
        {
            SetUpInnerGrid(folder.Key);
        }
    }
}