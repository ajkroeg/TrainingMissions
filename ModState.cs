using BattleTech;
using System.Collections.Generic;

namespace TrainingMissions
{

    public static class ModState
    {
        public static bool IsTrainingMission = false;
        public static int successReq = 0;

        public static List<MechDef> deployedMechs = new List<MechDef>();
    }
}