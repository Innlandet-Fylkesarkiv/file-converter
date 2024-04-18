using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;

public static class CreateControl
{
    /// <summary>
    /// Creates a TextBlock
    /// </summary>
    /// <param name="text"> The textcontent of the TextBlock </param>
    /// <returns> The Textblock that was created </returns>
    public static TextBlock CreateTextBlock(string text)
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
    public static ComboBox CreateComboBox(int index)
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
    public static TextBox CreateTextBox(string text, int index, bool readOnly)
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
    public static Button CreateButton(string text, int index)
    {
        return new Button
        {
            Content = text,
            Name = "Button" + (index + 1),
            Background = Avalonia.Media.Brushes.Green,
            Foreground = Avalonia.Media.Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }

    /// <summary>
    /// Creates a white separator
    /// </summary>
    /// <returns> a rectangle used as a separator </returns>
    public static Rectangle CreateSeparator()
    {
        return new Rectangle
        {
            Fill = Avalonia.Media.Brushes.SlateGray,
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }
    
    /// <summary>
    /// Creats a grid for the folder override
    /// </summary>
    /// <param name="index"> just to name them </param>
    /// <returns> The Grid </returns>
    public static Grid CreateFolderOverrideGrids(int index)
    {
        return new Grid
        {
            Name = "FolderOverrideGrid" + index,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
        };
    }

    /// <summary>
    /// Creates a CheckBox
    /// </summary>
    /// <param name="text"> title above the textbox </param>
    /// <returns></returns>
    public static CheckBox CreateCheckBox(string text)
    {
        return new CheckBox
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }
}