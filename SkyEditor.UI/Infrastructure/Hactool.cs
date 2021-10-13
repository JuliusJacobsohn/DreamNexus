﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyEditor.UI.Infrastructure
{
    public static class Hactool
    {
        private static readonly object logFileLock = new();

        private static void WriteToLogFile(string line)
        {
            lock(logFileLock)
            {
                File.AppendAllLines("hactool.log", new[] { line });
            }
        }

        public static string RunHactool(params string[] args)
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "hactool.exe" : "hactool",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            foreach (var arg in args)
            {
                proc.StartInfo.ArgumentList.Add(arg);
            }

            var output = new StringBuilder();
            var error = new StringBuilder();
            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                output.AppendLine(e.Data);
                Console.WriteLine($"[hactool stdout {proc.Id}] {e.Data}");
                WriteToLogFile($"[hactool stdout {proc.Id}] {e.Data}");
            };
            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                error.AppendLine(e.Data);
                Console.WriteLine($"[hactool stderr {proc.Id}] {e.Data}");
                WriteToLogFile($"[hactool stderr {proc.Id}] {e.Data}");
            };

            Console.WriteLine($"Running hactool with '{string.Join(" ", args)}'");

            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                try
                {
                    WriteToLogFile("Encountered exception running hactool: " + ex.ToString());
                }
                catch (Exception)
                {
                    // Bubble up the original exception, we don't want errors about logging interfering with errors from hactool
                }

                throw;
            }

            return output.ToString();
        }
    }
}