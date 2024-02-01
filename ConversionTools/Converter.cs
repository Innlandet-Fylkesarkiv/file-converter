﻿using iText.Forms.Form.Element;
using System;

/// <summary>
/// Parent class for all converters
/// </summary>
public class Converter
{
	public string? Name { get; set; } // Name of the converter
	public string? Version { get; set; } // Version of the converter
	public Dictionary<string, List<string>>? SupportedConversions { get; set; }

	public Converter()
	{ }



    public virtual Dictionary<string, List<string>>? listOfSupportedConversions()
	{
		return new Dictionary<string, List<string>>();
	}

    /// <summary>
    /// Convert a file to a new format
    /// </summary>
    /// <param name="fileinfo">The file to be converted</param>
    /// <param name="pronom">The file format to convert to</param>
    public virtual void ConvertFile(string fileinfo, string pronom)
	{ }
	
	/// <summary>
	/// Combine multiple files into one file
	/// </summary>
	/// <param name="files">Array of files that should be combined</param>
	/// <param name="pronom">The file format to convert to</param>
	public virtual void CombineFiles(string []files, string pronom)
	{ }

	/// <summary>
	/// Delete an original file, that has been converted, from the output directory
	/// </summary>
	/// <param name="fileInfo">The specific file to be deleted</param>
	public virtual void deleteOriginalFileFromOutputDirectory(string fileInfo) {
		if (File.Exists(fileInfo))
		{
            File.Delete(fileInfo);
        }
	}

	//TODO: Implement base methods
	//TODO: Try again if files fail to convert
}