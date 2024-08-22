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
        
        public static string CMD_CREATE_ENV = "create -n";
        public static string ARG_CREATE_ENV_PYTHON = "python=";
        public static string ARG_FORCE_Y = "-y";

        public static string CMD_REMOVE_ENV = "env remove -n";

        public static string RESP_ERROR = "error";

        public static string GetCreateCmd(string env_name, string python_ver)
        {
            //{ ARG_CREATE_ENV_PYTHON}
            //{ python_ver}
            return $"{CMD_CREATE_ENV} {env_name} {ARG_FORCE_Y}";
        }

        public static string GetRemoveCmd(string env_name)
        {
            return $"{CMD_REMOVE_ENV} {env_name}";
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


}
