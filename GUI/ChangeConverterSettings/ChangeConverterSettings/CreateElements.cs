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
    /// <param name="index"></param>
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

        if (existingClassName == null)
        {
            // Column1: FileClass name
            var formatTextBlock = CreateTextBlock(className);
            Grid.SetRow(formatTextBlock, newRow);
            Grid.SetColumn(formatTextBlock, 0);
            mainGrid.Children.Add(formatTextBlock);
            ComponentLists.formatNames.Add(formatTextBlock);

            // Column2: FileTypes name
            formatDropDown = CreateComboBox(index);
            Grid.SetRow(formatDropDown, newRow);
            Grid.SetColumn(formatDropDown, 1);
            mainGrid.Children.Add(formatDropDown);
            ComponentLists.formatDropDowns.Add(formatDropDown);
            formatDropDown.Items.Add(GlobalVariables.defaultText);
            ComponentLists.outputTracker.Add(fileSettings.ClassName, fileSettings.ClassDefault);
            formatDropDown.SelectionChanged += (sender, e) => FormatDropDown_SelectionChanged(sender, e);

            // Column3: Output Pronom Code
            var defaultTypeTextBox = CreateTextBox(fileSettings.DefaultType, index, false);
            Grid.SetRow(defaultTypeTextBox, newRow);
            Grid.SetColumn(defaultTypeTextBox, 2);
            mainGrid.Children.Add(defaultTypeTextBox);
            ComponentLists.outputPronomCodeTextBoxes.Add(defaultTypeTextBox);

            // Column4: Name of Output Pronom Code
            var outputTypeTextBox = CreateTextBox(PronomHelper.PronomToFullName(fileSettings.DefaultType), index, true);
            Grid.SetRow(outputTypeTextBox, newRow);
            Grid.SetColumn(outputTypeTextBox, 3);
            mainGrid.Children.Add(outputTypeTextBox);
            ComponentLists.outputNameTextBoxes.Add(outputTypeTextBox);

            // Column5: Update Button
            var updateButton = CreateButton("Update", index);
            updateButton.Click += (sender, e) => UpdateButton_Click(sender);
            Grid.SetRow(updateButton, newRow);
            Grid.SetColumn(updateButton, 4);
            mainGrid.Children.Add(updateButton);
            ComponentLists.updateButtons.Add(updateButton);
        }
        else
        {
            formatDropDown = (ComboBox)ComponentLists.formatDropDowns[ComponentLists.formatNames.IndexOf(existingClassName)];
        }

        formatDropDown.Items.Add(fileSettings.FormatName);
        ComponentLists.outputTracker.Add(fileSettings.FormatName, fileSettings.DefaultType);
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
    /// Updates the entire outputracker and recalculates the widths
    /// </summary>
    public void UpdateState()
    {
        Grid? mainGrid = mainWindow.FindControl<Grid>("MainGrid");
        foreach (RowDefinition row in mainGrid.RowDefinitions)
        {
            int rowIndex = mainGrid.RowDefinitions.IndexOf(row);

            // Access the child elements of the row (assuming ComboBox is the second child)
            ComboBox comboBox = null;
            if (mainGrid.Children.Count > rowIndex * mainGrid.ColumnDefinitions.Count + 1) // Ensure index is within bounds
            {
                comboBox = mainGrid.Children[rowIndex * mainGrid.ColumnDefinitions.Count + 1] as ComboBox;
            }

            if (comboBox != null)
            {
                string item = comboBox.SelectedItem?.ToString();
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

        string newType = textBox.Text.Trim();

        string oldType;
        if (item == GlobalVariables.defaultText)
        {
            item = textBlock.Text;
        }
        if (ComponentLists.outputTracker.ContainsKey(item))
        {
            oldType = ComponentLists.outputTracker[item];

            if (newType != oldType)
            {
                ComponentLists.outputTracker[item] = newType;
            }
            readOnlyTextBox.Text = PronomHelper.PronomToFullName(newType);
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

        // Access child elements using calculated indices
        var className = ((TextBlock)parent.Children[rowIndex * parent.ColumnDefinitions.Count + classNameColumnIndex]).Text;
        var textBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + textBoxColumnIndex];
        var readOnlyTextBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + readOnlyTextBoxColumnIndex];
        var item = comboBox.SelectedItem.ToString();

        UpdateOutputTracker(parent, previousSelection, rowIndex);

        string type;
        if (item == GlobalVariables.defaultText)
        {
            type = ComponentLists.outputTracker[className];
        }
        else
        {
            type = ComponentLists.outputTracker[item];
        }

        textBox.Text = type;
        readOnlyTextBox.Text = PronomHelper.PronomToFullName(type);
    }

    /// <summary>
    /// When the update button is pressed, the output tracker is updated
    /// </summary>
    /// <param name="sender"> The Button being pressed </param>
    private void UpdateButton_Click(object sender)
    {
        var button = (Button)sender;
        var parent = (Grid)button.Parent;

        // Calculate indices based on the row index
        int rowIndex = Grid.GetRow(button);
        int classNameColumnIndex = 0;
        int comboBoxColumnIndex = 1;
        int textBoxColumnIndex = 2;
        int readOnlyTextBoxColumnIndex = 3;

        // Access child elements using calculated indices
        var className = ((TextBlock)parent.Children[rowIndex * parent.ColumnDefinitions.Count + classNameColumnIndex]).Text;
        var comboBox = (ComboBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + comboBoxColumnIndex];
        var textBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + textBoxColumnIndex];
        var readOnlyTextBox = (TextBox)parent.Children[rowIndex * parent.ColumnDefinitions.Count + readOnlyTextBoxColumnIndex];
        var item = comboBox.SelectedItem.ToString();

        UpdateOutputTracker(parent, item, rowIndex);
    }

    /// <summary>
    /// Creates a TextBlock
    /// </summary>
    /// <param name="text"> The textcontent of the TextBlock </param>
    /// <returns></returns>
    private static TextBlock CreateTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }

    /// <summary>
    /// creates a ComboBox
    /// </summary>
    /// <param name="index"> just to name it </param>
    /// <returns> The ComboBox with its values </returns>
    private static ComboBox CreateComboBox(int index)
    {
        var comboBox = new ComboBox
        {
            Name = "formatDropDown" + (index + 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
        return comboBox;
    }

    /// <summary>
    /// Creates a TextBox
    /// </summary>
    /// <param name="text"> The textcontent of the textbox </param>
    /// <param name="index"> Just there to name it </param>
    /// <param name="readOnly"> true to make it readonly </param>
    /// <returns> The TextBox with its values </returns>
    private static TextBox CreateTextBox(string text, int index, bool readOnly)
    {
        return new TextBox
        {
            Text = text,
            IsReadOnly = readOnly,
            Name = "OutputTypeTextBox" + (index + 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0),
            Width = Double.NaN,
        };
    }

    /// <summary>
    /// Creates a button
    /// </summary>
    /// <param name="text"> The text of the button </param>
    /// <param name="index"> Index to name the button. Example: index = 1 -> Name = Button2 </param>
    /// <returns> The Button with its values </returns>
    private static Button CreateButton(string text, int index)
    {
        return new Button
        {
            Content = text,
            Name = "Button" + (index + 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }
}

