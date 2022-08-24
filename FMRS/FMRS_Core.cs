/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 SIT89
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
using KSP.IO;
using Contracts;
using KSP.UI;
using KSP.UI.Screens;
using System.Collections;

namespace FMRS
{
    public partial class FMRS_Core : FMRS_Util, IFMRS
    {
        //private string version_number = "1.0.3";
        public bool plugin_active = false;
        public double Time_Trigger_Staging, Time_Trigger_Start_Delay, Time_Trigger_Cuto;
        public bool timer_staging_active = false, timer_start_delay_active = false, timer_cuto_active = false;
        public float Timer_Stage_Delay = 0.2f;
        public double Timer_Start_Delay = 2;
        public double last_staging_event = 0;
        public string quicksave_file_name;
        public Guid anz_id;
        public bool separated_vessel, staged_vessel;
        public bool n_launchpad_preflight = false, flight_preflight = false;
        public bool main_ui_active = false;
        public bool revert_to_launch = false;
        public bool really_close = false;
        public bool show_setting = false;

        public string stockTexture;
        public string blizzyTexture;

        public static Texture2D upArrow;
        public static Texture2D downArrow;
        public GUIContent upContent;
        public GUIContent downContent;
        public GUIContent buttonContent;

        public bool can_restart, can_q_save_load;
        private int nr_save_files = 0;
        public Vector2 scroll_Vector = Vector2.zero;
        GUIStyle button_main, button_green, button_red, button_yellow, button_small, button_small_red, button_big;
        GUIStyle text_main, text_green, text_cyan, text_red, text_yellow, text_heading;
        GUIStyle area_style, scrollbar_stlye;
        private bool skin_init = false;
        private List<science_data_sent> science_sent = new List<science_data_sent>();
        private float current_rep, last_rep_change;
        private List<killed_kerbal_str> killed_kerbals = new List<killed_kerbal_str>();
        private Dictionary<Guid, List<Contract>> contract_complete = new Dictionary<Guid, List<Contract>>();
        private List<Guid> loaded_vessels = new List<Guid>();
        private List<Guid> damaged_vessels = new List<Guid>();
        private List<string> damaged_buildings = new List<string>();
        public FMRS_THL.FMRS_THL_Log ThrottleLogger;
        public FMRS_THL.FMRS_THL_Rep ThrottleReplay;
        public static bool HideFMRSUI = false;

#if DEBUG  //**************************
        public string[] debug_message = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
        public Rect debug_windowPos;
#endif //**************************

#if BETA //**************************
        //string beta_version = " .01";
        public Rect beta_windowPos;
#endif   //**************************


        /*************************************************************************************************************************/
        public void FMRS_core_awake()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.FMRS_core_awake", "entering FMRS_core_awake()");

            mod_vers = "v";
#endif
#if DEBUG //**************************
            mod_vers = "x";
#endif //**************************

#if BETA //**************************
            mod_vers = "b";
#endif //**************************

#if DEBUG
            mod_vers += FMRS_Version_Info.version_number;
#endif

#if BETA //**************************
            mod_vers += FMRS_Version_Info.beta_version;
#endif //**************************
            init_Save_File_Content();
            load_save_file();

#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leaving FMRS_core_awake()");
#endif
            if (HighLogic.CurrentGame != null)
                Timer_Stage_Delay = HighLogic.CurrentGame.Parameters.CustomParams<FMRS_Settings>().Timer_Stage_Delay;
        }


        /*************************************************************************************************************************/
        public void flight_scene_start_routine()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.flight_scene_start_routine", "entering flight_scene_start_routine()");
//            if (Debug_Active)
                Log.Info("FMRS flight_scene_start_routine");
#endif

