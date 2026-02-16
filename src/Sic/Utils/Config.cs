using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Windows.Forms;
using SharpConfig;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Utils;

public class Config
{
    private static readonly string ConfigFileName = Path.Combine(App.DataFolder, String.Format("{0}.{1}", App.Name, App.ConfigFileExtension));

    public static Config.SectionGeneral General = new();
    public static Configuration Cfg = new();

    #region Config Section Classes
    public class SectionGeneral
    {
        public string Language { get; set; } = "System";
        public bool BrailleUiMode { get; set; } = false;
        public string OutputFolder { get; set; } = "";
    }

    #endregion

    public static void Load()
    {
        try
        {
            Cfg = Configuration.LoadFromFile(ConfigFileName);
            General = Cfg["General"].ToObject<SectionGeneral>();
        }
        catch (FileNotFoundException)
        {
            Cfg = new Configuration();
            General = new Config.SectionGeneral();
            Cfg.Add(Section.FromObject("General", General));

            try
            {
                if (!Directory.Exists(App.DataFolder))
                {
                    Directory.CreateDirectory(App.DataFolder);
                }
                Cfg.SaveToFile(ConfigFileName);
            }
            catch (Exception)
            {
                DialogResult msg = MessageBox.Show("Unable to save configuration. Please contact the developer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                if (msg == DialogResult.OK)
                {
                    System.Windows.Forms.Application.Exit();
                }
            }
        }
        catch (Exception)
        {
            DialogResult msg = MessageBox.Show("Unable to load configuration. Please contact the developer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (msg == DialogResult.OK)
            {
                System.Windows.Forms.Application.Exit();
            }
        }
    }

    public static void Save()
    {
        Cfg = new Configuration();
        Cfg.Add(Section.FromObject("General", Config.General));

        try
        {
            Cfg.SaveToFile(ConfigFileName);
        }
        catch (Exception)
        {
            DialogResult msg = MessageBox.Show("Unable to save configuration. Please contact the developer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (msg == DialogResult.OK)
            {
                System.Windows.Forms.Application.Exit();
            }
        }
    }
}
