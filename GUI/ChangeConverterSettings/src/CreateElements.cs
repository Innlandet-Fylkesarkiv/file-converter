using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using ChangeConverterSettings;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Platform;
using System.Windows;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using System.Globalization;
using Avalonia.Input;
public class CreateElements
{
    private readonly MainWindow mainWindow;

    /// <summary>
    /// Constructor for the CreateElements class. Sets the main window to the one passed in
    /// </summary>
    /// <param name="_mainWindow"> the mainwindow </param>
    public CreateElements(MainWindow _mainWindow)
    {
        mainWindow = _mainWindow;
    }

    /// <summary>
    /// Constructor for the CreateElements class. Creates and adds all the elements to a row in the main grid.
    /// </summary>
    /// <param name="index"> the row number </param>
    /// <param name="mainPanel"> the main StackPanel </param>
    /// <param name="_mainWindow"> The main window </param>
    public CreateElements(int index, StackPanel mainPanel, MainWindow _mainWindow)
    {
        mainWindow = _mainWindow;
        CreateAndAddElements(index);
    }

    /// <summary>
    /// Creates all the elements for a row and adds them to the panel
    /// </summary>
    /// <param name="index"> the index in filesettings and to name controls </param>
    public void CreateAndAddElements(int index)
    {
        Grid? mainGrid = mainWindow.Find<Grid>("MainGrid");
        if (mainGrid == null) {
            Logger.Instance.SetUpRunTimeLogMessage("MainGrid is null", true);
            return;
        }
        int newRow = FindLastRow(mainGrid) + 1;
        RowDefinition newRowDefinition = new RowDefinition();
        newRowDefinition.Height = GridLength.Auto;
        mainGrid.RowDefinitions.Add(newRowDefinition);
        var fileSettings = GlobalVariables.FileSettings[index];

        var className = fileSettings.ClassName;
        var existingClassName = ComponentLists.formatNames.FirstOrDefault(tb => ((TextBlock)tb).Text == className);
        ComboBox formatDropDown;
        bool isChecked = false;

        if (existingClassName == null)
        {
            // Column1: FileClass name
            var formatTextBlock = CreateControl.CreateTextBlock(className);
            Grid.SetRow(formatTextBlock, newRow);
            Grid.SetColumn(formatTextBlock, 0);
            mainGrid.Children.Add(formatTextBlock);
            ComponentLists.formatNames.Add(formatTextBlock);

            // Column2: FileTypes name
            formatDropDown = CreateControl.CreateComboBox(index);
            Grid.SetRow(formatDropDown, newRow);
            Grid.SetColumn(formatDropDown, 1);
            mainGrid.Children.Add(formatDropDown);
            ComponentLists.formatDropDowns.Add(formatDropDown);
            formatDropDown.Items.Add(GlobalVariables.defaultText);
            ComponentLists.outputTracker.Add(fileSettings.ClassName, (fileSettings.ClassDefault,isChecked));
            formatDropDown.SelectionChanged += (sender, e) => FormatDropDown_SelectionChanged(sender, e);

            // Column3: Output Pronom Code
            var defaultTypeTextBox = CreateControl.CreateTextBox(fileSettings.DefaultType, index, false);
            Grid.SetRow(defaultTypeTextBox, newRow);
            Grid.SetColumn(defaultTypeTextBox, 2);
            mainGrid.Children.Add(defaultTypeTextBox);
            ComponentLists.outputPronomCodeTextBoxes.Add(defaultTypeTextBox);
            defaultTypeTextBox.TextChanged += (sender, e) => Text_Changed(sender, e);

            // Column4: Name of Output Pronom Code
            var outputTypeTextBox = CreateControl.CreateTextBox(PronomHelper.PronomToFullName(fileSettings.DefaultType), index, true);
            Grid.SetRow(outputTypeTextBox, newRow);
            Grid.SetColumn(outputTypeTextBox, 3);
            mainGrid.Children.Add(outputTypeTextBox);
            ComponentLists.outputNameTextBoxes.Add(outputTypeTextBox);

            // Column5: DoNotConvert CheckBox
            var doNotConvertCheckBox = CreateControl.CreateCheckBox("Do Not Convert");
            Grid.SetRow(doNotConvertCheckBox, newRow);
            Grid.SetColumn(doNotConvertCheckBox, 4);
            doNotConvertCheckBox.IsCheckedChanged += (sender, e) => DoNotConvertCheckBox_Checked(sender, e);  
            mainGrid.Children.Add(doNotConvertCheckBox);
            ComponentLists.doNotConvertCheckBoxes.Add(doNotConvertCheckBox);
            doNotConvertCheckBox.IsChecked = fileSettings.DoNotConvert;
        }
        else
        {
            formatDropDown = (ComboBox)ComponentLists.formatDropDowns[ComponentLists.formatNames.IndexOf(existingClassName)];
        }

        isChecked = fileSettings.DoNotConvert;
        formatDropDown.Items.Add(fileSettings.FormatName);
        ComponentLists.outputTracker.Add(fileSettings.FormatName, (fileSettings.DefaultType, isChecked));
        formatDropDown.SelectedIndex = 0;

    }
    /// <summary>
    /// Finds the last row in the grid
    /// </summary>
    /// <param name="grid"> main grid </param>
    /// <returns> the last row </returns>
    private int FindLastRow(Grid grid)
    {
        int lastRow = -1; 

        foreach (var child in grid.Children)
        {
            if (child is Control control)
            {
                int row = Grid.GetRow(control);
                if (row > lastRow)
                {
                    lastRow = row;
                }
            }
        }
        return lastRow;
    } 

