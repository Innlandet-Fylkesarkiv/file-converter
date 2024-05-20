using System.Diagnostics;

namespace FileConverter.LinuxSpecifics
{
    static class LinuxSetup
    {
        //Specific Linux distro
        private static string LinuxDistro = GetLinuxDistro();
        private static string PathRunningProgram = "/bin/bash";

        //Map for external converters to check if they are downloaded.
        static Dictionary<List<string>, string> converterArguments = new Dictionary<List<string>, string>()
    {
        {new List<string> { "\"-c \\\" \" + \"gs -version\" + \" \\\"\"", "GPL Ghostscript",  "LinuxSpecifics\\ghostscript.txt"}, "GhostScript"},
        {new List<string>{"\"-c \\\" \" + \"libreoffice --version\" + \" \\\"\"", "LibreOffice", "LinuxSpecifics\\libreoffice.txt"}, "LibreOffice" } ,
        {new List<string>{ "\"-c \\\" \" + \"javac -version\" + \" \\\"\"", "javac", "LinuxSpecifics\\email.txt"}, "Java JRE"},
        {new List<string>{ "\"-c \\\" \" + \"java -version\" + \" \\\"\"", "openjdk", "LinuxSpecifics\\email.txt"}, "Java JDE"},
        {new List<string>{ "\"-c \\\" \" + \"msgconvert --help\" + \" \\\"\"", "msgconvert", "LinuxSpecifics\\email.txt"}, "MSGConvert"}
    };

        /// <summary>
        /// Main set-up function for Linux
        /// </summary>
        public static void Setup()
        {
            Console.WriteLine("Running on Linux");
            foreach(var converter in converterArguments){
                checkInstallConverter(converter.Key[0], converter.Key[1], converter.Key[2]);
            }
            checkInstallSiegfried();
        }

        /// <summary>
        /// Runs a process with the given filename and arguments
        /// </summary>
        /// <param name="configure"> The start info of the process </param>
        /// <returns>Either output from process or empty string</returns>
        private static string RunProcess(Action<ProcessStartInfo> configure)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            configure(startInfo);

            try
            {
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                return process.StandardOutput.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "";
            }
        }

        /// <summary>
        /// Checks if Siegfried is installed, if not, asks the user if they want to install it
        /// </summary>
        private static void checkInstallSiegfried()
        {
            //Try to start Siegfried
            string output = RunProcess(startInfo =>
            {
                startInfo.FileName = PathRunningProgram;
                startInfo.Arguments = "-c \" " + "sf -version" + " \"";
            });

            // If output does not contain Siegfried, ask user if they want to install it
            if (!output.Contains("siegfried"))
            {
                Console.WriteLine("Siegfried is not installed. In order to install Siegfried your user must have sudo privileges.");
                Console.WriteLine("For more info on Siegfried see: https://www.itforarchivists.com/siegfried/");
                Console.WriteLine("Prompt for sudo password will appear after accepting the installation process.");
                Console.WriteLine("Do you want to install it? (Y/n)");
                string? r = Console.ReadLine();
                r = r?.ToUpper() ?? " ";
                if (r == "Y")
                {
                    Console.Write("Installing Siegfried...");
                    Console.Write("This may take a while");
                    InstallSiegfried();
                }
                else
                {
                    Console.WriteLine("Siegfried is not installed. Without Siegfried the program cannot run properly. Exiting program.");
                    Environment.Exit(0);
                }
            }
        }

        /// <summary>
        /// Install siegfried based on the linux distro
        /// </summary>
        private static void InstallSiegfried()
        {
            string checkDependencies;   //Result from running terminal process to check if dependencies are installed
            string output;              //Result from running terminal process to install Siegfried
            //Download methods for different distros can be found at https://www.github.com/richardlehane/siegfried
            switch (LinuxDistro)
            {
                case "debian":
                    checkDependencies = RunProcess(startInfo =>
                    {
                        startInfo.FileName = PathRunningProgram;
                        startInfo.Arguments = "-c \" " + "curl" + " \"";
                    });

                    if (checkDependencies.Contains(""))
                    {
                        output = RunProcess(startInfo =>
                         {
                             startInfo.FileName = PathRunningProgram;
                             startInfo.Arguments = $"-c \"curl -sL 'http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x20F802FE798E6857' | gpg --dearmor | sudo tee /usr/share/keyrings/siegfried-archive-keyring.gpg && echo 'deb [signed-by=/usr/share/keyrings/siegfried-archive-keyring.gpg] https://www.itforarchivists.com/ buster main' | sudo tee -a /etc/apt/sources.list.d/siegfried.list && sudo apt-get update && sudo apt-get install siegfried\"";
                         });
                        Console.WriteLine(output);
                    }
                    else
                    {
                        Console.WriteLine("Siegfried needs curl to install properly. Please install curl and try again.");
                        Environment.Exit(0);
                    }
                    break;
                case "fedora":
                case "arch":
                    checkDependencies = RunProcess(startInfo =>
                    {
                        startInfo.FileName = PathRunningProgram;
                        startInfo.Arguments = "-c \" " + "brew help" + " \"";
                    });
                    if (checkDependencies.Contains("brew config"))
                    {
                        RunProcess(startInfo =>
                         {
                             startInfo.FileName = PathRunningProgram;
                             startInfo.Arguments = $"-c \"brew install richardlehane/digipres/siegfried  \"";
                         });
                    }
                    else
                    {
                        Console.WriteLine("Siegfried needs homebrew to install properly. Please install homebrew and try again. See https://brew.sh for information about installation.");
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        /// <summary>
        /// Checks whether the given converter is installed
        /// </summary>
        /// <param name="arguments"> CLI arguments to be run</param>
        /// <param name="expectedOutput"> Expected output from CLI arguments </param>
        /// <param name="consoleMessage"> Message to write if converter is not installed </param>
        private static void checkInstallConverter(string arguments, string expectedOutput, string consoleMessage)
        {
            string output = RunProcess(startInfo =>
            {
                startInfo.FileName = PathRunningProgram;
                startInfo.Arguments = $"{arguments} | cat {consoleMessage}";
            });
            if (!output.Contains(expectedOutput))
            {
                Console.WriteLine(output);
            }
        }

        /// <summary>
        /// Get the linux distro
        /// </summary>
        /// <returns> A string with the distro name</returns>
        private static string GetLinuxDistro()
        {
            string distro = "";
            //Check which distro the user is running
            string output = RunProcess(startInfo =>
            {
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = "-c \" " + "cat /etc/*-release" + " \"";
            });

            switch (output)
            {
                case var o when o.Contains("Ubuntu") || o.Contains("Debian"):
                    Console.WriteLine("Running on Debian based distro");
                    distro = "debian";
                    break;
                case var o when o.Contains("Fedora"):
                    Console.WriteLine("Running on Fedora based distro");
                    distro = "fedora";
                    break;
                case var o when o.Contains("Arch"):
                    Console.WriteLine("Running on Arch based distro");
                    distro = "arch";
                    break;
                default:
                    Console.WriteLine("Distro not supported. Exiting program.");
                    Environment.Exit(0);
                    break;
            }

            return distro;
        }
    }
}