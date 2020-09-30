using BattleTech;
using System.Collections.Generic;

namespace TrainingMissions
{

    public static class ModState
    {
        public static bool IsTrainingMission = false;
        public static List<MechDef> deployedMechs = new List<MechDef>();
    }
}