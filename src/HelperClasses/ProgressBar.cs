using System;
using System.Text;
using System.Threading;
	
/// <summary>
/// An ASCII progress bar for the console.
/// It is based of the code from https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
/// </summary>
/// 
namespace FileConverter.HelperClasses
{
	public class ProgressBar : IDisposable
	{
		private const int blockCount = 10;
		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string animation = @"|/-\";

		private readonly Timer timer;
        private readonly object lockObject = new object();
        private readonly object disposeLock = new object();

        private double currentProgress = 0;
		private int currentDone = 0;
		private TimeSpan elapsed = TimeSpan.FromSeconds(0);
		private string currentText = string.Empty;
		private bool disposed = false;
		private int animationIndex = 0;
		private readonly int totalJobs = 0;
		private int percentTenth = 0;

		/// <summary>
		/// Creates a new progress bar with the total number of items to process.
		/// </summary>
		/// <param name="totalItems"> total number of items </param>
		public ProgressBar(int totalItems)
		{
			timer = new Timer(TimerHandler!);
            totalJobs = totalItems;
			// A progress bar is only for temporary display in a console window.
			// If the console output is redirected to a file, draw nothing.
			// Otherwise, we'll end up with a lot of garbage in the target file.
			if (!Console.IsOutputRedirected)
			{
				ResetTimer();
			}
		}

		/// <summary>
		/// Reports the progress
		/// </summary>
		/// <param name="value"> percenteage done in decimals (0.75 = 75% done)</param>
		/// <param name="currentJob"> the task number it is on</param>
		/// <param name="ts"> how much time has passed since start </param>
        public void Report(double value, int currentJob, TimeSpan ts)
		{
			// Makes sure value is in [0..1] range
			value = Math.Max(0, Math.Min(1, value));
			Interlocked.Exchange(ref currentProgress, value); // atomic operation
			Interlocked.Exchange(ref currentDone, currentJob); // atomic operation
			elapsed = ts;
		}

        /// <summary>
        /// Used as a callback for the timer
        /// </summary>
        /// <param name="state"> unused, but necessary to be used as callback </param>
        private void TimerHandler(object state)
		{
			lock (lockObject)
			{
				if (disposed) return;
				
				int progressBlockCount = (int)(currentProgress * blockCount);
				int percent = (int)(currentProgress * 100);
                Logger logger = Logger.Instance;
                if (percent / 10 != percentTenth/10)
				{
					// Only logs every 10%. This works because of integer division.
					percentTenth = percent / 10 * 10;
                    logger.SetUpRunTimeLogMessage("Conversion progress: " + percentTenth + "% done.", false);
                }

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

		/// <summary>
		/// Updates the text in the console
		/// </summary>
		/// <param name="text"> the new text </param>
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
            outputBuilder.Append(text.AsSpan(commonPrefixLength));

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

		/// <summary>
		/// Resets the timer
		/// </summary>
		private void ResetTimer()
		{
			timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
		}

		/// <summary>
		/// Disposes the progress bar
		/// </summary>
        public void Dispose()
        {
            lock (disposeLock) // Use a separate object for locking
            {
                if (!disposed)
                {
                    disposed = true;
                    UpdateText(string.Empty);
                }
            }
            GC.SuppressFinalize(this); // Suppress the finalization
        }
    }
}