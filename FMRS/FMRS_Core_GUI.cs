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
using KSP.Localization;

using ClickThroughFix;

namespace FMRS
{
    public partial class FMRS_Core : FMRS_Util, IFMRS
    {
        internal static String _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }
        int baseWindowID;

        // Localization Strings
        private static string Local_001 = Localizer.GetStringByTag("#FMRS_Local_001");
        private static string Local_002 = Localizer.GetStringByTag("#FMRS_Local_002");
        private static string Local_003 = Localizer.GetStringByTag("#FMRS_Local_003");
        private static string Local_004 = Localizer.GetStringByTag("#FMRS_Local_004");
        private static string Local_005 = Localizer.GetStringByTag("#FMRS_Local_005");
        private static string Local_006 = Localizer.GetStringByTag("#FMRS_Local_006");
        private static string Local_007 = Localizer.GetStringByTag("#FMRS_Local_007");
        private static string Local_008 = Localizer.GetStringByTag("#FMRS_Local_008");
        private static string Local_009 = Localizer.GetStringByTag("#FMRS_Local_009");
        private static string Local_010 = Localizer.GetStringByTag("#FMRS_Local_010");
        private static string Local_011 = Localizer.GetStringByTag("#FMRS_Local_011");
        private static string Local_012 = Localizer.GetStringByTag("#FMRS_Local_012");
        private static string Local_013 = Localizer.GetStringByTag("#FMRS_Local_013");
        private static string Local_014 = Localizer.GetStringByTag("#FMRS_Local_014");

        private static string Local_020 = Localizer.GetStringByTag("#FMRS_Local_020");

        private static string Local_023 = Localizer.GetStringByTag("#FMRS_Local_023");
        private static string Local_024 = Localizer.GetStringByTag("#FMRS_Local_024");
        private static string Local_025 = Localizer.GetStringByTag("#FMRS_Local_025");
        private static string Local_026 = Localizer.GetStringByTag("#FMRS_Local_026");

        /*************************************************************************************************************************/
        private void Start()
        {
            baseWindowID = UnityEngine.Random.Range(1000, 2000000) + _AssemblyName.GetHashCode();
        }

        public void drawGUI()
        {
            if (HideFMRSUI)
                return;
            if (!skin_init)
                init_skin();
            GUI.skin = HighLogic.Skin;

            if (main_ui_active)
            {
                windowPos = ClickThruBlocker.GUILayoutWindow(baseWindowID + 1, windowPos, MainGUI, "FMRS " /* + mod_vers */, GUILayout.MinWidth(100));
                windowPos.x = Mathf.Clamp(windowPos.x, 0, Screen.width - windowPos.width);
                windowPos.y = Mathf.Clamp(windowPos.y, 0, Screen.height - windowPos.height);

#if BETA && !DEBUG //**************************
                beta_windowPos.x = windowPos.x;
                beta_windowPos.y = windowPos.y + windowPos.height;
                beta_windowPos =  ClickThruBlocker.GUILayoutWindow(baseWindowID = 3,beta_windowPos, BetaGUI, "FMRS Beta");
#endif //**************************
            }

#if DEBUG //**************************
            if (main_ui_active)
            {
                debug_windowPos.x = windowPos.x;
                debug_windowPos.y = windowPos.y + windowPos.height;
                debug_windowPos = ClickThruBlocker.GUILayoutWindow(baseWindowID + 2, debug_windowPos, DebugGUI, "FMRS Debug Info");
            }
            
#endif //**************************
        }