            plugin_active = true;
            if (FlightGlobals.ActiveVessel == null)
                Log.Info("ActiveVessel is null");
            else
            {
                if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH || n_launchpad_preflight || flight_preflight)
                {
#if DEBUG
//                if (Debug_Active)
                    Log.Info("FMRS Vessel is prelaunch");
#endif

                    delete_dropped_vessels();
                    _SETTING_Enabled = true;
                    _SAVE_Switched_To_Dropped = false;
                    _SAVE_Kick_To_Main = false;
                    _SAVE_Main_Vessel = FlightGlobals.ActiveVessel.id;
                    _SAVE_Launched_At = 0;
                    _SAVE_Has_Launched = false;

                    if (flight_preflight)
                        _SAVE_Flight_Reset = true;

                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        foreach (PartModule pm in p.Modules)
                            if (pm.moduleName == "FMRS_PM")
                                pm.StartCoroutine("resetid");
                    }

                    recover_values.Clear();

                    StartCoroutine(save_game_pre_flight());
                }
            }
            
            can_restart = HighLogic.CurrentGame.Parameters.Flight.CanRestart;
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad && HighLogic.CurrentGame.Parameters.Flight.CanQuickSave)
                can_q_save_load = true;
            else
                can_q_save_load = false;

            if (FlightGlobals.ActiveVessel.id == _SAVE_Main_Vessel)
            {
                _SAVE_Switched_To_Dropped = false;
                _SAVE_Kick_To_Main = false;
            }
            else
                _SAVE_Switched_To_Dropped = true;
            

            Time_Trigger_Start_Delay = Planetarium.GetUniversalTime();
            timer_start_delay_active = true;

            if (_SAVE_Switched_To_Dropped)
            {
                foreach (KeyValuePair<Guid, string> kvp in Vessels_dropped)
                {
                    if (kvp.Value == _SAVE_Switched_To_Savefile)
                    {
                        if (get_vessel_state(kvp.Key) == vesselstate.FLY)
                        {
                            loaded_vessels.Add(kvp.Key);
                        }
                    }
                }
#if DEBUG
                if (Debug_Active)
                    Log.Info("loaded_vessels: " + loaded_vessels.Count.ToString());
#endif
                if (_SETTING_Throttle_Log)
                {
#if DEBUG
                    ThrottleReplay = new FMRS_THL.FMRS_THL_Rep(Debug_Active, Debug_Level_1_Active);
#else
                    ThrottleReplay = new FMRS_THL.FMRS_THL_Rep();
#endif

                    foreach (Vessel v in FlightGlobals.Vessels)
                    {
                        if (v.id == _SAVE_Main_Vessel)
                        {
                            //if (v.loaded)
                            {
                                Log.Info("appling flybywire callback to main vessel");
                                v.OnFlyByWire += new FlightInputCallback(ThrottleReplay.flybywire);
                            }
                        }
                    }
                }
            }

            if ((windowPos.x == 0) && (windowPos.y == 0))
            {
                windowPos = new Rect(Screen.width / 2, Screen.height / 2, 50, 50);
                write_save_values_to_file();
            }

            Vessels.Clear();
            fill_Vessels_list();

            timer_staging_active = false;
            really_close = false;
            revert_to_launch = false;
            separated_vessel = false;
            staged_vessel = false;

            current_rep = Reputation.CurrentRep;
            last_rep_change = 0;

            attach_handlers();

            if (!main_ui_active)
            {
                main_ui_active = true;
#if DEBUG
                if (Debug_Active)
                    Log.Info("activate drawMainGUI");
#endif
            }

