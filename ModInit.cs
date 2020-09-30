using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace TrainingMissions
{
    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;

        public static TrainingMissionsSettings Settings = new TrainingMissionsSettings();
        public const string HarmonyPackage = "us.tbone.TrainingMissions";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            try
            {
                ModInit.Settings = JsonConvert.DeserializeObject<TrainingMissionsSettings>(settingsJSON);
            }
            catch (Exception)
            {
                ModInit.Settings = new TrainingMissionsSettings();
            }

            modLog = new Logger(modDir, "TrainingMissions", Settings.enableLogging);
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    public class TrainingMissionsSettings
    {
        public bool enableLogging = true;
        public List<string> TrainingContractIDs = new List<string>();
    }

}