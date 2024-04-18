﻿using System;
using System.Text;
using System.Threading;

/// <summary>
/// An ASCII progress bar
/// </summary>
/// 
namespace HelperClasses.ProgressBar
{
	public class ProgressBar : IDisposable, IProgress<double>
	{
		private const int blockCount = 10;
		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string animation = @"|/-\";

		private readonly Timer timer;

		private double currentProgress = 0;
		private int currentDone = 0;
		private TimeSpan elapsed = TimeSpan.FromSeconds(0);
		private string currentText = string.Empty;
		private bool disposed = false;
		private int animationIndex = 0;
		private int totalJobs = 0;
		public ProgressBar(int totalItems)
		{
			timer = new Timer(TimerHandler!);
			this.totalJobs = totalItems;
			// A progress bar is only for temporary display in a console window.
			// If the console output is redirected to a file, draw nothing.
			// Otherwise, we'll end up with a lot of garbage in the target file.
			if (!Console.IsOutputRedirected)
			{
				ResetTimer();
			}
		}

		public void Report(double value) { }
		public void Report(double value, int currentJob, TimeSpan ts)
		{
			// Make sure value is in [0..1] range
			value = Math.Max(0, Math.Min(1, value));
			Interlocked.Exchange(ref currentProgress, value); // atomic operation
			Interlocked.Exchange(ref currentDone, currentJob); // atomic operation
			elapsed = ts;
		}

		private void TimerHandler(object state)
		{
			lock (timer)
			{
				if (disposed) return;

				int progressBlockCount = (int)(currentProgress * blockCount);
				int percent = (int)(currentProgress * 100);
				string progressBar = string.Format("[{0}{1}] {2,3}% {3} {4}/{5} jobs",
				new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
				percent,
				animation[animationIndex++ % animation.Length],
				currentDone, totalJobs);
				string elapsedTime = elapsed.ToString(@"hh\:mm\:ss");
				bool showEstimatedTime = currentDone < totalJobs && percent >= 25;
				string estimatedTimeLeft;
				string displayText;

				if (showEstimatedTime)
				{
					estimatedTimeLeft = TimeSpan.FromSeconds((elapsed.TotalSeconds / currentDone) * (totalJobs - currentDone)).ToString(@"hh\:mm\:ss");
					displayText = $"{progressBar} | Elapsed: {elapsedTime} | Estimated remaining time: {estimatedTimeLeft}";
				}
				else
				{
					displayText = $"{progressBar} | Elapsed: {elapsedTime}";
				}

				UpdateText(displayText);
				ResetTimer();
			}
		}

		private void UpdateText(string text)
		{
			// Get length of common portion
			int commonPrefixLength = 0;
			int commonLength = Math.Min(currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
			{
				commonPrefixLength++;
			}

			// Backtrack to the first differing character
			StringBuilder outputBuilder = new StringBuilder();
			outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

			// Output new suffix
			outputBuilder.Append(text.Substring(commonPrefixLength));

			// If the new text is shorter than the old one: delete overlapping characters
			int overlapCount = currentText.Length - text.Length;
			if (overlapCount > 0)
			{
				outputBuilder.Append(' ', overlapCount);
				outputBuilder.Append('\b', overlapCount);
			}

			Console.Write(outputBuilder);
			currentText = text;
		}

		private void ResetTimer()
		{
			timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
		}

		public void Dispose()
		{
			lock (timer)
			{
				disposed = true;
				UpdateText(string.Empty);
			}
		}
	}
}