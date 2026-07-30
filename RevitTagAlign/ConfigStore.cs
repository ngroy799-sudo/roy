using System;
using System.IO;
using System.Xml.Serialization;

namespace RevitTagAlign
{
    /// <summary>
    /// Persists Configure dialog settings under %AppData%\RevitTagAlign\AlignConfig.xml
    /// so values survive Revit restarts.
    /// </summary>
    public static class ConfigStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AlignConfig));

        public static string SettingsDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevitTagAlign");
            }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(SettingsDirectory, "AlignConfig.xml"); }
        }

        public static AlignConfig Load()
        {
            try
            {
                string path = SettingsPath;
                if (!File.Exists(path))
                    return new AlignConfig();

                using (var fs = File.OpenRead(path))
                {
                    var cfg = Serializer.Deserialize(fs) as AlignConfig;
                    return cfg ?? new AlignConfig();
                }
            }
            catch
            {
                return new AlignConfig();
            }
        }

        public static void Save(AlignConfig config)
        {
            if (config == null)
                return;

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                string path = SettingsPath;
                string temp = path + ".tmp";

                using (var fs = File.Create(temp))
                {
                    Serializer.Serialize(fs, config);
                }

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
            }
            catch
            {
                // Non-fatal: alignment still works without persistence.
            }
        }
    }
}
