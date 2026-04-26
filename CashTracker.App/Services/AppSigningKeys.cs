using System;
using System.IO;

namespace CashTracker.App.Services
{
    internal static class AppSigningKeys
    {
        private const string DefaultLicensePublicKeyXml =
            "<RSAKeyValue><Modulus>526JV46QA+8OJIS6jF9boltTQvdZxX3sGH4clNgHxtNoaIWsC/QIqamRn/S8igwl3VuZH7duNnp4ymif6nKdicOSpedt6jakto3k3JnkFyTVuIEoqWPNRc/Ixd4cLH+CQ+Fl9+Dz3Owb+RQGKNapv1zC3fo4hDwupXdBfqi4R0g2vFUfSqrTmjbgyT7OJzicDDCnhfxbeXSbxUe8T3xNBq/rvAZinIvlHf45rGC2EbD2wYER2C2p60mbMa3m5rWhdK05hq5nFHIfouyZfWVh/j2SD4lLwQdv4yyyHH6h0IcSRmKUpCrJuaRw/actIP4ZbkwZBQUXiG/m3xKlKPb/0Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string GetLicensePublicKeyXml(string? appDataPath = null)
        {
            return ResolveKey(
                "CASHTRACKER_LICENSE_PUBLIC_KEY_XML",
                "license-public-key.xml",
                appDataPath,
                DefaultLicensePublicKeyXml);
        }

        private static string ResolveKey(
            string envVarName,
            string fileName,
            string? appDataPath,
            string fallback)
        {
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            foreach (var candidate in GetCandidatePaths(fileName, appDataPath))
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        var fileValue = File.ReadAllText(candidate).Trim();
                        if (!string.IsNullOrWhiteSpace(fileValue))
                            return fileValue;
                    }
                }
                catch
                {
                    // Fall back to the embedded key if the override file cannot be read.
                }
            }

            return fallback;
        }

        private static string[] GetCandidatePaths(string fileName, string? appDataPath)
        {
            return new[]
            {
                !string.IsNullOrWhiteSpace(appDataPath) ? Path.Combine(appDataPath, fileName) : string.Empty,
                Path.Combine(AppContext.BaseDirectory, "AppData", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName)
            };
        }
    }
}
