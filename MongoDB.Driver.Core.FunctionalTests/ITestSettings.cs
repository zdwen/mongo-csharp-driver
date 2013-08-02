using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core
{
    public interface ITestSettings
    {
        string GetValueOrDefault(string key, string defaultValue);

        string[] GetArrayValuesOrDefault(string key, string[] defaultValues);
    }
}