    /// <summary>
    /// Updates the entire outputracker
    /// </summary>
    public void UpdateState()
    {
        Grid? mainGrid = mainWindow.FindControl<Grid>("MainGrid");
        if (mainGrid == null)
        {
            Logger.Instance.SetUpRunTimeLogMessage("MainGrid is null", true);
            return;
        }
        foreach (RowDefinition row in mainGrid.RowDefinitions)
        {
            int rowIndex = mainGrid.RowDefinitions.IndexOf(row);

            // Access the child elements of the row (assuming ComboBox is the second child)
            ComboBox? comboBox = null;
            if (mainGrid.Children.Count > rowIndex * mainGrid.ColumnDefinitions.Count + 1) // Ensure index is within bounds
            {
                comboBox = mainGrid.Children[rowIndex * mainGrid.ColumnDefinitions.Count + 1] as ComboBox;
            }

            if (comboBox != null)
            {
                string? item = comboBox.SelectedItem?.ToString();
                if (item != null)
                {
                    UpdateOutputTracker(mainGrid, item, rowIndex);
                }
            }
        }
    }

    /// <summary>
    /// Updates the output tracker for a row
    /// </summary>
    /// <param name="grid"> the main grid </param>
    /// <param name="item"> the current item in the combobox </param>
    /// <param name="rowIndex"> the index of the row being updated </param>
    private static void UpdateOutputTracker(Grid grid, string item, int rowIndex)
    {
        var textBlock = (TextBlock)grid.Children[rowIndex * grid.ColumnDefinitions.Count];
        var comboBox = (ComboBox)grid.Children[rowIndex * grid.ColumnDefinitions.Count + 1];
        var textBox = (TextBox)grid.Children[rowIndex * grid.ColumnDefinitions.Count + 2];
        var readOnlyTextBox = (TextBox)grid.Children[rowIndex * grid.ColumnDefinitions.Count + 3];
        var doNotConvertCheckBox = (CheckBox)grid.Children[rowIndex * grid.ColumnDefinitions.Count + 4];

        string newType = textBox.Text.Trim();
        bool isChecked = (bool)doNotConvertCheckBox.IsChecked;
        var newPair = (newType, isChecked);

        if (item == GlobalVariables.defaultText)
        {
            item = textBlock.Text;
        }
        if (ComponentLists.outputTracker.ContainsKey(item))
        {
            var oldPair = ComponentLists.outputTracker[item];
            if (newPair != oldPair)
            {
                ComponentLists.outputTracker[item] = (newType, (bool)doNotConvertCheckBox.IsChecked);
            }
            readOnlyTextBox.Text = PronomHelper.PronomToFullName(newType);
        }
    }

