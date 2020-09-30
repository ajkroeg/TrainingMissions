using System;
using Harmony;
using BattleTech;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using BestHTTP.SocketIO;

namespace TrainingMissions

{
	static class TrainingMissions_Patches
	{
        public static K FindFirstKeyByValue<K, V>(this Dictionary<K, V> dict, V val)
        {
            try
            {
                return dict.FirstOrDefault(entry => EqualityComparer<V>.Default.Equals(entry.Value, val))
                                .Key;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }


        public static MechDef GetMechDefByGUID(string GUID, SimGameState __instance)

        {
            List<MechDef> list = new List<MechDef>(__instance.ActiveMechs.Values);
            if (__instance.ActiveMechs == null)
            {
                return null;
            }
            for (int i = 0; i < list.Count; i++)
            {
                MechDef mechDef = list[i];
 //               if (mechDef != null && string.Compare(GUID, mechDef.GUID) == 0)
                if (mechDef != null && (GUID == mechDef.GUID))
                {
                    return mechDef;
                }
            }
            return null;
        }

		[HarmonyPatch(typeof(CombatGameState), "FirstTimeInit")]
        public static class CGS_FirstTimeInit_Patch
        {
            public static void Postfix(CombatGameState __instance, Contract contract, GameInstance game,
                string localPlayerTeamGuid)
            {
                if (ModInit.Settings.TrainingContractIDs.Contains(contract.Override.ID))
                {
                    ModState.IsTrainingMission = true;
                    ModInit.modLog.LogMessage($"{contract.Name} is a Training Mission, setting IsTrainingMission true.");
                    return;
                }
                ModState.IsTrainingMission = false;
                ModInit.modLog.LogMessage($"{contract.Name} is not Training Mission, setting IsTrainingMission false.");
            }
        }

        [HarmonyPatch(typeof(Mech), "AddToTeam")]
        public static class Mech_AddToTeam_Patch
        {
            public static void Postfix(Mech __instance, Team team)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                var p = __instance.pilot;
                if (team.IsLocalPlayer && (sim.PilotRoster.Any(x=>x.Callsign == p.Callsign) || p.IsPlayerCharacter))
                {
                    ModState.deployedMechs.Add(__instance.MechDef);
                    ModInit.modLog.LogMessage($"Adding {__instance.MechDef.Name} to ModState for restore");
                    return;
                }
                
            }
		}


        [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
        public static class SGS_ResolveCompleteContract_Patch
        {

            public static void Postfix(SimGameState __instance)
            {
                foreach (var kvp in new Dictionary<int, MechDef>(__instance.ActiveMechs))
                {
                    if (ModState.deployedMechs.Any(x => x.GUID == kvp.Value.GUID))
                    {
                        __instance.ActiveMechs.Remove(kvp.Key);
                    }
                    ModInit.modLog.LogMessage($"Removing old {kvp.Value.Name} from MechBay");
                }
                foreach (var deployedMech in ModState.deployedMechs)
                {
                    __instance.ActiveMechs.Add(__instance.GetFirstFreeMechBay(), deployedMech);

                    ModInit.modLog.LogMessage($"Added replacement {deployedMech.Name}");
                }
                ModState.deployedMechs = new List<MechDef>();
                ModState.IsTrainingMission = false;
            }
        }
    }
}