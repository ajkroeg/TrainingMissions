using System;
using Harmony;
using BattleTech;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using BestHTTP.SocketIO;
using Localize;
using Org.BouncyCastle.Asn1.X509;
using UnityEngine;

namespace TrainingMissions

{
	static class TrainingMissions_Patches
	{
        [HarmonyPatch(typeof(SimGameState), "RequestDataManagerResources")]
        public static class SimGameState_RequestDataManagerResources_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                LoadRequest loadRequest = __instance.DataManager.CreateLoadRequest(null, true);
                loadRequest.AddAllOfTypeBlindLoadRequest(BattleTechResourceType.LanceDef, new bool?(true));
                loadRequest.ProcessRequests(10U);
            }
        }

        [HarmonyPatch(typeof(LanceConfiguratorPanel), "ContinueConfirmClicked")]
        public static class LCP_ContinueConfirmClicked
        {

            public static void Prefix(LanceConfiguratorPanel __instance, LanceLoadoutSlot[] ___loadoutSlots, bool ___mechWarningsCheckResolved)
            {
                if (___mechWarningsCheckResolved)
                {
                    ModState.runContinueConfirmClickedPost = true;
                }
            }

            public static void Postfix(LanceConfiguratorPanel __instance, LanceLoadoutSlot[] ___loadoutSlots)
            {
                if (!ModState.runContinueConfirmClickedPost) return;
                ModState.IsSimulatorMission = false;
                ModState.AIGetsPlayerMechs = false;
                ModState.PlayerGetsAIMechs = false;
                ModState.playerMechs = new List<ModState.playerMechStore>();
                ModState.deployedMechs = new List<MechDef>();
                var hk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (hk) ModState.IsSimulatorMission = true;

                if (ModInit.Settings.SwapUnitsWithAIContractIDs.Contains(__instance.activeContract.Override.ID) ||
                    ModInit.Settings.DoppelgangerContractIDs.Contains(__instance.activeContract.Override.ID))
                {
                    ModState.AIGetsPlayerMechs = true;
                    foreach (var slot in ___loadoutSlots)
                    {
                        if (slot.SelectedMech?.MechDef == null) continue;
                        var newMech = new MechDef(slot.SelectedMech.MechDef, null, true);
                        ModState.playerMechs.Add(new ModState.playerMechStore(slot.SelectedMech.MechDef, 0));
                        ModInit.modLog.LogMessage($"Added {slot.SelectedMech.MechDef.Description.Name} to player mechs for AI use.");
                    }
                }
                ModState.runContinueConfirmClickedPost = false;
                if (ModInit.Settings.SwapUnitsWithAIContractIDs.Contains(__instance.activeContract.Override.ID))
                {
                    ModState.PlayerGetsAIMechs = true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitSpawnPointOverride), "SelectTaggedUnitDef")]
        public static class UnitSpawnPointOverride_SelectTaggedUnitDef
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0;
            public static void Postfix(UnitSpawnPointOverride __instance, ref UnitDef_MDD __result)
            {
                if (ModState.PlayerGetsAIMechs)
                {
                    var dm = UnityGameInstance.BattleTechGame.DataManager;
                    ModState.AIMechs.Add(new ModState.playerMechStore(dm.MechDefs.Get(__result.UnitDefID), 0));
                    ModInit.modLog.LogMessage($"Got unitoverride {__result.UnitDefID} for player");
                }
                if (ModState.AIGetsPlayerMechs)
                {
                    ModInit.modLog.LogMessage($"First mech in playerMechs was {ModState.playerMechs.First().mechDef.Name} with count {ModState.playerMechs.First().count}");
                    ModState.playerMechs = ModState.playerMechs.OrderBy(x => x.count).ToList();
                    ModInit.modLog.LogMessage($"Reordered! First mech in playerMechs is now {ModState.playerMechs.First().mechDef.Name} with count {ModState.playerMechs.First().count}");
                    Traverse.Create(__result).Property("UnitDefID").SetValue(ModState.playerMechs[0].mechDef.Description.Id);
                    ModState.playerMechs.First().count += 1;
                }
            }
        }

        [HarmonyPatch(typeof(UnitSpawnPointGameLogic))]
        [HarmonyPatch("Spawn", new Type[] {typeof(bool)})]
        public static class UnitSpawnPointGameLogic_Spawn
        {
            public static void Prefix(UnitSpawnPointGameLogic __instance, ref PilotDef ___pilotDefOverride)
            {
                ModInit.modLog.LogMessage($"Entered UnitSpawnPointGameLogic.Spawn, PRE");
                ModInit.modLog.LogMessage($"__instance.pilotDefID was {__instance.pilotDefId}, while ___pilotDefOverride was {___pilotDefOverride?.Description?.Id}");
                if (__instance.pilotDefId == "commander")
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    ___pilotDefOverride = sim.Commander.pilotDef;
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: Forcing ___pilotDefOverride to be Commander again because this is an ugly hacky fix that I don't understand. Fuck you Battletech.");
                }
            }

            public static void Postfix(UnitSpawnPointGameLogic __instance, PilotDef ___pilotDefOverride)
            {
                ModInit.modLog.LogMessage($"Entered UnitSpawnPointGameLogic.Spawn, POST");
                ModInit.modLog.LogMessage($"__instance.pilotDefID was {__instance.pilotDefId}, while ___pilotDefOverride was {___pilotDefOverride?.Description?.Id}");
            }
        }

        [HarmonyPatch(typeof(UnitSpawnPointGameLogic))]
        [HarmonyPatch( "OverrideSpawn", new Type[] {typeof(SpawnableUnit)})]
        public static class UnitSpawnPointGameLogic_OverrideSpawn
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0;
            public static bool Prefix(UnitSpawnPointGameLogic __instance, SpawnableUnit spawnableUnit, ref MechDef ___mechDefOverride, ref PilotDef ___pilotDefOverride, ref VehicleDef ___vehicleDefOverride, ref TurretDef ___turretDefOverride)
            {
                if (ModState.PlayerGetsAIMechs && __instance.team == TeamDefinition.Player1TeamDefinitionGuid && spawnableUnit.unitType == UnitType.Mech)
                {
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: First mech in AIMechVariants was {ModState.AIMechs.First().mechDef.Name} with count {ModState.AIMechs.First().count}");
                    var AIMechVariants = ModState.AIMechs.OrderBy(x => x.count).ToList();
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: Filtered to match mechdef IDs and reordered! First mech in AIMechVariants is now {AIMechVariants.First().mechDef.Name} with count {AIMechVariants.First().count}");
                    ModState.AIMechs.First().count += 1;
                    var newMechDef = AIMechVariants.FirstOrDefault()?.mechDef;
                    newMechDef.DependenciesLoaded(1000U);
//                    Traverse.Create(__instance).Field("mechDefOverride").SetValue(newMechDef);
//                    Traverse.Create(__instance).Field("pilotDefOverride").SetValue(spawnableUnit.Pilot);
                    ___mechDefOverride = newMechDef;
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: mechDefOverride set to {___mechDefOverride.Description.Id}");
                    ___pilotDefOverride = spawnableUnit.Pilot;
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: pilotDefOverride set to {___pilotDefOverride.Description.Id}");
                    __instance.pilotDefId = spawnableUnit.PilotId;
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: __instance.pilotDefId is {__instance.pilotDefId}, should be {___pilotDefOverride.Description.Id}");
                    __instance.mechDefId = newMechDef.Description.Id;
                    ModInit.modLog.LogMessage(
                        $"PLAYER UNIT: __instance.mechDefId is {__instance.mechDefId}, should be {___mechDefOverride.Description.Id}");
                    __instance.unitType = spawnableUnit.unitType;

                    ___vehicleDefOverride = spawnableUnit.VUnit;
                    ___turretDefOverride = spawnableUnit.TUnit;
                    __instance.vehicleDefId = spawnableUnit.UnitId;
                    __instance.turretDefId = spawnableUnit.UnitId;
                    
                    return false;
                }
                else return true;
            }
        }

        [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "SpawnMech")]
        public static class UnitSpawnPointGameLogic_SpawnMech
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0 || ModInit.Settings.DoppelgangerContractIDs.Count > 0;
            public static void Prefix(UnitSpawnPointGameLogic __instance, ref MechDef mDef)
            {
                if (ModState.AIGetsPlayerMechs && __instance.team == TeamDefinition.TargetsTeamDefinitionGuid)
                {
                    var oldmDef = mDef;
                    ModInit.modLog.LogMessage($"AI UNIT: First mech in playerMechs was {ModState.playerMechs.First().mechDef.Name} with count {ModState.playerMechs.First().count}");
                    var playerMechVariants = ModState.playerMechs.Where(x=>x.mechDef.Description.Id == oldmDef.Description.Id).OrderBy(x => x.count).ToList();
                    ModInit.modLog.LogMessage($"AI UNIT: Filtered to match mechdef IDs and reordered! First mech in playerMechVariants is now {playerMechVariants.First().mechDef.Name} with count {playerMechVariants.First().count}");
                    ModState.playerMechs.First().count += 1;
                    var newMechDef = playerMechVariants.FirstOrDefault()?.mechDef;
                    newMechDef.DependenciesLoaded(1000U);
                    mDef = newMechDef;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(CombatGameState), "FirstTimeInit")]
        public static class CGS_FirstTimeInit_Patch
        {
            public static void Postfix(CombatGameState __instance, Contract contract, GameInstance game,
                string localPlayerTeamGuid)
            {
                if (__instance.ActiveContract.ContractTypeValue.IsSkirmish) return;

                ModState.deployedMechs = new List<MechDef>();
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
                var combat = UnityGameInstance.BattleTechGame.Combat;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                
                var p = __instance.pilot;
                if (team.IsLocalPlayer && (ModState.IsTrainingMission || ModState.IsSimulatorMission) && (sim.PilotRoster.Any(x=>x.Callsign == p.Callsign) || p.IsPlayerCharacter))
                {
                    ModState.deployedMechs.Add(__instance.MechDef);
                    ModInit.modLog.LogMessage($"Adding {__instance.MechDef.Name} to ModState for restore");
                    return;
                }
                
            }
		}

        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        static class Contract_CompleteContract_Patch
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(Contract __instance)
            {
                if (ModState.IsSimulatorMission)
                {
                    Traverse.Create(__instance).Property("EmployerReputationResults").SetValue(0);
                    Traverse.Create(__instance).Property("TargetReputationResults").SetValue(0);
                    Traverse.Create(__instance).Property("MoneyResults").SetValue(0);
                    Traverse.Create(__instance).Property("SalvageResults").SetValue(new List<SalvageDef>());
                }
            }
        }


        [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
        public static class SGS_ResolveCompleteContract_Patch
        {
            public static void Prefix(SimGameState __instance, out Contract __state)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;
                __state = __instance.CompletedContract;
                ModInit.modLog.LogMessage($"Contract added to state for postfix.");
            }
            public static void Postfix(SimGameState __instance, Contract __state)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;

                if (ModState.IsSimulatorMission)
                {

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

                    ModState.deployedMechs = new List<MechDef>();
                    ModState.IsSimulatorMission = false;
                    ModState.IsTrainingMission = false;
                    ModState.AIGetsPlayerMechs = false;
                    ModState.PlayerGetsAIMechs = false;
                }

                else if (ModState.IsTrainingMission)
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
                    ModState.deployedMechs = new List<MechDef>();
                    ModState.IsTrainingMission = false;
                    ModState.IsSimulatorMission = false;
                    ModState.AIGetsPlayerMechs = false;
                    ModState.PlayerGetsAIMechs = false;
                }
            }
        }
    }
}