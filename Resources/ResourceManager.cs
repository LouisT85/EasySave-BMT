using System;
using System.Globalization;
using System.Resources;

namespace easySave_BMT.Resources_
{
    public static class ResourceManager
    {
        private static System.Resources.ResourceManager resourceManager;
        private static CultureInfo currentCulture;

        static ResourceManager()
        {
            resourceManager = new System.Resources.ResourceManager("easySave_BMT.Resources.Strings", typeof(ResourceManager).Assembly);
            currentCulture = new CultureInfo("en");
        }

        public static void SetLanguage(string language)
        {
            try
            {
                currentCulture = new CultureInfo(language);
            }
            catch
            {
                currentCulture = new CultureInfo("en");
            }
        }

        public static string GetString(string key)
        {
            try
            {
                string value = resourceManager.GetString(key, currentCulture);
                return value ?? key;
            }
            catch
            {
                return key;
            }
        }

        public static string GetCurrentLanguage()
        {
            return currentCulture.Name;
        }
    }
}
