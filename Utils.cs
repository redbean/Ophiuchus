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

    }
}
