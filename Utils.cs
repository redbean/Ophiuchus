using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Ophiuchus
{
    public static class Utils
    {
        public static Process RunCondaCmd(string cmd, params string[] args)
        {
            string arguments = $"{cmd} {string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = VARS.CMD_CONDA,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            //var startInfo = new ProcessStartInfo
            //{
            //    FileName = "cmd.exe",
            //    Arguments = $"/c {VARS.CMD} {arguments} && echo. && echo Press any key to continue... && pause > nul",
            //    UseShellExecute = false,
            //    RedirectStandardOutput = true,
            //    RedirectStandardError = true,
            //    CreateNoWindow = false  // 콘솔 창을 표시합니다.
            //};
            return proc;
        }

        public static async Task RunCommandWithVisibleCmd(string command, params string[] args)
        {
            string arguments = $"{command} {string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";

            using (var process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C {VARS.CMD_CONDA} {arguments} || pause";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                process.Start();
                await Task.Run(() => process.WaitForExit());
            }
        }

        public static async Task RunCommandWithOutput(string command, string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // 여기서 UI에 출력을 표시합니다.
                        // 예: textBox.AppendText(e.Data + Environment.NewLine);
                        Console.WriteLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // 여기서 UI에 에러를 표시합니다.
                        // 예: textBox.AppendText("Error: " + e.Data + Environment.NewLine);
                        Console.WriteLine("Error: " + e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());
            }
        }
    }
}
