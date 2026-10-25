using System;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

using UnityEngine;

namespace ChangelogEditor
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ChangelogEditorPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ChangelogEditor";
        internal const string ModVersion = "1.2.1";
        internal const string Author = "WackyMole";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ContentFile = ModGUID + ".txt";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private static string ContentFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ContentFile;

        internal static string ConnectionError = "";
        internal static string customFileText = "";

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource ChangelogEditorLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            shouldShowChangelog = config("1 - Changelog", "Should Show Changelog", Toggle.On, "If on, the original changelog button will be shown in the main menu and opens the independent bottom-left changelog window. If off, the button will be hidden.");
            forceShowChangelog = config("1 - Changelog", "Force Show Changelog", Toggle.Off, "If on, automatically opens the changelog at startup and whenever you return to the main menu, even if the changelog button is hidden. The changelog can still be closed normally. Changes apply the next time you return to the main menu.");
            shouldChangeText = config("1 - Changelog", "Should Change Text", Toggle.On, $"If on, your configuration file's text will be added to the changelog. (at the top). This pulls from the {ContentFile} found in your config folder.");
            overrideText = config("1 - Changelog", "Override Changelog Text", Toggle.Off, "If on, only your custom text that is set will show in the changlog. This deletes the default changelog text.");
            topicText = TextEntryConfig("1 - Changelog", "Title Text", "Changelog", "Change the title text of the changelog. This is the text that shows up in the top of the changelog.");
            changelogWidth = config("1 - Changelog", "Width", 445f, "Width of the changelog window.");
            shouldChangeText.SettingChanged += UpdateChangelogContent;
            overrideText.SettingChanged += UpdateChangelogContent;


            // Create the content file if not exist
            if (!File.Exists(ContentFileFullPath))
            {
                File.WriteAllText(ContentFileFullPath, $"This is the content file for ChangelogEditor. You can edit this file to change the changelog text.{Environment.NewLine}This file can be found currently at: {ContentFileFullPath}{Environment.NewLine}{Environment.NewLine}");
            }
            customFileText = File.ReadAllText(ContentFileFullPath, Encoding.UTF8);

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
            shouldChangeText.SettingChanged -= UpdateChangelogContent;
            overrideText.SettingChanged -= UpdateChangelogContent;
            if (ChangelogWindow.Instance != null) Destroy(ChangelogWindow.Instance);
            _harmony.UnpatchSelf();
        }

        internal static void UpdateChangelogContent(object sender = null, EventArgs e = null)
        {
            if (ChangelogWindow.Instance != null) ChangelogWindow.Instance.RefreshContent();
        }


        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;

            FileSystemWatcher contentwatcher = new(Paths.ConfigPath, ConfigFileName);
            contentwatcher.Changed += ReadContent;
            contentwatcher.Created += ReadContent;
            contentwatcher.Renamed += ReadContent;
            contentwatcher.IncludeSubdirectories = true;
            contentwatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            contentwatcher.EnableRaisingEvents = true;

            FileSystemWatcher contentwatcher2 = new(Paths.ConfigPath, ContentFile);
            contentwatcher2.Changed += ReadContent;
            contentwatcher2.Created += ReadContent;
            contentwatcher2.Renamed += ReadContent;
            contentwatcher2.IncludeSubdirectories = true;
            contentwatcher2.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            contentwatcher2.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ChangelogEditorLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch (Exception ex)
            {
                ChangelogEditorLogger.LogWarning($"There was an issue reloading {ConfigFileFullPath}: {ex}");
            }
        }

        private void ReadContent(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ContentFileFullPath)) return;
            try
            {
                // Read the content file into a string that can be used later. Include UTF-8 formatting
                customFileText = File.ReadAllText(ContentFileFullPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ChangelogEditorLogger.LogWarning($"There was an issue reading {ContentFileFullPath}: {ex}");
                return;
            }

            try
            {
                UpdateChangelogContent();
            }
            catch (Exception ex)
            {
                ChangelogEditorLogger.LogWarning($"Loaded {ContentFileFullPath}, but there was an issue updating the changelog UI: {ex}");
            }
        }


        #region ConfigOptions

        //private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        internal static ConfigEntry<Toggle> shouldShowChangelog = null!;
        internal static ConfigEntry<Toggle> forceShowChangelog = null!;
        internal static ConfigEntry<Toggle> shouldChangeText = null!;
        internal static ConfigEntry<Toggle> overrideText = null!;
        internal static ConfigEntry<string> topicText = null!;
        internal static ConfigEntry<float> changelogWidth = null!;
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            //var configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        internal ConfigEntry<T> TextEntryConfig<T>(string group, string name, T value, string desc)
        {
            ConfigurationManagerAttributes attributes = new()
            {
                CustomDrawer = TextAreaDrawer
            };
            return config(group, name, value, new ConfigDescription(desc, null, attributes));
        }

        internal static void TextAreaDrawer(ConfigEntryBase entry)
        {
            GUILayout.ExpandHeight(true);
            GUILayout.ExpandWidth(true);
            entry.BoxedValue = GUILayout.TextArea((string)entry.BoxedValue, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
        }
        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }
}