        /*************************************************************************************************************************/
        public void MainGUI(int windowID)
        {
            List<string> save_files = new List<string>();
            Vessel temp_vessel;
            string temp_string;
            Guid guid_delete_vessel = FlightGlobals.ActiveVessel.id;
            bool delete_vessel = false;
            float scrollbar_size;
            bool scrollbar_enable = false;
            float window_height, window_width, scrollbar_width_offset;

            window_height = 60;
            window_width = 280;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (!_SETTING_Minimize)
            {
                if (_SAVE_Has_Launched)
                    GUILayout.Box($"{Local_001}: " + get_time_string(Planetarium.GetUniversalTime() - _SAVE_Launched_At), text_main, GUILayout.Width(188)); // Mission Time
                else
                    GUILayout.Box($"{Local_001}: " + "00:00", text_main, GUILayout.Width(137)); // Mission Time
            }
            else
                GUILayout.Space(5);

            if (_SETTING_Armed)
                temp_string = Local_002;//"Armed"
            else
                temp_string = Local_003;//"Arm"

            if (!_SAVE_Has_Launched)
                _SETTING_Armed = GUILayout.Toggle(_SETTING_Armed, temp_string, button_small, GUILayout.Width(50));
            else
                if (_SETTING_Minimize)
                GUILayout.Box(Local_004, text_main, GUILayout.Width(50));//"Flight"

            if (!_SETTING_Minimize)
            {
                show_setting = GUILayout.Toggle(show_setting, buttonContent, button_small, GUILayout.Width(25));
                if (show_setting)
                    buttonContent = upContent;
                else
                    buttonContent = downContent;
            }

            _SETTING_Minimize = GUILayout.Toggle(_SETTING_Minimize, "_", button_small, GUILayout.Width(25));
            if (really_close && _SETTING_Minimize)
                _SETTING_Minimize = false;

            if (!_SETTING_Minimize)
                really_close = GUILayout.Toggle(really_close, "x", button_small, GUILayout.Width(25));
            else
                window_width = 105;

            GUILayout.EndHorizontal();

            if (really_close)
            {
                if (_SAVE_Has_Launched)
                {
                    GUILayout.Space(5);
                    window_height += 5;

                    GUILayout.Box(Local_005, text_heading, GUILayout.Width(266));//"Plugin will be reset!"
                    window_height += 29;
                    GUILayout.Box(Local_006, text_heading, GUILayout.Width(266));//"Close?"
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(Local_007, button_big, GUILayout.Width(132)))//"YES"
                        close_FMRS();

                    if (GUILayout.Button(Local_008, button_big, GUILayout.Width(133)))//"NO"
                        really_close = false;

                    GUILayout.EndHorizontal();
                    window_height += 58;
                }
                else
                    close_FMRS();
            }

            if (really_close == false && _SETTING_Minimize == false && show_setting == true)
            {
                GUILayout.Space(5);
                window_height += 5;
                GUILayout.BeginVertical( /* area_style ,*/ GUILayout.Width(266));
                GUILayout.Space((5 * 30) + 5);
                _SETTING_Messages = GUI.Toggle(new Rect(5, 35 + (30 * 1), 25, 25), _SETTING_Messages, Local_009);//"Messaging System"
                window_height += 30;
                _SETTING_Auto_Cut_Off = GUI.Toggle(new Rect(5, 35 + (30 * 2), 25, 25), _SETTING_Auto_Cut_Off, Local_010);//"Auto Cut Off Engines"
                window_height += 30;
                _SETTING_Auto_Recover = GUI.Toggle(new Rect(5, 35 + (30 * 3), 25, 25), _SETTING_Auto_Recover, Local_011);//"Auto Recover Landed Crafts"
                window_height += 30;
                _SETTING_Throttle_Log = GUI.Toggle(new Rect(5, 35 + (30 * 4), 25, 25), _SETTING_Throttle_Log, Local_012);//"Throttle Logger WIP"
                window_height += 30;
                Timer_Stage_Delay = GUI.HorizontalSlider(new Rect(45, 35 + (30 * 6) + 15, 205, 25), Timer_Stage_Delay, 0.2f, 5.0f);
                window_height += 45;
                GUI.Label(new Rect(20, 35 + (30 * 7), 225, 25), $"{Local_013}: " + Timer_Stage_Delay.ToString("F1"));//Stage Save Delay

#if DEBUG
                window_height += 30;
                Debug_Active = GUI.Toggle(new Rect(5, 35 + (30 * 5), 25, 25), Debug_Active, "write debug messages to log file");
#endif
                GUILayout.EndVertical();
                window_height += 42;
            }

