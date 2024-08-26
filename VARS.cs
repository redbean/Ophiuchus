using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ophiuchus
{
    class VARS
    {
        public static string CMD_CONDA = "conda";
        public static string CMD_ENV = "env list";
        public static string CMD_DEP = "list -n";
        public static string CMD_EXPORT = "env export -n";

        //conda env create -n myproject --file custom_env.yaml
        //conda env export -n 환경이름 > environment.yaml
        public static string CMD_CREATE_ENV = "create -n";
        public static string ARG_CREATE_ENV_PYTHON = "python=";
        public static string ARG_FORCE_Y = "-y";

        public static string CMD_REMOVE_ENV = "env remove -n";

        public static string RESP_ERROR = "error";



        public static string GetCreateCmd(string env_name, string python_ver)
        {
            return $"{CMD_CREATE_ENV} {env_name} {ARG_CREATE_ENV_PYTHON}{python_ver} {ARG_FORCE_Y}";
        }

        public static string GetRemoveCmd(string env_name)
        {
            return $"{CMD_REMOVE_ENV} {env_name}";
        }

        public static string GetExportCmd(string env_name)
        {
            return $"{CMD_EXPORT} {env_name} > environment_{env_name}.yaml";
        }
        public static string GetImportCmd(string env_name, string yamlFilePath)
        {
            return $"env {CMD_CREATE_ENV} {env_name} --file {yamlFilePath} {ARG_FORCE_Y}";
            //conda env create -n 환경 --file 야믈 -y
        }

    }
    class Deps
    {
        public string dep { get; set; }
        public string version { get; set; }
        public string source { get; set; }

        public Deps(string dep, string version, string source)
        {
            this.dep = dep;
            this.version = version;
            this.source = source;
        }
    }
    enum OphiuchusYAMLError
    {
        Success = 0,
        FileNotFoundError = 1,
        YAMLMissingKeyError = 2,
        YAMLParseError = 3,
        ExceptionError = 4
    }

}
