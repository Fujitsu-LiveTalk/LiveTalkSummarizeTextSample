using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveTalkSummarizeTextSample.Common
{
    internal static class Config
    {
        private static Configuration ConfigManager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        internal static void SetConfig(string name, string value)
        {
            ConfigManager.AppSettings.Settings[name].Value = value;
            ConfigManager.Save();
        }

        internal static string GetConfig(string name)
        {
            return ConfigManager.AppSettings.Settings[name].Value;
        }
    }
}