            if (really_close == false && _SETTING_Minimize == false && show_setting == false)
            {
                if (Vessels_dropped.Count > 0)
                {
                    GUILayout.Space(5);
                    window_height += 5;

                    GUILayout.Box($"{Local_014}:", text_heading, GUILayout.Width(266));//Separated Stages
                    window_height += 33;

                    foreach (KeyValuePair<Guid, string> temp_keyvalue in Vessels_dropped)
                    {
                        if (!save_files.Contains(temp_keyvalue.Value))
                            save_files.Add(temp_keyvalue.Value);
                    }

                    save_files.Sort(delegate (string x, string y)
                    {
                        return get_save_value(save_cat.SAVEFILE, y).CompareTo(get_save_value(save_cat.SAVEFILE, x));
                    });

                    nr_save_files = save_files.Count;

                    scrollbar_size = nr_save_files * 61;
                    scrollbar_size += (Vessels_dropped.Count - nr_save_files) * 25;
                    if (_SAVE_Switched_To_Dropped && can_q_save_load)
                        scrollbar_size += 43;

                    if (scrollbar_size > 225)
                    {
                        scrollbar_enable = true;
                        scroll_Vector = GUILayout.BeginScrollView(scroll_Vector, scrollbar_stlye, GUILayout.Width(266), GUILayout.Height(225));
                        GUILayout.BeginVertical();
                        window_height += 220;
                        scrollbar_width_offset = 0;
                    }
                    else
                    {
                        GUILayout.BeginVertical(/* area_style, */ GUILayout.Width(266));
                        window_height += scrollbar_size;
                        scrollbar_width_offset = 20;
                    }

                    while (save_files.Count != 0)
                    {
                        GUILayout.Space(5);
                        GUILayout.BeginVertical(); //  area_style);
                        if (save_files.Last().Contains("separated_"))
                            GUILayout.Box(Localizer.Format("#FMRS_Local_015", get_time_string(Convert.ToDouble(get_save_value(save_cat.SAVEFILE, save_files.Last())) - _SAVE_Launched_At)), text_main, GUILayout.Width(230 + scrollbar_width_offset));//"Separated at " + get_time_string(Convert.ToDouble(get_save_value(save_cat.SAVEFILE, save_files.Last())) - _SAVE_Launched_At)
                        else
                            GUILayout.Box(Localizer.Format("#FMRS_Local_016", save_files.Last().Substring(10), get_time_string(Convert.ToDouble(get_save_value(save_cat.SAVEFILE, save_files.Last())) - _SAVE_Launched_At)), text_main, GUILayout.Width(230 + scrollbar_width_offset));//"Stage " + save_files.Last().Substring(10) + " separated at " + get_time_string(Convert.ToDouble(get_save_value(save_cat.SAVEFILE, save_files.Last())) - _SAVE_Launched_At)

                        foreach (KeyValuePair<Guid, string> vessel_in_savefile in Vessels_dropped)
                        {

                            if (FMRS_SAVE_Util.Instance.jumpInProgress)
                                GUI.enabled = false;
                            if (vessel_in_savefile.Value == save_files.Last())
                            {
                                GUILayout.BeginHorizontal();
                                if (get_vessel_state(vessel_in_savefile.Key) == vesselstate.RECOVERED)
                                {
                                    GUILayout.Box(Localizer.Format("#FMRS_Local_017", Vessels_dropped_names[vessel_in_savefile.Key]), text_cyan, GUILayout.Width(205 + scrollbar_width_offset));//Vessels_dropped_names[vessel_in_savefile.Key] + " recovered"
                                }
                                else if (get_vessel_state(vessel_in_savefile.Key) == vesselstate.LANDED)
                                {
                                    GUILayout.Box(Localizer.Format("#FMRS_Local_018", Vessels_dropped_names[vessel_in_savefile.Key]), text_green, GUILayout.Width(205 + scrollbar_width_offset));//Vessels_dropped_names[vessel_in_savefile.Key] + " landed"
                                }
                                else if (vessel_in_savefile.Key == FlightGlobals.ActiveVessel.id || vessel_in_savefile.Key == anz_id)
                                {
                                    float temp_float = 230 + scrollbar_width_offset;
                                    if (can_q_save_load)
                                    {
                                        GUILayout.EndHorizontal();
                                        GUILayout.Space(5);
                                        GUILayout.BeginVertical(/* area_style, */ GUILayout.Width(230));
                                        temp_float = 222 + scrollbar_width_offset;
                                    }
                                    if (FlightGlobals.ActiveVessel.LandedOrSplashed)
                                    {
                                        GUILayout.Box(Localizer.Format("#FMRS_Local_018", Vessels_dropped_names[vessel_in_savefile.Key]), text_green, GUILayout.Width(temp_float));//Vessels_dropped_names[vessel_in_savefile.Key] + " landed"
                                    }
                                    else
                                    {
                                        GUILayout.Box(Localizer.Format("#FMRS_Local_019", Vessels_dropped_names[vessel_in_savefile.Key]), text_yellow, GUILayout.Width(temp_float));//"contr.: " + Vessels_dropped_names[vessel_in_savefile.Key]
                                    }
                                    if (can_q_save_load)
                                    {
                                        if (GUILayout.Button(Local_020, button_main, GUILayout.Width(222 + scrollbar_width_offset)))//"Jump back to Separation"
                                            jump_to_vessel(vessel_in_savefile.Key, false);                                        

                                        GUILayout.EndVertical();
                                        GUILayout.Space(5);
                                        GUILayout.BeginHorizontal();
                                    }
                                }
                                else if (get_vessel_state(vessel_in_savefile.Key) == vesselstate.DESTROYED)
                                {
                                    GUILayout.Box(Localizer.Format("#FMRS_Local_021", Vessels_dropped_names[vessel_in_savefile.Key]), text_red, GUILayout.Width(205 + scrollbar_width_offset));//Vessels_dropped_names[vessel_in_savefile.Key] + " destroyed"
                                }
                                else
                                {
                                    temp_vessel = FlightGlobals.Vessels.Find(p => p.id == vessel_in_savefile.Key);

                                    if (temp_vessel == null)
                                    {
                                        if (GUILayout.Button(Vessels_dropped_names[vessel_in_savefile.Key], button_main, GUILayout.Width(205 + scrollbar_width_offset)))
                                            jump_to_vessel(vessel_in_savefile.Key, true);
                                    }
                                    else
                                    {
                                        if (loaded_vessels.Contains(temp_vessel.id) && _SAVE_Switched_To_Dropped)
                                        {
                                            if (temp_vessel.LandedOrSplashed)
                                            {
                                                if (GUILayout.Button(Localizer.Format("#FMRS_Local_018", Vessels_dropped_names[vessel_in_savefile.Key]), button_green, GUILayout.Width(205 + scrollbar_width_offset)))//Vessels_dropped_names[vessel_in_savefile.Key] + " landed"
                                                    FlightGlobals.ForceSetActiveVessel(temp_vessel);
                                            }
                                            else
                                            {
                                                if (GUILayout.Button(Localizer.Format("#FMRS_Local_022", Vessels_dropped_names[vessel_in_savefile.Key]), button_yellow, GUILayout.Width(205 + scrollbar_width_offset)))//Vessels_dropped_names[vessel_in_savefile.Key] + " is near"
                                                    FlightGlobals.ForceSetActiveVessel(temp_vessel);
                                            }
                                        }
                                        else
                                        {
                                            if (GUILayout.Button(Vessels_dropped_names[vessel_in_savefile.Key], button_main, GUILayout.Width(205 + scrollbar_width_offset)))
                                                jump_to_vessel(vessel_in_savefile.Key, true);
                                        }
                                    }
                                }

                                if (vessel_in_savefile.Key != FlightGlobals.ActiveVessel.id && vessel_in_savefile.Key != anz_id)
                                {
                                    if (GUILayout.Button("X", button_small_red, GUILayout.Width(25)))
                                    {
                                        guid_delete_vessel = vessel_in_savefile.Key;
                                        delete_vessel = true;
                                    }
                                }
                                GUILayout.EndHorizontal();
                                button_main.normal.textColor = button_main.focused.textColor = Color.white;
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndVertical();
                        temp_string = save_files.Last();
                        save_files.Remove(temp_string);
                    }

                    if (scrollbar_enable)
                    {
                        GUILayout.EndVertical();
                        GUILayout.EndScrollView();
                    }
                    else
                        GUILayout.EndVertical();
                }

                if (_SAVE_Switched_To_Dropped)
                {
                    GUILayout.Space(5);
                    window_height += 5;
                    if (FMRS_SAVE_Util.Instance.jumpInProgress)
                        GUI.enabled = false;
                    if (GUILayout.Button(Local_023, button_big, GUILayout.Width(266)))//"Jump back to Main Mission"
                    {
                        jump_to_vessel("Main");
                    }
                    GUI.enabled = true;
                    window_height += 31;
                }

                if (_SAVE_Has_Launched && can_restart)
                {
                    GUILayout.Space(5);
                    window_height += 5;
                    if (FMRS_SAVE_Util.Instance.jumpInProgress)
                        GUI.enabled = false;
                    if (revert_to_launch)
                    {
                        GUILayout.Box(Local_024, text_heading, GUILayout.Width(266));//"Revert Flight?"

                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button(Local_007, button_big, GUILayout.Width(132)))//"YES"
                        {
                           // _SETTING_Enabled = false;
                            jump_to_vessel(_SAVE_Main_Vessel, "before_launch");
                        }
                        if (GUILayout.Button(Local_008, button_big, GUILayout.Width(133)))//"NO"
                            revert_to_launch = false;

                        GUILayout.EndHorizontal();
                        window_height += 58;
                    }
                    else
                    {
                        if (_SAVE_Flight_Reset)
                            revert_to_launch = GUILayout.Toggle(revert_to_launch, Local_025, button_big, GUILayout.Width(266));//"Revert To Plugin Start"
                        else
                            revert_to_launch = GUILayout.Toggle(revert_to_launch, Local_026, button_big, GUILayout.Width(266));//"Revert To Launch"
                        window_height += 31;
                    }
                    GUI.enabled = true;
                }
            }
            GUILayout.EndVertical();

            if (delete_vessel && guid_delete_vessel != FlightGlobals.ActiveVessel.id)
                delete_dropped_vessel(guid_delete_vessel);

            windowPos.height = window_height;
            //   windowPos.width = window_width;

            GUI.DragWindow();
        }


#if DEBUG
        /*************************************************************************************************************************/
        public void DebugGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            //GUI.Toggle(new Rect(5, 3, 25, 25), plugin_active, " ");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("print savefile", button_small, GUILayout.Width(132)))
                write_save_values_to_file();
            if (GUILayout.Button("read savefile", button_small, GUILayout.Width(132)))
                load_save_file();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Debug_Level_1_Active = GUILayout.Toggle(Debug_Level_1_Active, "debug lv 1", button_small, GUILayout.Width(132));
            Debug_Level_2_Active = GUILayout.Toggle(Debug_Level_2_Active, "debug lv 2", button_small, GUILayout.Width(132));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("mark bug", button_small, GUILayout.Width(115)))
                Log.Info("##################### BUG MARKER #####################");

            GUILayout.Space(5);

            debug_message[0] = _SAVE_Switched_To_Savefile;
            debug_message[1] = loaded_vessels.Count.ToString();
            //debug_message[2] = "";
            //debug_message[3] = "";
            //debug_message[4] = "";
            //debug_message[5] = "";
            //debug_message[6] = "";
            //debug_message[7] = "";
            //debug_message[8] = "";
            //debug_message[9] = "";
            //debug_message[10] = "";
            //debug_message[11] = "";
            //debug_message[12] = "";
            //debug_message[13] = "";
            //debug_message[14] = "";
            //debug_message[15] = "";
            //debug_message[16] = "";
            //debug_message[17] = "";
            //debug_message[18] = "";
            //debug_message[19] = "";

            foreach (string temp_string in debug_message)
                if (temp_string != "")
                    GUILayout.Box(temp_string, text_main, GUILayout.Width(266));

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
#endif

