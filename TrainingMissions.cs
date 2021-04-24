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
using static TrainingMissions.ModState;

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
                ModState.IsSimulatorMission = false;
                if (ModInit.Settings.enableSimulationHotKey)
                {
                    var hk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (hk) ModState.IsSimulatorMission = true;
                }
                
                bool flag = false;
                foreach (LanceLoadoutSlot lanceLoadoutSlot in ___loadoutSlots)
                {
                    if (lanceLoadoutSlot.SelectedMech != null)
                    {
                        List<Text> mechFieldableWarnings =
                            MechValidationRules.GetMechFieldableWarnings(__instance.dataManager,
                                lanceLoadoutSlot.SelectedMech.MechDef);
                        if (mechFieldableWarnings.Count > 0)
                        {
                            flag = true;
                        }
                    }
                }
                if (!ModState.runContinueConfirmClickedPost && flag) return;


//                ModState.IsTrainingMission = false;
                ModState.AIGetsPlayerMechs = false;
                ModState.PlayerGetsAIMechs = false;
//                ModState.successReq = "";
                ModState.playerMechs = new List<ModState.playerMechStore>();
                ModState.AIMechs = new List<ModState.playerMechStore>();
                ModState.deployedMechs = new List<MechDef>();
                ModState.contractID = __instance.activeContract.Override.ID;

                if (ModInit.Settings.enableSimulationHotKey)
                {
                    var hk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (hk) ModState.IsSimulatorMission = true;
                }
                var newMech = new MechDef();
                if (ModInit.Settings.SwapUnitsWithAIContractIDs.ContainsKey(contractID))
                {
                    ModState.AIGetsPlayerMechs = true;
                    ModState.PlayerGetsAIMechs = true;
                    foreach (var slot in ___loadoutSlots)
                    {
                        if (slot.SelectedMech?.MechDef == null) continue;
                        var newGUID = Guid.NewGuid().ToString();
                        newMech = new MechDef(slot.SelectedMech.MechDef, newGUID, true);
                    
                        ModState.playerMechs.Add(new ModState.playerMechStore(newMech, 0));
                        ModInit.modLog.LogMessage($"Added {slot.SelectedMech.MechDef.Description.Name} to player mechs for AI use.");
                        ModState.deployedMechs.Add(slot.SelectedMech.MechDef);
                    }
                }
                
                else if (ModInit.Settings.DoppelgangerContractIDs.ContainsKey(contractID))
                {
                    ModState.AIGetsPlayerMechs = true;
                    foreach (var slot in ___loadoutSlots)
                    {
                        if (slot.SelectedMech?.MechDef == null) continue; 
                        var newGUID = Guid.NewGuid().ToString();
                        newMech = new MechDef(slot.SelectedMech.MechDef, newGUID, true);

                        ModState.playerMechs.Add(new ModState.playerMechStore(newMech, 0));
                        ModInit.modLog.LogMessage($"Added {slot.SelectedMech.MechDef.Description.Name} to player mechs for AI use.");
                    }
                }

                if (ModInit.Settings.TrainingContractIDs.ContainsKey(contractID) || ModState.IsSimulatorMission)
                {
//                    ModState.successReq = ModInit.Settings.TrainingContractIDs[contractID];
//                    ModState.IsTrainingMission = true;
                    foreach (var slot in ___loadoutSlots)
                    {
                        ModState.deployedMechs.Add(slot.SelectedMech.MechDef);
                        ModInit.modLog.LogMessage($"Adding {slot.SelectedMech.MechDef.Name} to deployedMechs for restore");
                    }
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
        [HarmonyPatch( "OverrideSpawn", new Type[] {typeof(SpawnableUnit)})]
        public static class UnitSpawnPointGameLogic_OverrideSpawn
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0;
             public static bool Prefix(UnitSpawnPointGameLogic __instance, SpawnableUnit spawnableUnit, MechDef ___mechDefOverride, ref PilotDef ___pilotDefOverride, VehicleDef ___vehicleDefOverride, TurretDef ___turretDefOverride)
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

                return true;
             }
        }

        [HarmonyPatch(typeof(Mech), "AddToTeam")]
        public static class Mech_AddToTeam
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0 || ModInit.Settings.DoppelgangerContractIDs.Count > 0;
            public static void Postfix(Mech __instance)
            {
                if (__instance.pilot.ParentActor.team.GUID == TeamDefinition.Player1TeamDefinitionGuid)
                {
                    if (ModInit.Settings.DoppelgangerContractIDs.ContainsKey(contractID))
                    {
                        if (ModInit.Settings.DoppelgangerContractIDs[contractID] == "SIMULATOR")
                        {
                            var pilotID = __instance.pilot.Description.Id;
                            var injuryCount = __instance.pilot.StatCollection.GetValue<int>("Injuries");
                            ModState.pilotStartingInjuries.Add(pilotID, injuryCount);
                            ModInit.modLog.LogMessage($"{__instance.pilot.Callsign} ({pilotID}) has {injuryCount} injuries before contract. Saving to state for post-contract restore.");
                        }
                    }
                    else if (ModInit.Settings.SwapUnitsWithAIContractIDs.ContainsKey(contractID))
                    {
                        if (ModInit.Settings.SwapUnitsWithAIContractIDs[contractID] == "SIMULATOR")
                        {
                            var pilotID = __instance.pilot.Description.Id;
                            var injuryCount = __instance.pilot.StatCollection.GetValue<int>("Injuries");
                            ModState.pilotStartingInjuries.Add(pilotID, injuryCount);
                            ModInit.modLog.LogMessage($"{__instance.pilot.Callsign} ({pilotID}) has {injuryCount} injuries before contract. Saving to state for post-contract restore.");
                        }
                    }
                    else if (ModState.IsSimulatorMission)
                    {
                        var pilotID = __instance.pilot.Description.Id;
                        var injuryCount = __instance.pilot.StatCollection.GetValue<int>("Injuries");
                        ModState.pilotStartingInjuries.Add(pilotID, injuryCount);
                        ModInit.modLog.LogMessage($"{__instance.pilot.Callsign} ({pilotID}) has {injuryCount} injuries before contract. Saving to state for post-contract restore.");
                    }
                }
            }
        }


        [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "SpawnMech")]
        public static class UnitSpawnPointGameLogic_SpawnMech
        {
            static bool Prepare() => ModInit.Settings.SwapUnitsWithAIContractIDs.Count > 0 || ModInit.Settings.DoppelgangerContractIDs.Count > 0;
            public static void Prefix(UnitSpawnPointGameLogic __instance, ref MechDef mDef, PilotDef pilot, Team team, Mech __result)
            {
                if (__instance.team == TeamDefinition.TargetsTeamDefinitionGuid)
                {
                    if (ModState.AIGetsPlayerMechs)
                    {
                        var oldmDef = mDef;
                        ModInit.modLog.LogMessage(
                            $"AI UNIT: First mech in playerMechs was {ModState.playerMechs.First().mechDef.Name} with count {ModState.playerMechs.First().count}");
                        var playerMechVariants = ModState.playerMechs
                            .Where(x => x.mechDef.Description.Id == oldmDef.Description.Id).OrderBy(x => x.count)
                            .ToList();
                        ModInit.modLog.LogMessage(
                            $"AI UNIT: Filtered to match mechdef IDs and reordered! First mech in playerMechVariants is now {playerMechVariants.First().mechDef.Name} with count {playerMechVariants.First().count}");
                        ModState.playerMechs.First().count += 1;
                        var newMechDef = playerMechVariants.FirstOrDefault()?.mechDef;
                        newMechDef.DependenciesLoaded(1000U);
                        mDef = newMechDef;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInData", new Type[] {typeof(int)})]
        static class AAR_UnitStatusWidget_FillInDataPatch
        {
            public static void Postfix(AAR_UnitStatusWidget __instance, Contract ___contract, UnitResult ___UnitData)
            {
                if (ModInit.Settings.SwapUnitsWithAIContractIDs.ContainsKey(contractID))
                {
                    if (ModInit.Settings.SwapUnitsWithAIContractIDs[contractID] == "RECOVER")
                    {
                        if (!___UnitData.mech.MechTags.Any(x => ModInit.Settings.DisallowedRecoveryTags.Contains(x)))
                        {
                            ModState.recoveredMechDefs.Add(___UnitData.mech);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        static class Contract_CompleteContract_Patch
        {
            [HarmonyPriority(Priority.Last)]
            
            public static void Postfix(Contract __instance, ref List<SalvageDef> ___finalPotentialSalvage)
            {
                if (ModInit.Settings.SwapUnitsWithAIContractIDs.ContainsKey(contractID))
                {
                    if (ModInit.Settings.SwapUnitsWithAIContractIDs[contractID] == "SIMULATOR")
                    {
                        Traverse.Create(__instance).Property("EmployerReputationResults").SetValue(0);
                        Traverse.Create(__instance).Property("TargetReputationResults").SetValue(0);
                        Traverse.Create(__instance).Property("SalvageResults").SetValue(new List<SalvageDef>());
                        ___finalPotentialSalvage = new List<SalvageDef>();
                        foreach (var unitresult in __instance.PlayerUnitResults)
                        {
                            var pilotID = unitresult.pilot.Description.Id;
                            if (ModState.pilotStartingInjuries.ContainsKey(pilotID))
                            {
                                var injuryCount = ModState.pilotStartingInjuries[pilotID];
                                unitresult.pilot.StatCollection.Set("Injuries", injuryCount);
                            }
                        }
                    }
                }
                else if (ModInit.Settings.DoppelgangerContractIDs.ContainsKey(contractID))
                {
                    if (ModInit.Settings.DoppelgangerContractIDs[contractID] == "SIMULATOR")
                    {
                        Traverse.Create(__instance).Property("EmployerReputationResults").SetValue(0);
                        Traverse.Create(__instance).Property("TargetReputationResults").SetValue(0);
                        Traverse.Create(__instance).Property("SalvageResults").SetValue(new List<SalvageDef>());
                        ___finalPotentialSalvage = new List<SalvageDef>();
                        foreach (var unitresult in __instance.PlayerUnitResults)
                        {
                            var pilotID = unitresult.pilot.Description.Id;
                            if (ModState.pilotStartingInjuries.ContainsKey(pilotID))
                            {
                                var injuryCount = ModState.pilotStartingInjuries[pilotID];
                                unitresult.pilot.StatCollection.Set("Injuries", injuryCount);
                            }
                        }
                    }
                }

                else if (ModState.IsSimulatorMission)
                {
                    Traverse.Create(__instance).Property("EmployerReputationResults").SetValue(0);
                    Traverse.Create(__instance).Property("TargetReputationResults").SetValue(0);
                    Traverse.Create(__instance).Property("MoneyResults").SetValue(0);
                    Traverse.Create(__instance).Property("SalvageResults").SetValue(new List<SalvageDef>());
                    ___finalPotentialSalvage = new List<SalvageDef>();
                    foreach (var unitresult in __instance.PlayerUnitResults)
                    {
                        var pilotID = unitresult.pilot.Description.Id;
                        if (ModState.pilotStartingInjuries.ContainsKey(pilotID))
                        {
                            var injuryCount = ModState.pilotStartingInjuries[pilotID];
                            unitresult.pilot.StatCollection.Set("Injuries", injuryCount);
                        }
                    }
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
                
                if (ModInit.Settings.SwapUnitsWithAIContractIDs.ContainsKey(__state.Override.ID)) 
                {
                    if (ModInit.Settings.SwapUnitsWithAIContractIDs[contractID] == "SIMULATOR")
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
                    }

                    else if (ModInit.Settings.SwapUnitsWithAIContractIDs[contractID] == "RECOVER")
                    {
                        foreach (var kvp in new Dictionary<int, MechDef>(__instance.ActiveMechs))
                        {
                            if (ModState.deployedMechs.Any(x => x.GUID == kvp.Value.GUID))
                            {
                                __instance.ActiveMechs.Remove(kvp.Key);
                                ModInit.modLog.LogMessage($"Removing original {kvp.Value.Name} from MechBay");
                            }
                        }
                        foreach (var recoveredMech in ModState.recoveredMechDefs)
                        {
                            __instance.ActiveMechs.Add(__instance.GetFirstFreeMechBay(), recoveredMech);

                            ModInit.modLog.LogMessage($"Added replacement damaged {recoveredMech.Name}");
                        }
                    }
                }

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
                }
                
                if (ModInit.Settings.TrainingContractIDs.ContainsKey(__state.Override.ID)) 
                {
                    if (ModInit.Settings.TrainingContractIDs[contractID] == "SUCCESS" && __state.State != Contract.ContractState.Complete)
                    {
                        ModInit.modLog.LogMessage($"Mission was not successful, not restoring mechs.");
                        return;
                    }

                    if (ModInit.Settings.TrainingContractIDs[contractID] == "GOODFAITH" && !__state.IsGoodFaithEffort &&
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
                ModState.IsSimulatorMission = false;
//                ModState.IsTrainingMission = false;
                ModState.AIGetsPlayerMechs = false;
                ModState.PlayerGetsAIMechs = false;
//                ModState.successReq = "";
                ModState.playerMechs = new List<ModState.playerMechStore>();
                ModState.AIMechs = new List<ModState.playerMechStore>();
                ModState.deployedMechs = new List<MechDef>();
                ModState.contractID = "";
                ModState.pilotStartingInjuries = new Dictionary<string, int>();
                ModState.recoveredMechDefs = new List<MechDef>();
                ModState.runContinueConfirmClickedPost = false;
            }
        }
    }
}