#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leaving flight_scene_start_routine()");
#endif
        }


        /*************************************************************************************************************************/
        public void flight_scene_update_routine()
        {
            Log.PushStackInfo("FMRS_Core.flight_scene_update_routine", "entering flight_scene_update_routine()");
            Instance = this;
            if (_SETTING_Enabled)
            {
                if (timer_start_delay_active)
                    if ((Timer_Start_Delay + Time_Trigger_Start_Delay) <= Planetarium.GetUniversalTime())
                    {
                        if (!_SAVE_Switched_To_Dropped)
                        {
                            write_recovered_values_to_save();
                        }
                        else
                        {
                            List<Guid> temp_guid_list = new List<Guid>();
                            foreach (Guid id in loaded_vessels)
                            {
                                if (FlightGlobals.Vessels.Find(v => v.id == id) == null)
                                    temp_guid_list.Add(id);
                            }
                            foreach (Guid id in temp_guid_list)
                            {
#if DEBUG
                                if (Debug_Active)
                                    Log.Info("loaded_vessels: removing " + id.ToString());
#endif
                                loaded_vessels.Remove(id);
                            }
#if DEBUG
                            if (Debug_Active)
                                Log.Info("loaded_vessels: " + loaded_vessels.Count.ToString());
#endif
                        }
                        timer_start_delay_active = false;
                    }

                if (timer_staging_active)
                {
                    if (timer_cuto_active && _SETTING_Auto_Cut_Off)
                    {
                        //if ((Time_Trigger_Cuto + 0.1) <= Planetarium.GetUniversalTime())
                        //if (_SETTING_Auto_Cut_Off)
                        {
#if DEBUG
                            if (Debug_Active)
                                Log.Info("auto thrust cut off");
#endif
                            foreach (Vessel temp_vessel in FlightGlobals.Vessels)
                            {
                                if (!Vessels.Contains(temp_vessel.id))
                                {
                                   
                                        temp_vessel.ctrlState.mainThrottle = 0;
                                }
                            }
                            timer_cuto_active = false;
                        }
                    }

                    if ((Time_Trigger_Staging + Timer_Stage_Delay) <= Planetarium.GetUniversalTime())
                    {
#if DEBUG
                        if (Debug_Active)
                            Log.Info("Has Staged Delayed");
#endif

                        last_staging_event = Planetarium.GetUniversalTime();

                        timer_staging_active = false;

                        quicksave_file_name = gamesave_name + FlightGlobals.ActiveVessel.currentStage.ToString();

                        if (Vessels_dropped.ContainsValue(quicksave_file_name) || (separated_vessel && !staged_vessel))
                        {
                            int nr_save_file = 0;

                            foreach (KeyValuePair<Guid, string> temp_keyvalues in Vessels_dropped)
                            {
                                if (temp_keyvalues.Value.Contains("separated_"))
                                    if (nr_save_file <= Convert.ToInt16(temp_keyvalues.Value.Substring(20)))
                                        nr_save_file = Convert.ToInt16(temp_keyvalues.Value.Substring(20)) + 1;
                            }

                            quicksave_file_name = gamesave_name + "separated_" + nr_save_file;
                        }

                        separated_vessel = false;
                        staged_vessel = false;

                        if (search_for_new_vessels(quicksave_file_name))
                        {
                            // GamePersistence.SaveGame(quicksave_file_name, HighLogic.SaveFolder + "/FMRS", SaveMode.OVERWRITE);
                            FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.flight_scene_update_routine", quicksave_file_name, HighLogic.SaveFolder + "/FMRS", SaveMode.OVERWRITE);

                            if (_SAVE_Main_Vessel != FlightGlobals.ActiveVessel.id && !_SAVE_Switched_To_Dropped)
                                main_vessel_changed(quicksave_file_name);

                            set_save_value(save_cat.SAVEFILE, quicksave_file_name, Planetarium.GetUniversalTime().ToString());
                            write_save_values_to_file();
                        }
                    }
                }

                if (n_launchpad_preflight && !FlightGlobals.ActiveVessel.Landed)
                {
                    EventReport dummy_event = null;
#if DEBUG
                    if (Debug_Active)
                        Log.Info("non launchpad launch");
#endif
                    n_launchpad_preflight = false;
                    launch_routine(dummy_event);
                }
            }
#if DEBUG
            Log.PopStackInfo("leaving flight_scene_update_routine");
#endif
        }


        /*************************************************************************************************************************/
        private IEnumerator save_game_pre_flight()
        {
            while (!FlightGlobals.ActiveVessel.protoVessel.wasControllable)
                yield return 0;

            FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.save_game_pre_flight", "before_launch", HighLogic.SaveFolder + "/FMRS", SaveMode.OVERWRITE);
            FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.save_game_pre_flight", "FMRS_main_save", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }


        /*************************************************************************************************************************/
        public void toolbar_button_clicked()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.toolbar_button_clicked", "enter toolbar_button_clicked()");
            if (Debug_Active)
                Log.Info("Toolbar Button Clicked");
#endif

            if (_SETTING_Enabled)
            {
                if (!_SAVE_Has_Launched)
                    close_FMRS();
                else
                {
                    if (really_close)
                        really_close = false;
                    else
                        really_close = true;
                }
            }
            else
                toolbar_open();

            write_save_values_to_file();
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leave toolbar_button_clicked()");
#endif
        }


        /*************************************************************************************************************************/
        bool onLaunchEnabled  = false;
        public void toolbar_open()
        {
            bool arm_save = false;

#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.toolbar_open", "enter toolbar_open()");
            if (Debug_Active)
                Log.Info("enable plugin form toolbar");
#endif

            stockTexture = "FMRS/icons/tb_st_en";
            blizzyTexture = "FMRS/icons/tb_blz_en";
            FMRS.toolbarControl.SetTexture(stockTexture, blizzyTexture);
            Log.Info("SetTexture 2, stockTexture: " + stockTexture + ",   blizzyTexture" + blizzyTexture);

            _SETTING_Enabled = true;

            if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("start plugin on launchpad");
#endif
                GameEvents.onLaunch.Add(launch_routine);
                onLaunchEnabled = true;
                flight_scene_start_routine();
            }
            else if (FlightGlobals.ActiveVessel.Landed)
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("start plugin not on launchpad");
#endif
                n_launchpad_preflight = true;
                flight_scene_start_routine();
            }
            else
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("start plugin during flight");
#endif
                if (!_SETTING_Armed)
                {
                    _SETTING_Armed = true;
                    arm_save = true;
                }
                flight_preflight = true;
                flight_scene_start_routine();
                EventReport dummy_event = null;
                launch_routine(dummy_event);
                if (arm_save)
                    _SETTING_Armed = false;
                flight_preflight = false;
            }
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leave toolbar_open()");
#endif
        }

        void OnDestroy()
        {
            if (onLaunchEnabled)
                GameEvents.onLaunch.Add(launch_routine);
            onLaunchEnabled = false;
        }
        /*************************************************************************************************************************/
        public void close_FMRS()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.close_FMRS", "enter close_FMRS()");
            if (Debug_Active)
                Log.Info("close plugin");
