using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Diagnostics
{
    public class CommandResult
    {
        public string Result { get; set; } = string.Empty;     // Command output
        public string Error { get; set; } = string.Empty;      // Error message, if any
        public bool IsSuccess { get; set; }    // Whether the command was successful
        public int ExitCode { get; set; }      // Exit code of the command

        // Override ToString method for easier output display
        public override string ToString()
        {
            if (IsSuccess)
                return $"Command executed successfully. \nResult:\n{Result}";
            else
                return $"Command failed with exit code {ExitCode}. \nError:\n{Error}";
        }
    }



    public static class StringExtension
    {
        /// <summary>
        /// Executes the string as a command using the appropriate system shell (cmd for Windows, bash for Unix/Linux) asynchronously.
        /// </summary>
        /// <param name="command">The full command to execute, including arguments if any.</param>
        /// <returns>A Task that represents the asynchronous operation. The result is a CommandResult object containing the result, error message, success status, and exit code.</returns>
        public static async Task<CommandResult> ExecuteCommandAsync(this string command)
        {
            CommandResult result = new CommandResult();

            try
            {
                ProcessStartInfo processStartInfo;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows uses cmd.exe to execute the command
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}", // /C executes the command and terminates
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Linux and macOS (Unix-based) use bash to execute the command
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"", // -c executes the command and terminates
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    throw new PlatformNotSupportedException("This operating system is not supported.");
                }

                // Start the process
                using (Process process = Process.Start(processStartInfo))
                {
                    // Capture the output and error messages asynchronously
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for both output and error to be captured
                    await Task.WhenAll(outputTask, errorTask);

                    // Get the result
                    result.Result = await outputTask;
                    result.Error = await errorTask;

                    // Wait for the process to exit asynchronously
                    await Task.Run(() => process.WaitForExit());

                    // Set exit code and success status
                    result.ExitCode = process.ExitCode;
                    result.IsSuccess = process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                // Handle any exception and mark the command as failed
                result.Error = $"Command execution failed: {ex.Message}";
                result.IsSuccess = false;
                result.ExitCode = -1; // Set a custom exit code to indicate failure
            }

            return result;
        }
    }
}
