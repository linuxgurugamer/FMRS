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

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace FMRS
{

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class FMRS_SAVE_Util : MonoBehaviour
    {
        bool saveInProgress = false;
        bool readyToLoad = false;
        Game gameToLoad = null;
        int vesselToFocus = 0;

        public bool jumpInProgress { get; private set; }

        public static FMRS_SAVE_Util Instance;

        public void Wait(float seconds, Action action)
        {
            Log.Info("Wait, seconds: " + seconds.ToString());
            jumpInProgress = true;
            StartCoroutine(_wait(seconds, action));
        }

        IEnumerator _wait(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            jumpInProgress = false;
            callback();
        }

         private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Instance = this;
            jumpInProgress = false;
            GameEvents.onGameStateSaved.Add(OnGameStateSaved);
            jumpInProgress = false;
            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            GameEvents.onGameStateSaved.Remove(OnGameStateSaved);
        }

        public string SaveGame(string func, Game game, string saveFileName, string saveFolder, SaveMode saveMode)
        {
            saveInProgress = true;
            Log.Info("Save: SaveGame 1: " + func);
            return GamePersistence.SaveGame(game, saveFileName, saveFolder, saveMode);
        }

        public string SaveGame(string func, string saveFileName, string saveFolder, SaveMode saveMode)
        {
            saveInProgress = true;
            Log.Info("Save: SaveGame 2: " + func);

            return GamePersistence.SaveGame(saveFileName, saveFolder, saveMode);
        }


        public void StartAndFocusVessel(Game stateToLoad, int vesselToFocusIdx)
        {
            Log.Info("Save: StartAndFocusVessel, saveInprogress: " + saveInProgress.ToString());
           if (saveInProgress)
         // if (false)
            {
                readyToLoad = true;
                gameToLoad = stateToLoad;
                vesselToFocus = vesselToFocusIdx;
            }
            else
            {
                gameToLoad = stateToLoad;
                vesselToFocus = vesselToFocusIdx;
               // detach_handlers();
                Wait(1, () => {
                    doStartAndFocusVessel();
                   // Debug.Log("1 second is lost forever");
                });
                //Log.Info("Save: Calling FlightDriver.StartAndFocusVessel 1");
                //FlightDriver.StartAndFocusVessel(stateToLoad, vesselToFocusIdx);
            }
        }

        void doStartAndFocusVessel()
        {
            Log.Info("Save: doStartAndFocusVessel, Calling FlightDriver.StartAndFocusVessel 1");
            FlightDriver.StartAndFocusVessel(gameToLoad, vesselToFocus);
            readyToLoad = false;
            gameToLoad = null;
            //attach_handlers();
        }

        void OnGameStateSaved(Game game)
        {
            Log.Info("Save: OnGameStateSaved, readyToLoad: " + readyToLoad.ToString());
            saveInProgress = false;
            if (gameToLoad != null && readyToLoad)
            {
                if (gameToLoad != null)
                {
                    Wait(1, () => {
                        doStartAndFocusVessel();
                       // Debug.Log("1 second is lost forever");
                    });
                    Log.Info("Save: Calling FlightDriver.StartAndFocusVessel 2");

                   // FlightDriver.StartAndFocusVessel(gameToLoad, vesselToFocus);
                   // readyToLoad = false;
                   // gameToLoad = null;
                }
            }
        }
    }

    public class FMRS_Util : MonoBehaviour
    {
        private string mod_version;
        public const string gamesave_name = "FMRS_save_";

        public struct recover_value
        {
            public string cat;
            public string key;
            public string value;
        }

        public enum save_cat : int { SETTING = 1, SAVE, SAVEFILE, SUBSAVEFILE, DROPPED, NAME, STATE, KERBAL_DROPPED, UNDEF };
        public enum vesselstate : int {NONE = 1, FLY, LANDED, DESTROYED, RECOVERED }

        public List<Guid> Vessels = new List<Guid>();
        public Dictionary<Guid, string> Vessels_dropped = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> Vessels_dropped_names = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> Vessel_sub_save = new Dictionary<Guid, string>();
        public Dictionary<Guid, vesselstate> Vessel_State = new Dictionary<Guid, vesselstate>();
        public Dictionary<String, Guid> Kerbal_dropped = new Dictionary<string, Guid>();
        public List<recover_value> recover_values = new List<recover_value>();
#if DEBUG
        public static bool Debug_Active = true;
        public static bool Debug_Level_1_Active = true;
        public static bool Debug_Level_2_Active = true;
#else
        //public bool Debug_Active = false;
        //public bool Debug_Level_1_Active = false;
        //public bool Debug_Level_2_Active = false;
#endif
        public bool bflush_save_file = false;
        
        public Dictionary<save_cat, Dictionary<string, string>> Save_File_Content = new Dictionary<save_cat, Dictionary<string, string>>();
        public Rect windowPos;
        public Guid _SAVE_Main_Vessel;
        public string _SAVE_Switched_To_Savefile, _SAVE_SaveFolder;
        public bool _SETTING_Enabled, _SETTING_Armed, _SETTING_Minimize, _SAVE_Has_Launched, _SAVE_Flight_Reset, _SAVE_Kick_To_Main, _SAVE_Switched_To_Dropped;
        public static bool _SETTING_Messages, _SETTING_Auto_Cut_Off, _SETTING_Auto_Recover, _SETTING_Throttle_Log, _SETTING_Parachutes, _SETTING_Defer_Parachutes_to_StageRecovery,
            _SETTING_Control_Uncontrollable;
        public static bool stageRecoveryInstalled = false;

        public double _SAVE_Launched_At;

#if DEBUG
        public string mod_vers
        {
            get { return mod_version; }
            set { mod_version = value; }
        }
#endif
        /*************************************************************************************************************************/
        private void Start()
        {


        }
    public void set_save_value(save_cat cat, string key, string value)
        {
#if DEBUG
            if (Debug_Level_2_Active)   Log.Info("entering set_save_value(int cat, string key, string value)");
#endif

            if (Save_File_Content[cat].ContainsKey(key))
            {
                Save_File_Content[cat][key] = value;
            }
            else
            {
                Save_File_Content[cat].Add(key, value);
            }
#if DEBUG
            if (Debug_Active) Log.Info("set_save_value: " + key + " = " + value);

            if (Debug_Level_2_Active) Log.Info("leaving set_save_value(int cat, string key, string value)");
#endif
        }


/*************************************************************************************************************************/
        public string get_save_value(save_cat cat, string key)
        {
#if DEBUG
            if (Debug_Level_2_Active)
                Log.Info("entering get_save_value(int cat, string key) #### FMRS: NO LEAVE MESSAGE");
#endif

            if (Save_File_Content[cat].ContainsKey(key))
                return (Save_File_Content[cat][key]);
            else
                return (false.ToString());
        }


/*************************************************************************************************************************/
        public void write_save_values_to_file()
        {
            Vessel dummy_vessel = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.write_save_values_to_file", "entering write_save_values_to_file()");
#endif

            set_save_value(save_cat.SETTING, "Window_X", Convert.ToInt32(windowPos.x).ToString());
            set_save_value(save_cat.SETTING, "Window_Y", Convert.ToInt32(windowPos.y).ToString());
            set_save_value(save_cat.SETTING, "Armed", _SETTING_Armed.ToString());
            set_save_value(save_cat.SETTING, "Minimized", _SETTING_Minimize.ToString());
            set_save_value(save_cat.SETTING, "Enabled", _SETTING_Enabled.ToString());
            //set_save_value(save_cat.SETTING, "Messages", _SETTING_Messages.ToString());
            //set_save_value(save_cat.SETTING, "Auto_Cut_Off", _SETTING_Auto_Cut_Off.ToString());
            //set_save_value(save_cat.SETTING, "Auto_Recover", _SETTING_Auto_Recover.ToString());
            //set_save_value(save_cat.SETTING, "Throttle_Log", _SETTING_Throttle_Log.ToString());
#if DEBUG
            set_save_value(save_cat.SETTING, "Debug", Debug_Active.ToString());
#endif
            set_save_value(save_cat.SAVE, "Main_Vessel", _SAVE_Main_Vessel.ToString());
            set_save_value(save_cat.SAVE, "Has_Launched", _SAVE_Has_Launched.ToString());
            set_save_value(save_cat.SAVE, "Launched_At", _SAVE_Launched_At.ToString());
            set_save_value(save_cat.SAVE, "Flight_Reset", _SAVE_Flight_Reset.ToString());
            set_save_value(save_cat.SAVE, "Kick_To_Main", _SAVE_Kick_To_Main.ToString());
            set_save_value(save_cat.SAVE, "Switched_To_Dropped", _SAVE_Switched_To_Dropped.ToString());
            set_save_value(save_cat.SAVE, "Switched_To_Savefile", _SAVE_Switched_To_Savefile);
            set_save_value(save_cat.SAVE, "SaveFolder", _SAVE_SaveFolder);

            write_vessel_dict_to_Save_File_Content();
            
            TextWriter file = File.CreateText<FMRS>("save.txt", dummy_vessel);
            file.Flush();
            file.Close();
            file = File.CreateText<FMRS>("save.txt", dummy_vessel);
            foreach (KeyValuePair<save_cat, Dictionary<string, string>> save_cat_block in Save_File_Content)
            {
                foreach (KeyValuePair<string, string> writevalue in save_cat_block.Value)
                {
                   file.WriteLine(save_cat_toString(save_cat_block.Key) + "=" + writevalue.Key + "=" + writevalue.Value);
                }
            }
            file.Close();
#if DEBUG
            if (Debug_Active) Log.Info("Save File written in private void write_save_values_to_file()");
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving save_values_to_file()");
#endif
        }


/*************************************************************************************************************************/
        public void write_vessel_dict_to_Save_File_Content()
        {
            List<string> delete_values = new List<string>();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.write_vessel_dict_to_Save_File_Content", "entering write_vessel_dict_to_Save_File_Content()");
#endif

            Save_File_Content[save_cat.DROPPED].Clear();
            Save_File_Content[save_cat.NAME].Clear();
            Save_File_Content[save_cat.STATE].Clear();
            
            foreach (KeyValuePair<Guid, string> write_keyvalue in Vessels_dropped)
            {
                set_save_value(save_cat.DROPPED, write_keyvalue.Key.ToString(), write_keyvalue.Value);
#if DEBUG
                if (Debug_Active)  Log.Info("write " + write_keyvalue.Key.ToString() + " to Save_File_Content DROPPED");
#endif
            }

            foreach (KeyValuePair<Guid, string> write_keyvalue in Vessels_dropped_names)
            {
                set_save_value(save_cat.NAME, write_keyvalue.Key.ToString(), write_keyvalue.Value);
#if DEBUG
                if (Debug_Active) Log.Info("write NAME " + write_keyvalue.Key.ToString() + " to Save_File_Content NAME");
#endif
            }

            foreach (KeyValuePair<Guid,vesselstate> st  in Vessel_State)
            {
                set_save_value(save_cat.STATE, st.Key.ToString(), st.Value.ToString());
#if DEBUG
                if (Debug_Active)  Log.Info("write " + st.Key.ToString() + ": " + st.Value.ToString() + " to Save_File_Content STATE");
#endif
            }

            foreach (KeyValuePair<Guid, string> st in Vessel_sub_save)
            {
                set_save_value(save_cat.SUBSAVEFILE, st.Key.ToString(), st.Value.ToString());
#if DEBUG
                if (Debug_Active) Log.Info("write " + st.Key.ToString() + ": " + st.Value.ToString() + " to Save_File_Content SUBSAVEFILE");
#endif
            }

            foreach (KeyValuePair<string, Guid> write_keyvalue in Kerbal_dropped)
            {
                set_save_value(save_cat.KERBAL_DROPPED, write_keyvalue.Key.ToString(), write_keyvalue.Value.ToString());
#if DEBUG
                if (Debug_Active)    Log.Info("write KERBAL_DROPPED " + write_keyvalue.Key.ToString() + " to Save_File_Content KERBAL_DROPPED");
#endif
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving write_vessel_dict_to_Save_File_Content()");
#endif
        }


/*************************************************************************************************************************/
        public string get_time_string(double dtime)
        {
            // Guard against inputs that overflow the Int32 conversion below.
            // dtime is supplied by callers as elapsed game seconds, typically
            // (Planetarium.GetUniversalTime() - _SAVE_Launched_At). For
            // sufficiently long missions, or under heavy time warp (100,000x
            // and above), this delta exceeds Int32.MaxValue / 100 (~21.47M
            // seconds, ~248 game-days). The previous Convert.ToInt32(dtime *
            // 100) then threw OverflowException, and because this method is
            // called from FMRS_Core.MainGUI on every GUI frame, the same
            // exception was logged at ~60 Hz, producing very large Player.log
            // files and visible frame-time stutters at high warp.
            if (double.IsNaN(dtime) || double.IsInfinity(dtime) || dtime < 0)
                return "00:00.0";

            // Use long throughout so the multiply-by-100 is safe for any
            // realistically representable mission time. Replace the original
            // O(N) while-loop decomposition with O(1) integer division so
            // very large dtime values do not also burn CPU on the GUI thread.
            long time = (long)(dtime * 100);   // deciseconds

            long min = time / 6000;
            long rem = time % 6000;
            long s = rem / 100;
            long ds = (rem % 100) / 10;

            string return_string;

            if (min < 10)
                return_string = "0" + min.ToString();
            else
                return_string = min.ToString();

            return_string += ":";

            if (s < 10)
                return_string += "0" + s.ToString();
            else
                return_string += s.ToString();

            return_string += "." + ds.ToString();

            return (return_string);
        }


/*************************************************************************************************************************/
        public void flush_save_file()
        {
            Vessel dummy_vessel = null;
            int anz_lines;
#if DEBUG
          //  if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.flush_save_file", "enter flush_save_file()");

            if (Debug_Active) Log.Info("flush save file");
#endif
            string[] lines = File.ReadAllLines<FMRS>("save.txt", dummy_vessel);
            anz_lines = lines.Length;

#if DEBUG
            if (Debug_Active) Log.Info("delete " + anz_lines.ToString() + " lines");
#endif

            TextWriter file = File.CreateText<FMRS>("save.txt", dummy_vessel);
            while (anz_lines != 0)
            {
                file.WriteLine("");
                anz_lines--;
            }
            file.Close();

            foreach (KeyValuePair<save_cat, Dictionary<string, string>> content in Save_File_Content)
                Save_File_Content[content.Key].Clear();

            bflush_save_file = false;
            init_save_file();
            read_save_file();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave flush_save_file()");
#endif
        }


/*************************************************************************************************************************/
        public void read_save_file()
        {
            Vessel dummy_vessel = null;
            double temp_double;
            save_cat temp_cat;

#if DEBUG
           // if (Debug_Level_1_Active) 
                Log.PushStackInfo("FMRS_Util.read_save_file", "enter read_save_file()");
				
			if (Debug_Active)Log.Info("read save file");
#endif
            foreach(KeyValuePair<save_cat,Dictionary<string,string>> content in Save_File_Content)
                Save_File_Content[content.Key].Clear();

            string[] lines = File.ReadAllLines<FMRS>("save.txt", dummy_vessel);

            foreach (string value_string in lines)
            {
                if (value_string != "")
                {
                    string[] line = value_string.Split('=');
                    temp_cat = save_cat_parse(line[0]);

                    try
                    {
                        Save_File_Content[temp_cat].Add(line[1].Trim(), line[2].Trim());
                    }
                    catch (Exception)
                    {
#if DEBUG
                    //    Debug_Active = true;
                    //    Debug_Level_1_Active = true;
#endif
                        Log.Info("inconsistent save file, flush save file");
                        bflush_save_file = true;
                        break;
                    }
                }
            }
            if (bflush_save_file)
                return;

            try
            {
#if DEBUG
                Debug_Active = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Debug"));
#endif

                temp_double = Convert.ToDouble(get_save_value(save_cat.SETTING, "Window_X"));
                windowPos.x = Convert.ToInt32(temp_double);
                temp_double = Convert.ToDouble(get_save_value(save_cat.SETTING, "Window_Y"));
                windowPos.y = Convert.ToInt32(temp_double);
                Debug.Log("windowPos loaded from file");
            }
            catch (Exception)
            {
#if false
                // DEBUG
                Debug_Active = true;
                Debug_Level_1_Active = true;
#endif
                Log.Info("invalid save file, flush save file");
                bflush_save_file = true;
            }
#if DEBUG
            if (Debug_Active)
                foreach (KeyValuePair<save_cat, Dictionary<string, string>> temp_keyvalue in Save_File_Content)
                    foreach (KeyValuePair<string, string> readvalue in temp_keyvalue.Value)
                        Log.Info("" + temp_keyvalue.Key.ToString() + " = " + readvalue.Key + " = " + readvalue.Value);
#endif

            if (bflush_save_file)
                return;
#if false
            // DEBUG
            if (get_save_value(save_cat.SETTING,"Debug_Level") == "1" && Debug_Active)
                Debug_Level_1_Active = true;

            if (get_save_value(save_cat.SETTING,"Debug_Level") == "2" && Debug_Active)
            {
                Debug_Level_1_Active = true;
                Debug_Level_2_Active = true;
            }
#endif
#if BETA && DEBUG //**************************
          //  Debug_Active = true;
          //  Debug_Level_1_Active = true;
#endif //**************************

            try
            {
                _SETTING_Armed = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Armed"));
                _SETTING_Minimize = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Minimized"));
                _SETTING_Enabled = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Enabled"));
                //_SETTING_Messages = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Messages"));
                //_SETTING_Auto_Cut_Off = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Auto_Cut_Off"));
                //_SETTING_Auto_Recover = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Auto_Recover"));
                //_SETTING_Throttle_Log = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Throttle_Log"));
                _SAVE_Main_Vessel = new Guid(get_save_value(save_cat.SAVE, "Main_Vessel"));
                _SAVE_Has_Launched = Convert.ToBoolean(get_save_value(save_cat.SAVE, "Has_Launched"));
                _SAVE_Launched_At = Convert.ToDouble(get_save_value(save_cat.SAVE, "Launched_At"));
                _SAVE_Flight_Reset = Convert.ToBoolean(get_save_value(save_cat.SAVE, "Flight_Reset"));
                _SAVE_Kick_To_Main = Convert.ToBoolean(get_save_value(save_cat.SAVE, "Kick_To_Main"));
                _SAVE_Switched_To_Dropped = Convert.ToBoolean(get_save_value(save_cat.SAVE, "Switched_To_Dropped"));
                _SAVE_Switched_To_Savefile = get_save_value(save_cat.SAVE, "Switched_To_Savefile");
                _SAVE_SaveFolder = get_save_value(save_cat.SAVE, "SaveFolder");
            }
            catch (Exception)
            {
#if DEBUG
            //    Debug_Active = true;
            //    Debug_Level_1_Active = true;
#endif
                Log.Info("invalid save file, flush save file");
                bflush_save_file = true;
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave read_save_file()");
#endif
        }


/*************************************************************************************************************************/
        public void init_save_file()
        {
            Vessel dummy_vessel = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.init_save_file", "enter init_save_file()");

            if (Debug_Active)  Log.Info("init save file");
#endif

            foreach (KeyValuePair<save_cat, Dictionary<string, string>> content in Save_File_Content)
                Save_File_Content[content.Key].Clear();

#if DEBUG
            set_save_value(save_cat.SETTING,"Version", mod_version);
#endif
            set_save_value(save_cat.SETTING, "Window_X", Convert.ToInt32(windowPos.x).ToString());
            set_save_value(save_cat.SETTING, "Window_Y", Convert.ToInt32(windowPos.y).ToString());
            set_save_value(save_cat.SETTING, "Enabled", true.ToString());
            set_save_value(save_cat.SETTING, "Armed", true.ToString());
            set_save_value(save_cat.SETTING, "Minimized", false.ToString());
            set_save_value(save_cat.SETTING, "Messages", true.ToString());
            set_save_value(save_cat.SETTING, "Auto_Cut_Off", false.ToString());
            set_save_value(save_cat.SETTING, "Auto_Recover", false.ToString());
            set_save_value(save_cat.SETTING, "Throttle_Log", false.ToString());
            set_save_value(save_cat.SETTING, "Debug", false.ToString());
            set_save_value(save_cat.SETTING, "Debug_Level", "0");
            set_save_value(save_cat.SAVE, "Main_Vessel", new Guid().ToString());          
            set_save_value(save_cat.SAVE, "Has_Launched", false.ToString());
            set_save_value(save_cat.SAVE, "Launched_At", "null");
            set_save_value(save_cat.SAVE, "Flight_Reset", false.ToString());
            set_save_value(save_cat.SAVE, "Switched_To_Dropped", false.ToString());
            set_save_value(save_cat.SAVE, "Kick_To_Main", false.ToString());
            set_save_value(save_cat.SAVE, "Switched_To_Savefile", "");
            set_save_value(save_cat.SAVE, "SaveFolder", HighLogic.SaveFolder);


#if DEBUG //**************************
           // set_save_value(save_cat.SETTING,"Debug", true.ToString());
           // set_save_value(save_cat.SETTING, "Debug_Level", "0");
#endif //**************************

#if DEBUG
            Debug_Active = Convert.ToBoolean(get_save_value(save_cat.SETTING, "Debug"));
#endif

            TextWriter file = File.CreateText<FMRS>("save.txt", dummy_vessel);

            foreach (KeyValuePair<save_cat, Dictionary<string, string>> writecat in Save_File_Content)
                foreach (KeyValuePair<string, string> writevalue in writecat.Value)
                    file.WriteLine(writecat.Key.ToString() + "=" + writevalue.Key + "=" + writevalue.Value);

            file.Close();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave init_save_file()");
#endif
        }


/*************************************************************************************************************************/
        public void load_save_file()
        {
            Vessel dummy_vessel = null;

            if (!File.Exists<FMRS>("save.txt", dummy_vessel))
                init_save_file();
            read_save_file();

#if false
            if (!bflush_save_file)
                if (get_save_value(save_cat.SETTING, "Version") != mod_version)
                {
                    Log.Info("diferent version, flush save file");
                    bflush_save_file = true;
                }
#endif
            if (bflush_save_file)
                flush_save_file();

            if (!File.Exists<FMRS>("recover.txt", dummy_vessel))
                init_recover_file();
            read_recover_file();

            get_dropped_vessels();
        }


/*************************************************************************************************************************/
        public void get_dropped_vessels()
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.get_dropped_vessels", "entering get_dropped_vessels()");
#endif

            foreach (KeyValuePair<save_cat, Dictionary<string, string>> savecat in Save_File_Content)
            {
                if (savecat.Key == save_cat.DROPPED)
                    foreach(KeyValuePair<string,string> save_value in savecat.Value)
                    {
                        Vessels_dropped.Add(new Guid(save_value.Key), save_value.Value);
#if DEBUG
                        if (Debug_Active)  Debug.Log(" #### FMRS: " + save_value.Key.ToString() + " set to " + save_value.Value + " in Vessels_dropped");
#endif
                    }

                if (savecat.Key == save_cat.NAME)
                    foreach (KeyValuePair<string, string> save_value in savecat.Value)
                    {
                        Vessels_dropped_names.Add(new Guid(save_value.Key), save_value.Value);
#if DEBUG
                        if (Debug_Active)   Debug.Log(" #### FMRS: " + save_value.Key.ToString() + " set to " + save_value.Value + " in Vessels_dropped_names");
#endif
                    }

                if (savecat.Key == save_cat.STATE)
                    foreach (KeyValuePair<string, string> save_value in savecat.Value)
                    {
                        Vessel_State.Add(new Guid(save_value.Key), parse_vesselstate(save_value.Value));
#if DEBUG
                        if (Debug_Active)   Debug.Log(" #### FMRS: " + save_value.Key.ToString() + " set to " + save_value.Value + " in Vessels_dropped_landed");
#endif
                    }

                if (savecat.Key == save_cat.SUBSAVEFILE)
                    foreach (KeyValuePair<string, string> save_value in savecat.Value)
                    {
                        Vessel_sub_save.Add(new Guid(save_value.Key), save_value.Value);
#if DEBUG
                        if (Debug_Active)  Debug.Log(" #### FMRS: " + save_value.Key.ToString() + " set to " + save_value.Value + " in Vessel_sub_save");
#endif
                    }

                if (savecat.Key == save_cat.KERBAL_DROPPED)
                    foreach (KeyValuePair<string, string> save_value in savecat.Value)
                    {
                        Kerbal_dropped.Add(save_value.Key, new Guid(save_value.Value));
#if DEBUG
                        if (Debug_Active) Debug.Log(" #### FMRS: " + save_value.Key + " set to " + save_value.Value + " in Kerbal_dropped");
#endif
                    }
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving get_dropped_vessels()");
#endif
        }


/*************************************************************************************************************************/
        public void init_recover_file()
        {
            Vessel dummy_vessel = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.init_recover_file", "enter init_recover_file()");
            if (Debug_Active)  Log.Info("init recover file");
#endif
            TextWriter file = File.CreateText<FMRS>("recover.txt", dummy_vessel);
            file.Close();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave init_recover_file()");
#endif
        }


/*************************************************************************************************************************/
        public void flush_recover_file()
        {
            int anz_lines;
            Vessel dummy_vessel = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("fMRS_Util.flush_recover_file", "enter flush_recover_file()");
            if (Debug_Active)  Log.Info("flush recover file");
#endif
            string[] lines = File.ReadAllLines<FMRS>("recover.txt", dummy_vessel);
            anz_lines = lines.Length;

            TextWriter file = File.CreateText<FMRS>("recover.txt", dummy_vessel);
            while (anz_lines != 0)
            {
                file.WriteLine("");
                anz_lines--;
            }
            file.Close();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave flush_recover_file()");
#endif
        }


/*************************************************************************************************************************/
        public void read_recover_file()
        {
            Vessel dummy_vessel = null;
            recover_value temp_value;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.read_recover_file", "enter read_recover_file()");
            if (Debug_Active)  Log.Info("read recover file");
#endif
            recover_values.Clear();
            string[] lines = File.ReadAllLines<FMRS>("recover.txt", dummy_vessel);

            foreach (string value_string in lines)
            {
                if (value_string != "")
                {
                    string[] line = value_string.Split('=');
                    temp_value.cat = line[0].Trim();
                    temp_value.key = line[1].Trim();
                    temp_value.value = line[2].Trim();
                    recover_values.Add(temp_value);
                }
            }
#if DEBUG
            if (Debug_Active)
                foreach (recover_value temp in recover_values)
                    Log.Info("recover value: " + temp.cat + " = " + temp.key + " = " + temp.value);

           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave read_recover_file()");
#endif
        }


/*************************************************************************************************************************/
        public void write_recover_file()
        {
            Vessel dummy_vessel = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.write_recover_file", "enter write_recover_file()");
#endif
            flush_recover_file();
#if DEBUG
            if (Debug_Active)  Log.Info("write recover file");
#endif
            TextWriter file = File.CreateText<FMRS>("recover.txt", dummy_vessel);

            foreach (recover_value writevalue in recover_values)
            {
                file.WriteLine(writevalue.cat + "=" + writevalue.key + "=" + writevalue.value);
            }
            file.Close();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave write_recover_file()");
#endif
        }


/*************************************************************************************************************************/
        public void set_recoverd_value(string cat, string key, string value)
        {
            recover_value temp_value;
#if DEBUG
            if (Debug_Level_2_Active)
                Log.Info("enter set_recoverd_value(string key, string value)");
            if (Debug_Active) Log.Info("add to recover file: " + cat + " = " + key + " = " + value);
#endif

            temp_value.cat = cat;
            temp_value.key = key;
            temp_value.value = value;
            recover_values.Add(temp_value);
#if DEBUG
            if (Debug_Level_2_Active)  Log.Info("set_recoverd_value(string key, string value)");
#endif
        }


/*************************************************************************************************************************/
        public save_cat save_cat_parse(string in_string)
        {
#if DEBUG
            if (Debug_Level_2_Active)
                Log.Info("enter save_cat_parse(string in_string) " + in_string + " NO LEAVE MESSAGE");
#endif
            switch (in_string)
            {
                case "SETTING":
                    return save_cat.SETTING;
                case "SAVE":
                    return save_cat.SAVE;
                case "SAVEFILE":
                    return save_cat.SAVEFILE;
                case "SUBSAVEFILE":
                    return save_cat.SUBSAVEFILE;
                case "VESSEL_DROPPED":
                    return save_cat.DROPPED;
                case "VESSEL_NAME":
                    return save_cat.NAME;
                case "VESSEL_STATE":
                    return save_cat.STATE;
                case "KERBAL_DROPPED":
                    return save_cat.KERBAL_DROPPED;

                default:
                    return save_cat.UNDEF;
            }
        }


/*************************************************************************************************************************/
        public string save_cat_toString(save_cat cat)
        {
#if DEBUG
            if (Debug_Level_2_Active)  Log.Info("enter string save_cat_toString(save_cat cat) " + cat.ToString() + " NO LEAVE MESSAGE");
#endif
            switch (cat)
            {
                case save_cat.SETTING:
                    return "SETTING";
                case save_cat.SAVE:
                    return "SAVE";
                case save_cat.SAVEFILE:
                    return "SAVEFILE";
                case save_cat.SUBSAVEFILE:
                    return "SUBSAVEFILE";
                case save_cat.DROPPED:
                    return "VESSEL_DROPPED";
                case save_cat.NAME:
                    return "VESSEL_NAME";
                case save_cat.STATE:
                    return "VESSEL_STATE";
                case save_cat.KERBAL_DROPPED:
                    return "KERBAL_DROPPED";

                default:
                    return "UNDEF";
            }
        }

        
/*************************************************************************************************************************/
        public void delete_dropped_vessels()
        {
            List<string> temp_list = new List<string>();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.delete_dropped_vessels", "entering delete_dropped_vessels()");
#endif
            Vessels_dropped.Clear();
            Vessels_dropped_names.Clear();
            Vessel_State.Clear();
            Vessel_sub_save.Clear();
            Kerbal_dropped.Clear();
            Save_File_Content[save_cat.SAVEFILE].Clear();

            write_save_values_to_file();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving delete_dropped_vessels()");
#endif
        }


/*************************************************************************************************************************/
        public void delete_dropped_vessel(Guid vessel_guid)
        {
            List<string> temp_list = new List<string>();
            string temp_string = null;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.delete_dropped_vessel", "entering delete_dropped_vessel(Guid vessel_guid) " + vessel_guid.ToString());

            if (Debug_Active)  Log.Info("remove vessel" + vessel_guid.ToString());
#endif
            if (Vessels_dropped.ContainsKey(vessel_guid))
                Vessels_dropped.Remove(vessel_guid);
            if (Vessels_dropped_names.ContainsKey(vessel_guid))
                Vessels_dropped_names.Remove(vessel_guid);
            if (Vessel_State.ContainsKey(vessel_guid))
                Vessel_State.Remove(vessel_guid);
            if (Vessel_sub_save.ContainsKey(vessel_guid))
                Vessel_sub_save.Remove(vessel_guid);

            foreach (KeyValuePair<string, Guid> Kerbal in Kerbal_dropped)
                if (Kerbal.Value == vessel_guid)
                    temp_string = Kerbal.Key;
            if(temp_string!=null)
                Kerbal_dropped.Remove(temp_string);
 

            write_save_values_to_file();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving delete_dropped_vessel(Guid vessel_guid)");
#endif
        }


/*************************************************************************************************************************/
        public void init_Save_File_Content()
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Util.init_Save_File_Content", "entering init_Save_File_Content()");
#endif
            if (!Save_File_Content.ContainsKey(save_cat.SETTING))
                Save_File_Content.Add(save_cat.SETTING, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.SAVE))
                Save_File_Content.Add(save_cat.SAVE, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.SAVEFILE))
                Save_File_Content.Add(save_cat.SAVEFILE, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.SUBSAVEFILE))
                Save_File_Content.Add(save_cat.SUBSAVEFILE, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.DROPPED))
                Save_File_Content.Add(save_cat.DROPPED, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.NAME))
                Save_File_Content.Add(save_cat.NAME, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.STATE))
                Save_File_Content.Add(save_cat.STATE, new Dictionary<string, string>());

            if (!Save_File_Content.ContainsKey(save_cat.KERBAL_DROPPED))
                Save_File_Content.Add(save_cat.KERBAL_DROPPED, new Dictionary<string, string>());
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving init_Save_File_Content()");
#endif
        }

      
        /*************************************************************************************************************************/
        public string vesselstate_toString(vesselstate vs)
        {
#if DEBUG
            if (Debug_Level_2_Active)
                Log.Info("entering vesselstate_toString(vesselstate vs) " + vs.ToString() + " NO LEAVE ENTRY");
#endif
            switch (vs)
            {
                case vesselstate.DESTROYED:
                    return "DESTROYED";
                case vesselstate.FLY:
                    return "FLY";
                case vesselstate.LANDED:
                    return "LANDED";
                case vesselstate.RECOVERED:
                    return "RECOVERED";
                default:
                    return "NONE";
            }
        }