#endif
            _SETTING_Enabled = false;
            _SAVE_Has_Launched = false;
            delete_dropped_vessels();
            really_close = false;
            _SAVE_Flight_Reset = false;

            FMRS.toolbarControl.SetTexture(stockTexture, blizzyTexture);

            if (_SAVE_Has_Launched && _SAVE_Switched_To_Dropped)
                jump_to_vessel("Main");

            destroy_FMRS();
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leave close_FMRS()");
#endif
        }


        /*************************************************************************************************************************/
        public void destroy_FMRS()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.destroy_FMRS", "enter destroy_FMRS()");
#endif
            plugin_active = false;

            if (!_SETTING_Enabled)
                _SAVE_Has_Launched = false;

            if (ThrottleLogger != null)
                ThrottleLogger.EndLog();

            if (ThrottleReplay != null)
                ThrottleReplay.EndReplay();

            write_save_values_to_file();
            write_recover_file();

            if (main_ui_active)
            {
                main_ui_active = false;
#if DEBUG
                if (Debug_Active)
                    Log.Info("close drawMainGUI");
#endif
            }

            detach_handlers();

            n_launchpad_preflight = false;

            if (ThrottleLogger != null)
                ThrottleLogger.EndLog();
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leave destroy_FMRS()");
#endif
        }
    }
}
