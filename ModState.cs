using System;
using BattleTech;
using System.Collections.Generic;

namespace TrainingMissions
{

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

        public static bool IsTrainingMission = false;
        public static bool IsSimulatorMission = false;
        public static bool AIGetsPlayerMechs = false;
        public static bool PlayerGetsAIMechs = false;
        public static int successReq = 0;

        public static List<MechDef> deployedMechs = new List<MechDef>();

        public static List<playerMechStore> playerMechs = new List<playerMechStore>();
        public static List<playerMechStore> AIMechs = new List<playerMechStore>();

    }
}