using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Utils
{
    internal static class PssaModule
    {
        private static readonly Lazy<string> s_locationLazy = new Lazy<string>(GetThisModuleRootPath);

        public static string Name { get; } = "PSScriptAnalyzer";

        public static string RootPath => s_locationLazy.Value;

        private static string GetThisModuleRootPath()
        {
            string path = Assembly.GetExecutingAssembly().Location;

            while (true)
            {
                path = Path.GetDirectoryName(path);

                if (string.IsNullOrEmpty(path))
                {
                    throw new FileNotFoundException($"Cannot find the module root from path '{Assembly.GetExecutingAssembly().Location}'");
                }

                if (!path.EndsWith(PssaModule.Name))
                {
                    continue;
                }

                if (File.Exists(Path.Combine(path, $"{PssaModule.Name}.psd1")))
                {
                    return path;
                }
            }
        }
    }
}
