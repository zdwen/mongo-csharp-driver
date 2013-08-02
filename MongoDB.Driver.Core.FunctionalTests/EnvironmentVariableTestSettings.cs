using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core
{
    public class EnvironmentVariableTestSettings : ITestSettings
    {
        private static readonly Dictionary<string, string> __settings;

        static EnvironmentVariableTestSettings()
        {
            const string prefix = "CSharpDriver-Tests-";

            __settings = Environment.GetEnvironmentVariables().OfType<DictionaryEntry>()
                .Where(x => x.Key.ToString().StartsWith(prefix))
                .ToDictionary(x => x.Key.ToString().Substring(prefix.Length), x => x.Value.ToString());
        }

        public string GetValueOrDefault(string key, string defaultValue)
        {
            string value;
            return __settings.TryGetValue(key, out value) ? value : defaultValue;
        }

        public string[] GetArrayValuesOrDefault(string key, string[] defaultValues)
        {
            var values = __settings.Where(x => x.Key.StartsWith(key)).ToList();
            if (values.Count == 0)
            {
                return defaultValues;
            }

            return values.Select(x => x.Value).ToArray();
        }
    }
}