/*************************************************************************************************************************/
        public vesselstate parse_vesselstate (string str)
        {
#if DEBUG
            if (Debug_Level_2_Active)  Log.Info("entering  parse_vesselstate (string str) " + str + " NO LEAVE ENTRY");
#endif
            switch (str)
            {
                case "DESTROYED":
                    return vesselstate.DESTROYED;
                case "FLY":
                    return vesselstate.FLY;
                case "LANDED":
                    return vesselstate.LANDED;
                case "RECOVERED":
                    return vesselstate.RECOVERED;
                default:
                    return vesselstate.NONE;
            }
        }


/*************************************************************************************************************************/
        public vesselstate get_vessel_state(Guid id)
        {
#if DEBUG
            if (Debug_Level_2_Active)
                Log.Info("entering  vesselstate get_vessel_state(Guid id) " + id.ToString() + " NO LEAVE ENTRY");
#endif
            if (Vessel_State.ContainsKey(id))
                return Vessel_State[id];
            else
            {
#if DEBUG
                if (Debug_Active) Log.Info("" + id.ToString() + " NOT IN Vessel_State");
#endif
                return vesselstate.NONE;
            }
        }


/*************************************************************************************************************************/
        public bool set_vessel_state(Guid id, vesselstate state)
        {
#if DEBUG
            if (Debug_Level_2_Active)   Log.Info("entering  bool set_vessel_state(Guid id, vesselstate state) " + id.ToString() + " " + state.ToString() + " NO LEAVE ENTRY");
#endif
            if (Vessel_State.ContainsKey(id))
            {
                Vessel_State[id] = state;
                return true;
            }
            else
            {
#if DEBUG
                if (Debug_Active)  Log.Info("" + id.ToString() + " NOT IN Vessel_State");
#endif
                return false;
            }
        }
    }
}
