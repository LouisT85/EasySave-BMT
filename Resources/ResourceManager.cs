using System;
using System.Globalization;
using System.Resources;

namespace easySave_BMT.Resources_
{
    /// <summary>
    /// Provides a simple, centralized way to access localized strings
    /// from the embedded .resx resource files (<c>Strings.resx</c>, <c>Strings.fr.resx</c>, etc.).
    /// </summary>
    public static class ResourceManager
    {
        /// <summary>
        /// Underlying .NET <see cref="System.Resources.ResourceManager"/> used
        /// to resolve string resources by key.
        /// </summary>
        private static System.Resources.ResourceManager resourceManager;

        /// <summary>
        /// Culture currently used to resolve resource strings (e.g. "en", "fr").
        /// </summary>
        private static CultureInfo currentCulture;

        /// <summary>
        /// Static constructor that initializes the resource manager with the
        /// base name of the resources file and sets English ("en") as the
        /// default language.
        /// </summary>
        static ResourceManager()
        {
            resourceManager = new System.Resources.ResourceManager("easySave_BMT.Resources.Strings", typeof(ResourceManager).Assembly);
            currentCulture = new CultureInfo("en");
        }

        /// <summary>
        /// Changes the current UI language used when resolving resource strings.
        /// </summary>
        /// <param name="language">
        /// A culture name such as "en" or "fr". If the culture is invalid or
        /// not installed on the system, the method falls back to English.
        /// </param>
        public static void SetLanguage(string language)
        {
            try
            {
                currentCulture = new CultureInfo(language);
            }
            catch
            {
                // Fallback to English if the requested language is invalid or unavailable.
                currentCulture = new CultureInfo("en");
            }
        }

        /// <summary>
        /// Retrieves the localized string associated with the specified key
        /// using the current culture.
        /// </summary>
        /// <param name="key">The name of the resource string to retrieve.</param>
        /// <returns>
        /// The localized string if it exists; otherwise the key itself, so that
        /// missing resources are still visible during development.
        /// </returns>
        public static string GetString(string key)
        {
            try
            {
                string value = resourceManager.GetString(key, currentCulture);
                return value ?? key;
            }
            catch
            {
                // In case of any error (e.g. missing resource), return the key as a fallback.
                return key;
            }
        }

        /// <summary>
        /// Gets the name of the culture currently used for resource lookups.
        /// </summary>
        /// <returns>The culture name (e.g. "en", "fr").</returns>
        public static string GetCurrentLanguage()
        {
            return currentCulture.Name;
        }
    }
}