#if BETA && !DEBUG
/*************************************************************************************************************************/
        public void BetaGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("mark bug", button_small, GUILayout.Width(115)))
                Log.Info("##################### BUG MARKER #####################");

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
#endif


        /*************************************************************************************************************************/
        private void init_skin()
        {
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PushStackInfo("FMRS_Core.init_skin", "enter init_skin()");
            if (Debug_Active)
                Log.Info("init_skin");
#endif
            GUIStyle MyButton = new GUIStyle(HighLogic.Skin.button);
            GUIStyle MyTextArea = new GUIStyle(HighLogic.Skin.textArea);
            GUIStyle MyScrollView = new GUIStyle(HighLogic.Skin.scrollView);

            MyButton.fontSize = 15;
            MyTextArea.fontSize = 15;
            MyButton.normal.textColor = Color.white;
            MyButton.hover.textColor = Color.yellow;
            MyButton.onNormal.textColor = Color.green;
            MyButton.padding = new RectOffset(5, 3, 3, 3);
            MyButton.border = new RectOffset(3, 3, 3, 3);
            MyButton.margin = new RectOffset(1, 1, 1, 1);
            MyButton.overflow = new RectOffset(1, 1, 1, 1);
            MyButton.alignment = TextAnchor.MiddleLeft;
            MyButton.wordWrap = false;
            MyButton.clipping = TextClipping.Clip;

            MyTextArea.padding = new RectOffset(3, 3, 4, 2);
            MyTextArea.border = new RectOffset(3, 3, 3, 3);
            MyTextArea.margin = new RectOffset(1, 1, 1, 1);
            MyTextArea.overflow = new RectOffset(1, 1, 1, 1);
            MyTextArea.alignment = TextAnchor.MiddleLeft;
            MyTextArea.wordWrap = false;
            MyTextArea.clipping = TextClipping.Clip;

            button_main = new GUIStyle(MyButton);
            button_green = new GUIStyle(MyButton);
            button_green.normal.textColor = button_green.focused.textColor = Color.green;
            button_red = new GUIStyle(MyButton);
            button_red.normal.textColor = button_red.focused.textColor = Color.red;
            button_yellow = new GUIStyle(MyButton);
            button_yellow.normal.textColor = button_yellow.focused.textColor = Color.yellow;
            button_small = new GUIStyle(MyButton);
            button_small.padding = new RectOffset(2, 3, 3, 3);
            button_small.alignment = TextAnchor.MiddleCenter;
            button_small_red = new GUIStyle(button_small);
            button_small_red.normal.textColor = button_small_red.focused.textColor = Color.red;

            button_big = new GUIStyle(MyButton);
            button_big.padding = new RectOffset(6, 6, 6, 6);
            button_big.alignment = TextAnchor.MiddleCenter;

            text_main = new GUIStyle(MyTextArea);
            text_green = new GUIStyle(MyTextArea);
            text_green.normal.textColor = Color.green;
            text_cyan = new GUIStyle(MyTextArea);
            text_cyan.normal.textColor = Color.cyan;
            text_red = new GUIStyle(MyTextArea);
            text_red.normal.textColor = Color.red;
            text_yellow = new GUIStyle(MyTextArea);
            text_yellow.normal.textColor = Color.yellow;
            text_heading = new GUIStyle(MyTextArea);
            text_heading.fontSize = 16;
            text_heading.fontStyle = FontStyle.Bold;
            text_heading.alignment = TextAnchor.MiddleCenter;

            area_style = new GUIStyle(MyTextArea);
            area_style.active = area_style.hover = area_style.normal;

            scrollbar_stlye = new GUIStyle(MyScrollView);
            scrollbar_stlye.padding = new RectOffset(3, 3, 3, 3);
            scrollbar_stlye.border = new RectOffset(3, 3, 3, 3);
            scrollbar_stlye.margin = new RectOffset(1, 1, 1, 1);
            scrollbar_stlye.overflow = new RectOffset(1, 1, 1, 1);

            skin_init = true;
#if DEBUG
            //if (Debug_Level_1_Active)
            Log.PopStackInfo("leave init_skin()");
#endif
        }
    }
}
