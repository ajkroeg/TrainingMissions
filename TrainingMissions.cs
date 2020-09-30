using System;
using Harmony;
using BattleTech;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using BestHTTP.SocketIO;
using Localize;

namespace TrainingMissions

{
	static class TrainingMissions_Patches
	{

        [HarmonyPatch(typeof(CombatGameState), "FirstTimeInit")]
        public static class CGS_FirstTimeInit_Patch
        {
            public static void Postfix(CombatGameState __instance, Contract contract, GameInstance game,
                string localPlayerTeamGuid)
            {
                ModState.deployedMechs.Clear();
                ModState.IsTrainingMission = false;
                ModState.successReq = 0;
                if (ModInit.Settings.TrainingContractIDs.Keys.Contains(contract.Override.ID))
                {
                    ModState.IsTrainingMission = true;
                    ModState.successReq = ModInit.Settings.TrainingContractIDs[contract.Override.ID];
                    ModInit.modLog.LogMessage($"{contract.Name} is a Training Mission, setting IsTrainingMission true.");
                    return;
                }
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
            public static void Prefix(SimGameState __instance, out Contract __state)
            {
                __state = __instance.CompletedContract;
                ModInit.modLog.LogMessage($"Contract added to state for postfix.");
            }
            public static void Postfix(SimGameState __instance, Contract __state)
            {
                if (ModState.IsTrainingMission)
                {
                    if (ModState.successReq == 2 && __state.State != Contract.ContractState.Complete)
                    {
                        ModInit.modLog.LogMessage($"Mission was not successful, not restoring mechs.");
                        return;
                    }

                    if (ModState.successReq == 1 && !__state.IsGoodFaithEffort &&
                        (__state.State == Contract.ContractState.Failed || __state.State == Contract.ContractState.Retreated))
                    {
                        ModInit.modLog.LogMessage(
                            $"Mission failed, not restoring mechs.");
                        return;
                    }

                    foreach (var kvp in new Dictionary<int, MechDef>(__instance.ActiveMechs))
                    {
                        if (ModState.deployedMechs.Any(x => x.GUID == kvp.Value.GUID))
                        {
                            __instance.ActiveMechs.Remove(kvp.Key);
                            ModInit.modLog.LogMessage($"Removing old {kvp.Value.Name} from MechBay");
                        }

                        
                    }

                    foreach (var deployedMech in ModState.deployedMechs)
                    {
                        __instance.ActiveMechs.Add(__instance.GetFirstFreeMechBay(), deployedMech);

                        ModInit.modLog.LogMessage($"Added replacement {deployedMech.Name}");
                    }

                    if (ModInit.Settings.showRestoreNotification && ModState.deployedMechs.Count > 0)
                    {
                        Traverse.Create(__instance).Field("interruptQueue").GetValue<SimGameInterruptManager>()
                            .QueuePauseNotification("Mechs Restored",
                                "As per the terms of the contract, our employer has repaired, replaced, and refitted our damaged and destroyed units. Our pilots are another story, however.",
                                __instance.GetCrewPortrait(SimGameCrew.Crew_Darius), "", null, "Continue", null, null);
                    }
                }
                ModState.deployedMechs.Clear();
                ModState.IsTrainingMission = false;
            }
        }
    }
}