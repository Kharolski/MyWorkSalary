using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorkSalary.Helpers.Localization
{
    public static class LocalizationHelper
    {
        // key = namn på strängen i resx
        public static string Translate(string key) =>
            Resources.Resx.Resources.ResourceManager.GetString(key) ?? key;
    }
}