    /// <summary>
    /// When the checkbox is checked, the DoNotConvert property is updated
    /// </summary>
    /// <param name="sender"> the checkbox being checked </param>
    /// <param name="e"> mandatory event, unused </param>
    private static void DoNotConvertCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        CheckBox? checkBox = (CheckBox)sender;
        Grid? mainGrid = (Grid)checkBox.Parent;
        int rowIndex = Grid.GetRow(checkBox);
        var textBlock = (TextBlock)mainGrid.Children[rowIndex * mainGrid.ColumnDefinitions.Count];
        var comboBox = (ComboBox)mainGrid.Children[rowIndex * mainGrid.ColumnDefinitions.Count + 1];
        var textBox = (TextBox)mainGrid.Children[rowIndex * mainGrid.ColumnDefinitions.Count + 2];
        int index = -1;

        textBox.IsEnabled = !textBox.IsEnabled;
        var selected = comboBox.SelectedItem;
        if (selected == null)
        {
            return;
        }
        if (selected.ToString() == "Default")
        {

            index = GlobalVariables.FileSettings.FindIndex(x => x.ClassName == textBlock.Text);
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString() != GlobalVariables.defaultText)
                {
                    GlobalVariables.FileSettings[index+i-1].DoNotConvert = (bool)checkBox.IsChecked;
                }
            }
        }
        else
        {
            index = GlobalVariables.FileSettings.FindIndex(x => x.FormatName == comboBox.SelectedItem.ToString());

            GlobalVariables.FileSettings[index].DoNotConvert = !GlobalVariables.FileSettings[index].DoNotConvert;
        }
            

 
    }



    /// <summary>
    /// When the selection of the ComboBox is changed, the output tracker is updated and the widths are recalculated
    /// </summary>
    /// <param name="sender"> The ComboBox being changed </param>
    /// <param name="e"> provides data about whats being changed, such as what it was before the change </param>

    private void FormatDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousSelection = "";
        if (e.RemovedItems.Count > 0)
        {
            previousSelection = e.RemovedItems[0].ToString();
        }

        var comboBox = (ComboBox)sender;
        var parent = (Grid)comboBox.Parent;

        // Calculate indices based on the row index
        int rowIndex = Grid.GetRow(comboBox);
        int classNameColumnIndex = 0;
        int comboBoxColumnIndex = 1;
        int textBoxColumnIndex = 2;
        int readOnlyTextBoxColumnIndex = 3;
        int doNotConvertCheckBoxColumnIndex = 4;

        // Access child elements using calculated indices
        var className = ((TextBlock)parent.Children[rowIndex * parent.ColumnDefinitions.Count + classNameColumnIndex]).Text;
        var textBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + textBoxColumnIndex];
        var readOnlyTextBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + readOnlyTextBoxColumnIndex];
        var doNotConvertCheckBox = (CheckBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + doNotConvertCheckBoxColumnIndex];
        var item = comboBox.SelectedItem.ToString();

        UpdateOutputTracker(parent, previousSelection, rowIndex);

        string type;
        bool isChecked = false;
        if (item == GlobalVariables.defaultText)
        {
            (type,isChecked) = ComponentLists.outputTracker[className];
        }
        else
        {
            (type, isChecked) = ComponentLists.outputTracker[item];
        }

        textBox.Text = type;
        readOnlyTextBox.Text = PronomHelper.PronomToFullName(type);
        doNotConvertCheckBox.IsChecked = isChecked;
    }

    /// <summary>
    /// When the text is changed, the name of the Pronom code is updated
    /// </summary>
    /// <param name="sender"> the textbox being typed in </param>
    /// <param name="e"> Event arguments containing information about the text change </param>
    private void Text_Changed(object sender, TextChangedEventArgs e)
    {
        string? newText = "";
        if (sender is TextBox textBox)
        {
            newText = textBox.Text;
        }
        Grid? mainGrid = mainWindow.FindControl<Grid>("MainGrid");
        if (mainGrid == null)
        {
            Logger.Instance.SetUpRunTimeLogMessage("MainGrid is null", true);
            return;
        }

        int index = mainGrid.Children.IndexOf((TextBox)sender) + 1;
        TextBox? readOnlyTextBox = (TextBox)mainGrid.Children[index];
        if (!String.IsNullOrEmpty(newText) && readOnlyTextBox != null)
            readOnlyTextBox.Text = PronomHelper.PronomToFullName(newText);
         
    }
}

