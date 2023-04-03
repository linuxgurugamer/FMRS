using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;



namespace FMRS
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class FMRS_Settings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } } // column heading
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "FMRS"; } }
        public override string DisplaySection { get { return "FMRS"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("#FMRS_Local_027")]//FMRS Enabled
        public bool enabled = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_028")]//Auto-Active at launch
        public bool autoactiveAtLaunch = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_029",//Include Undocking events as staging events
            toolTip = "#FMRS_Local_030")]//useful when staging isn't available (ie: after docking two ships, can't make the ports a stage)
        public bool _SETTING_Include_Undock = false;


        [GameParameters.CustomFloatParameterUI("#FMRS_Local_031", minValue = 0.2f, maxValue = 5.0f, asPercentage = false, displayFormat = "0.0",
                   toolTip = "#FMRS_Local_032")]//Stage Delay How long after staging before saves are taken
        public float Timer_Stage_Delay = 0.2f;

        [GameParameters.CustomParameterUI("#FMRS_Local_033")]//Messaging System
        public bool _SETTING_Messages = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_034")]//Auto Cut Off Engines
        public bool _SETTING_Auto_Cut_Off = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_035")]//Auto Recover Landed Crafts
        public bool _SETTING_Auto_Recover = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_036")]//Throttle Logger WIP
        public bool _SETTING_Throttle_Log = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_037", // Parachutes are controllable
            toolTip = "#FMRS_Local_038")]//If enabled, any stage with a parachute will be treated as controllable by the mod
        public bool _SETTING_Parachutes = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_039",//Defer parachute-only stages to Stage-Recovery (if installed)
            toolTip = "#FMRS_Local_040")]//If Stage Recovery is installed, do not control stages which only have parachutes.  Note that using RecoveryController to specify a mod to control the stage will override this.
        public bool _SETTING_Defer_Parachutes_to_StageRecovery = true;

        [GameParameters.CustomParameterUI("#FMRS_Local_041",//Uncontrolled stages are controllable
           toolTip = "#FMRS_Local_042")]//Ignored if RecoveryController is active.  If enabled, any stage will be treated as controllable by the mod, even if you have no control over it.
        public bool _SETTING_Control_Uncontrollable = false;


#if false
        [GameParameters.CustomParameterUI("Default all stages to Stage-Recovery (if installed)",
           toolTip = "If Stage Recovery is installed, it will control the recovery unless changed in the Decoupler")]
        public bool _SETTING_Default_to_StageRecovery = true;
#endif

#if DEBUG
        [GameParameters.CustomParameterUI("Debug mode (spams the log file")]
        public  bool Debug_Active = true;
        [GameParameters.CustomParameterUI("Debug mode 1 initial")]
        public  bool Debug_Level_1_Active = true;
        [GameParameters.CustomParameterUI("Debug mode 2 initial")]
        public  bool Debug_Level_2_Active = true;
#endif


        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "enabled")
                return true;

            return enabled; //otherwise return true
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return true;
            //            return true; //otherwise return true
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }

    }
}
