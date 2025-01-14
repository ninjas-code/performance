﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ScenarioMeasurement
{
    public class ProcessHelper
    {
        /// <summary>
        /// Specifies whether the app exits on its own
        /// true: App will exit, Timeout specifies how long to wait before terminating the process.
        /// false: App will be closed after MeasurementDelay.
        /// </summary>
        public bool ProcessWillExit { get; set; } = false;
        /// <summary>
        /// Max time to wait for app to close (seconds)
        /// </summary>
        public int Timeout { get; set; } = 60;
        /// <summary>
        /// Time to wait before closing app (seconds)
        /// </summary>
        public int MeasurementDelay { get; set; } = 15;

        public string Executable { get; set; }

        public string Arguments { get; set; } = String.Empty;
        public string WorkingDirectory { get; set; } = String.Empty;

        public bool GuiApp { get; set; } = false;

        /// <summary>
        /// Runs the specified process and waits for it to exit.
        /// </summary>
        /// <param name="appExe">Full path to executable</param>
        /// <param name="appArgs">Optional arguments</param>
        /// <param name="workingDirectory">Optional working directory (defaults to current directory)</param>
        /// <returns></returns>
        public Result Run()
        {
            var psi = new ProcessStartInfo();
            psi.FileName = Executable;
            psi.Arguments = Arguments;
            psi.WorkingDirectory = WorkingDirectory;
            psi.UseShellExecute = GuiApp;
            if (!GuiApp)
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
            }
            else
            {
                psi.WindowStyle = ProcessWindowStyle.Maximized;
            }
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            using (var process = new Process())
            {
                process.StartInfo = psi;
                if (!GuiApp)
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        error.AppendLine(e.Data);
                    };
                }
                process.Start();
                if (!GuiApp)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                if (ProcessWillExit)
                {
                    bool exited = process.WaitForExit(Timeout * 1000);
                    if (!exited)
                    {
                        process.Kill();
                        return Result.TimeoutExceeded;
                    }
                }
                else
                {
                    Thread.Sleep(MeasurementDelay * 1000);
                    if (!process.HasExited) { process.CloseMainWindow(); }
                    else
                    {
                        return Result.ExitedEarly;
                    }
                    bool exited = process.WaitForExit(5000);
                    if (!exited)
                    {
                        process.Kill();
                        return Result.CloseFailed;
                    }
                }
                using (var sw = new StreamWriter($"testlog_{Path.GetFileName(Executable)}_{process.Id}.log"))
                {
                    sw.WriteLine("Standard output:");
                    sw.WriteLine(output.ToString());
                    sw.WriteLine("Standard error:");
                    sw.WriteLine(error.ToString());
                }
                return Result.Success;
            }
        }
        public enum Result
        {
            Success,
            TimeoutExceeded,
            ExitedEarly,
            CloseFailed
        }
    }
}
