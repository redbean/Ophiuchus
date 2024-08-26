using Ophiuchus;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel;

internal static class CondaServiceHelpers
{

    //TODO 다른 클래스로 빼기
    public static (bool isSuccess, OphiuchusError error) ReadYamlFile(string path, ref string envName)
    {
        List<string> requiredKeys = new List<string> { "name", "channels", "dependencies" };
        try
        {
            using (var reader = new StreamReader(path))
            {
                var yaml = new YamlStream();
                yaml.Load(reader);

                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                var missingKeys = new List<string>();
                foreach (var key in requiredKeys)
                {
                    if (!mapping.Children.ContainsKey(key))
                    {
                        missingKeys.Add(key);
                    }
                }
                if (missingKeys.Count > 0)
                {
                    return (false, OphiuchusError.YAMLMissingKeyError);
                }
                if (mapping.Children.TryGetValue("name", out var nameNode) && nameNode is YamlScalarNode scalarNode)
                {
                    envName = scalarNode?.Value ?? string.Empty;
                }
                return (true, OphiuchusError.Success);
            }
        }
        catch (FileNotFoundException)
        {
            return (false, OphiuchusError.FileNotFoundError);
        }
        catch (YamlDotNet.Core.YamlException e)
        {
            return (false, OphiuchusError.YAMLParseError);
        }
        catch (Exception e)
        {
            return (false, OphiuchusError.ExceptionError);
        }
    }

    //TODO 다른 클래스로 빼기
    public static async Task<string> ReadDependenciesAsync(string env)
    {
        try
        {
            var output = await RunCommandOutput(VARS.GetDependancy(env));
            return output;
        }
        catch
        {
            return VARS.RESP_ERROR;
        }
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

    public static async Task<string> RunCommandOutput(string command, params string[] args)
    {
        string arguments = $"{command} {string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
        using (var process = new Process())
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {VARS.CMD_CONDA} {arguments}";
            process.StartInfo.UseShellExecute = false; 
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }
    }

    public static OphiuchusError OpenNewCmdWithActivation(string condaPath, string environmentName)
    {
        try
        {
            string activatePath = Path.Combine(condaPath, "Scripts", "activate.bat");
            
            if (!File.Exists(activatePath))
            {
                return OphiuchusError.CondaActivateNotFoundError;
            }


            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K call \"{activatePath}\" && conda activate {environmentName}",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            process.StartInfo = startInfo;
            process.Start();
            return OphiuchusError.Success;
        }
        catch (Exception ex)
        {
            return OphiuchusError.ExceptionError;
        }
    }

    public static bool IsAnacondaInstalled()
    {
        return Directory.Exists(VARS.PATH_CONDA_DEFAULTPATH);
    }
    
}