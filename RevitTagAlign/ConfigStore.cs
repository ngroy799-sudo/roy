using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace RevitTagAlign
{
    /// <summary>
    /// Persists Configure dialog settings under:
    ///   C:\Users\&lt;you&gt;\AppData\Roaming\RevitTagAlign\AlignConfig.xml
    /// (%AppData% = Roaming, not Local)
    /// </summary>
    public static class ConfigStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AlignConfig));

        public static string LastError { get; private set; }

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

        /// <summary>Create folder + default XML immediately so the path always exists.</summary>
        public static AlignConfig LoadOrCreate()
        {
            AlignConfig cfg = Load();
            if (!File.Exists(SettingsPath))
                Save(cfg);
            return cfg;
        }

        public static AlignConfig Load()
        {
            LastError = null;
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                string path = SettingsPath;
                if (!File.Exists(path))
                    return new AlignConfig();

                using (var fs = File.OpenRead(path))
                {
                    var cfg = Serializer.Deserialize(fs) as AlignConfig;
                    return cfg ?? new AlignConfig();
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return new AlignConfig();
            }
        }

        public static bool Save(AlignConfig config)
        {
            LastError = null;
            if (config == null)
            {
                LastError = "Config is null.";
                return false;
            }

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
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public static void OpenSettingsFolder()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                if (!File.Exists(SettingsPath))
                    Save(new AlignConfig());

                Process.Start(new ProcessStartInfo
                {
                    FileName = SettingsDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }
    }
}
