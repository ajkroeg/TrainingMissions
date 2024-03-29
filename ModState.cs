﻿using BattleTech;
using System.Collections.Generic;

namespace TrainingMissions
{
    public static class Util
    {
        public static void AddTrainingContract(Contract contract, string repairRequirement)
        {
            var id = contract.GenerateID();
            ModState.DynamicTrainingMissionsDict.Add(id, repairRequirement);
            ModInit.modLog.LogMessage($"Added contract with id {id} to dynamic training missions via external call");
        }

        public static string GenerateID(this Contract contract)
        {
            return $"{contract?.Override?.ID}_{contract?.Override?.employerTeam?.FactionValue?.Name}_{contract?.Override?.finalDifficulty}";
        }
    }

    public static class ModState
    {
        public class playerMechStore
        {
            public playerMechStore(MechDef mech, int counter)
            {
                this.mechDef = mech;
                this.count = counter;
            }
            public MechDef mechDef;
            public int count;
        }

        public static bool runContinueConfirmClickedPost = false;

//        public static bool IsTrainingMission = false;
        public static bool IsSimulatorMission = false;
        public static bool AIGetsPlayerMechs = false;
        public static bool PlayerGetsAIMechs = false;
//        public static string successReq = "";

        public static List<MechDef> deployedMechs = new List<MechDef>();

        public static List<playerMechStore> playerMechs = new List<playerMechStore>();
        public static List<playerMechStore> AIMechs = new List<playerMechStore>();

        public static List<MechDef> recoveredMechDefs = new List<MechDef>();

        public static Dictionary<string, int> pilotStartingInjuries = new Dictionary<string, int>();

        public static string contractID = "";

        public static Dictionary<string, string> DynamicTrainingMissionsDict = new Dictionary<string, string>(); //key is ID from GenerateID
    }
}