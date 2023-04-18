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
            ModInit.modLog.LogMessage($"Initializing TrainingMissions - Version {typeof(TrainingMissionsSettings).Assembly.GetName().Version}");
            //var harmony = HarmonyInstance.Create(HarmonyPackage);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }
    }
    public class TrainingMissionsSettings
    {
        public bool enableLogging = true;
        public bool showRestoreNotification = true;
        public bool enableSimulationHotKey = true;
        public Dictionary<string, string> TrainingContractIDs = new Dictionary<string, string>(); // SUCCESS, GOODFAITH, ALWAYS
        public Dictionary<string, string> SwapUnitsWithAIContractIDs = new Dictionary<string, string>(); // SIMULATOR, RECOVER
        public Dictionary<string, string> DoppelgangerContractIDs = new Dictionary<string, string>(); // SIMULATOR, RECOVER
        public List<string> DisallowedRecoveryTags = new List<string>();
    }

}