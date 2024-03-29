using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
    }
}

