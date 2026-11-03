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


namespace FMRS
{
    public partial class FMRS_Core : FMRS_Util, IFMRS
    {
        /*************************************************************************************************************************/
        public void save_landed_vessel(bool auto_recover_allowed, bool ForceRecover)
        {
            Game loadgame, savegame;
            Guid temp_guid;
            int ReferenceBodyIndex = 0;
            List<Guid> id_list = new List<Guid>();
            List<ProtoVessel> vessel_list = new List<ProtoVessel>();
            Dictionary<Guid, List<ProtoVessel>> vessel_dict = new Dictionary<Guid, List<ProtoVessel>>();
            string message;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.save_landed_vessel", "entering save_landed_vessel(bool auto_recover_allowed) " + auto_recover_allowed.ToString());

            if (Debug_Active)
                Log.Info("save landed vessels");
#endif
            if (SwitchedToDropped == false)
            {
#if DEBUG
                if (Debug_Active)  Log.Info("in Main Save, leaving save_landed_vessel");
#endif
                return;
            }

            //GamePersistence.SaveGame("FMRS_quicksave", HighLogic.SaveFolder + "/FMRS", SaveMode.OVERWRITE);
            FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.save_landed_vessel", "FMRS_quicksave", HighLogic.SaveFolder + "/FMRS", SaveMode.OVERWRITE);
            loadgame = GamePersistence.LoadGame("FMRS_quicksave", HighLogic.SaveFolder + "/FMRS", false, false);
            savegame = load_snapshot(main_save_name, fmrs_folder(HighLogic.SaveFolder));
            if (savegame == null)
            {
                Log.Warning("save_landed_vessel: no usable " + main_save_name + ", landed vessels not saved");
                return;
            }

            foreach (Guid id in loaded_vessels)
            {
                if (FlightGlobals.Vessels.Find(v => v.id == id) == null)
                    if (!damaged_vessels.Contains(id))
                        damaged_vessels.Add(id);
            }

            foreach (Vessel v in FlightGlobals.Vessels.FindAll(v => v.loaded))
                id_list.Add(v.id);

            vessel_list = loadgame.flightState.protoVessels.FindAll(tpv => id_list.Contains(tpv.vesselID));

            foreach (Guid id in loaded_vessels)
                vessel_dict.Add(id, new List<ProtoVessel>());

            foreach (ProtoVessel pv in vessel_list)
            {
                if (loaded_vessels.Contains(pv.vesselID))
                    vessel_dict[pv.vesselID].Add(pv);
                else
                {
                    foreach (ProtoPartSnapshot ps in pv.protoPartSnapshots)
                    {
                        foreach (ProtoPartModuleSnapshot ppms in ps.modules)
                        {
                            if (ppms.moduleName == "FMRS_PM")
                            {
                                try
                                {
                                    temp_guid = new Guid(ppms.moduleValues.GetValue("parent_vessel"));
                                    if (loaded_vessels.Contains(temp_guid))
                                        vessel_dict[temp_guid].Add(pv);
                                }
                                catch (Exception)
                                {
                                    Log.Info("Exception: save_landed_vessel: temp_guid = new Guid(ppms.moduleValues.GetValue(parent_vessel));");
                                }

                            }
                            break;
                        }
                    }
                }
                ReferenceBodyIndex = pv.orbitSnapShot.ReferenceBodyIndex;
            }

            id_list.Clear();
            foreach (KeyValuePair<Guid, List<ProtoVessel>> kvp in vessel_dict)
                id_list.Add(kvp.Key);
            foreach (Guid id in id_list)
                if (vessel_dict[id].Count == 0)
                    vessel_dict.Remove(id);
            id_list.Clear();
            foreach (KeyValuePair<Guid, List<ProtoVessel>> kvp in vessel_dict)
                id_list.Add(kvp.Key);

            foreach (Guid id in id_list)
            {
                if (id_list.Count == 1 && vessel_dict[id].Count == 0 && get_vessel_state(id) != vesselstate.RECOVERED)
                    set_vessel_state(id, vesselstate.DESTROYED);

                if (get_vessel_state(id) == vesselstate.RECOVERED)
                    vessel_dict.Remove(id);
            }

            if (vessel_dict.Count != 0)
            {
#if DEBUG
                if (Debug_Active)   Log.Info("save landed vessel or recover");
#endif

                foreach (KeyValuePair<Guid, List<ProtoVessel>> kvp in vessel_dict)
                {
                    if ((_SETTING_Auto_Recover || ForceRecover) && auto_recover_allowed && ReferenceBodyIndex == 1)
                        savegame = recover_vessel(kvp.Key, kvp.Value, loadgame, savegame);
                    else
                    {
                        ProtoVessel temp_proto_del2 = savegame.flightState.protoVessels.Find(prtv => prtv.vesselID == kvp.Key);
                        if (temp_proto_del2 != null)
                            savegame.flightState.protoVessels.Remove(temp_proto_del2);

                        foreach (ProtoVessel pv in kvp.Value)
                        {
                            ProtoVessel temp_proto_del = savegame.flightState.protoVessels.Find(prtv => prtv.vesselID == pv.vesselID);
                            if (temp_proto_del != null)
                                savegame.flightState.protoVessels.Remove(temp_proto_del);

                            if (pv.landed || pv.splashed)
                            {
                                savegame.flightState.protoVessels.Add(pv);
                                set_vessel_state(kvp.Key, vesselstate.LANDED);
                            }
                        }
                    }
                }
            }

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                message = "";
                if (contract_complete.Count > 0)
                {
                    foreach (KeyValuePair<Guid, List<Contract>> kvp in contract_complete)
                    {
                        if (loaded_vessels.Contains(kvp.Key))
                        {
                            foreach (Contract c in kvp.Value)
                            {
                                set_recoverd_value("contract", "complete", c.ContractID.ToString());
                                message += c.Title + "@";
                            }
                        }
                    }
                    if (message != "")
                        set_recoverd_value("message", "FMRS: Completed Contracts", message);
                }
                foreach (KeyValuePair<Guid, List<ProtoVessel>> kvp in vessel_dict)
                {
                    if (contract_complete.ContainsKey(kvp.Key))
                        contract_complete.Remove(kvp.Key);
                }
            }

            foreach (killed_kerbal_str killed in killed_kerbals)
            {
#if DEBUG
                if (Debug_Active) Log.Info("Kerbal " + killed.name + " killed on that flight?");
#endif

                message = "";

                if (loaded_vessels.Contains(killed.vessel_id))
                {
#if DEBUG
                    if (Debug_Active) Log.Info("Kerbal " + killed.name + " killed");
#endif

                    foreach (ProtoCrewMember member in savegame.CrewRoster.Crew)
                    {
                        if (member.name == killed.name)
                        {
                            member.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                            set_recoverd_value("kerbal", "kill", killed.rep.ToString());
                            message += killed.name + " killed: " + Math.Round(killed.rep, 2).ToString() + " Reputation@";
                        }
                    }
                }
                if (message != "")
                    set_recoverd_value("message", "FMRS: Kerbal Killed", message);
            }

            if (damaged_buildings.Count > 0)
            {
                message = "";
                foreach (string build in damaged_buildings)
                {
                    set_recoverd_value("building", "destroyed", build);
                    message += build + "@";
                }
                if (message != "")
                    set_recoverd_value("message", "FMRS: Building Destroyed", message);
            }

            //GamePersistence.SaveGame(savegame, "FMRS_main_save", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.save_landed_vessel", savegame, main_save_name, fmrs_folder(HighLogic.SaveFolder), SaveMode.OVERWRITE);

            write_recover_file();
            write_save_values_to_file();

#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving save_landed_vessel(bool auto_recover_allowed)");
#endif
        }


        /*************************************************************************************************************************/
        public void fill_Vessels_list()
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.fill_Vessels_list", "entering fill_Vessels_list()");
#endif

            foreach (Vessel temp_vessel in FlightGlobals.Vessels)
            {
                if (!Vessels.Contains(temp_vessel.id))
                {
                    Vessels.Add(temp_vessel.id);
#if DEBUG
                    if (Debug_Active) Log.Info("" + temp_vessel.vesselName + " Found");
#endif
                }
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving fill_Vessels_list()");
#endif
        }


        /*************************************************************************************************************************/
        public bool search_for_new_vessels(string save_file_name)
        {
            bool new_vessel_found = false, controllable = false;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.search_for_new_vessels(string)", "entering search_for_new_vessels(string save_file_name) " + save_file_name);
#endif
            foreach (Vessel temp_vessel in FlightGlobals.Vessels)
            {
                controllable = false;

                //Check if the stage was claimed by another mod or by this mod
                string controllingMod = RecoveryControllerWrapper.ControllingMod(temp_vessel);                
                   
                bool FMRSIsControllingMod = false;
                if (controllingMod != null)
                {
                    Log.Info("RecoveryControllerWrapper.ControllingMod for vessel: " + temp_vessel.name + " :  " + controllingMod);

                    FMRSIsControllingMod = string.Equals(controllingMod, "FMRS", StringComparison.OrdinalIgnoreCase);
                }

                if (controllingMod == null || 
                    string.Equals(controllingMod, "auto", StringComparison.OrdinalIgnoreCase) ||
                   FMRSIsControllingMod)
                {
                    if (!Vessels.Contains(temp_vessel.id))
                    { 
                        if (FMRSIsControllingMod ||
                             (
                                ((temp_vessel.isCommandable && temp_vessel.IsControllable) || (_SETTING_Control_Uncontrollable && controllingMod == null)) &&
                                temp_vessel.vesselType != VesselType.EVA &&
                                temp_vessel.vesselType != VesselType.Flag &&
                                temp_vessel.vesselType != VesselType.SpaceObject &&
                                temp_vessel.vesselType != VesselType.Unknown
                              )
                           )

                            controllable = true;
                        else
                        {
                            foreach (ProtoPartSnapshot proto_part in temp_vessel.protoVessel.protoPartSnapshots)
                            {
                                List<ProtoPartModuleSnapshot> proto_modules = proto_part.modules;
                                ProtoPartModuleSnapshot module = null;

                                if (proto_modules != null && 
                                    (_SETTING_Parachutes && 
                                        ( (controllingMod != null && string.Equals(controllingMod, "FMRS", StringComparison.OrdinalIgnoreCase)) || 
                                        !_SETTING_Defer_Parachutes_to_StageRecovery || 
                                        !stageRecoveryInstalled)
                                        )
                                    )
                                {
                                    //
                                    module = proto_part.modules.Find(p => p.moduleName == "RealChuteModule" ||
                                                                            p.moduleName == "ModuleParachute" ||
                                                                            p.moduleName == "ModuleKrKerbalParachute" ||
                                                                            p.moduleName == "RealChuteFAR");
                                    if (module != null)
                                        controllable = true;
                                }

                                if (proto_part.protoCrewNames.Count > 0)
                                    controllable = true;
                            }
                        }
                        foreach (Part p in temp_vessel.Parts)
                        {
                            foreach (PartModule pm in p.Modules)
                            {
                                if (pm.moduleName == "FMRS_PM")
                                {
                                    if ((pm as FMRS_PM).parent_vessel != "00000000-0000-0000-0000-000000000000")
                                    {
                                        controllable = false;
                                        break;
                                    }
                                }
                            }
                            break;
                        }

                        if (controllable)
                        {
#if DEBUG
                            if (Debug_Active) Log.Info("" + temp_vessel.vesselName + " Found and will be added to the dicts");
#endif

                            Vessels_dropped.Add(temp_vessel.id, save_file_name);
                            Vessels_dropped_names.Add(temp_vessel.id, temp_vessel.vesselName);
                            Vessel_State.Add(temp_vessel.id, vesselstate.FLY);
                            foreach (Part p in temp_vessel.Parts)
                            {
                                foreach (PartModule pm in p.Modules)
                                    if (pm.moduleName == "FMRS_PM")
                                        pm.StartCoroutine("setid");
                            }

                            foreach (ProtoPartSnapshot part_snapshot in temp_vessel.protoVessel.protoPartSnapshots)
                            {
                                foreach (ProtoCrewMember member in part_snapshot.protoModuleCrew)
                                {
                                    if (!Kerbal_dropped.ContainsKey(member.name))
                                        Kerbal_dropped.Add(member.name, temp_vessel.id);
                                }
                            }
                            new_vessel_found = true;
                        }
                        Vessels.Add(temp_vessel.id);
                    }
                }
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving search_for_new_vessels(string save_file_name)");
#endif
            return (new_vessel_found);
        }


        /*************************************************************************************************************************/
        public List<ProtoVessel> search_for_new_vessels(Game loadgame, Game savegame)
        {
            List<ProtoVessel> return_list = new List<ProtoVessel>();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.search_for_new_vessels(string, string)", "entering List<Guid> search_for_new_vessels(Game loadgame, Game savegame)");
#endif

            foreach (ProtoVessel vessel_load in loadgame.flightState.protoVessels)
            {
                if (vessel_load.landed || vessel_load.splashed)

                    if (savegame.flightState.protoVessels.Find(v => v.vesselID == vessel_load.vesselID) == null)
                    {
                        return_list.Add(vessel_load);
#if DEBUG
                        if (Debug_Active) Log.Info("" + vessel_load.vesselName + " Found and added to list");
#endif
                    }
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving List<Guid> search_for_new_vessels(Game loadgame, Game savegame)");
#endif
            return (return_list);
        }


        /*************************************************************************************************************************/
        public void jump_to_vessel(Guid vessel_id, bool save_landed)
        {
            int load_vessel;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.jump_to_vessel(Guid, bool)", "entering jump_to_vessel(Guid vessel_id) " + vessel_id.ToString() + " " + save_landed.ToString());

            if (Debug_Active)  Log.Info("Jump to " + vessel_id.ToString());
#endif
            // would save the main vessel before contracts have loaded
            if (!career_state_loaded())
                return;

            if (save_landed)
            {
                if (FlightGlobals.ActiveVessel.id == _SAVE_Main_Vessel)
                {
                    //GamePersistence.SaveGame("FMRS_main_save", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                    FMRS_SAVE_Util.Instance.SaveGame("FMRS_Core.jump_to_vessel(Guid, bool)", main_save_name, fmrs_folder(HighLogic.SaveFolder), SaveMode.OVERWRITE);
                }
                save_landed_vessel(true, false);
            }

            Game loadgame = load_snapshot(get_save_value(save_cat.DROPPED, vessel_id.ToString()), fmrs_folder(HighLogic.SaveFolder));

            if (loadgame != null && loadgame.compatible && loadgame.flightState != null)
            {
#if DEBUG
                if (Debug_Active) Log.Info("try to load gamefile " + get_save_value(save_cat.DROPPED, vessel_id.ToString()));
#endif

                // TODO: semantics refactor. load_vessel is the incentive
                for (load_vessel = 0; load_vessel < loadgame.flightState.protoVessels.Count && loadgame.flightState.protoVessels[load_vessel].vesselID != vessel_id; load_vessel++) ;
              
              
                if (load_vessel < loadgame.flightState.protoVessels.Count)
                {
#if DEBUG
                    if (Debug_Active) Log.Info("FMRS_save found, Vessel found, try to start");
#endif
                    
                    if (vessel_id != _SAVE_Main_Vessel)
                    {
                        _SAVE_Switched_To_Savefile = get_save_value(save_cat.DROPPED, vessel_id.ToString());
                        Log.Info("sithilfe: " + _SAVE_Switched_To_Savefile);
                        _SAVE_Switched_To_Dropped = true;
                    }
                    else
                    {
                        _SAVE_Switched_To_Dropped = false;
                    }
                    Log.Info("load_vessel: " + load_vessel.ToString());
                    Log.Info("loadgame.flightState.protoVessels.Count: " + loadgame.flightState.protoVessels.Count.ToString());
                    //Log.Info("loadgame: " + loadgame.ToString());
                    Log.Info("File loaded: " + HighLogic.SaveFolder + "/FMRS/" + get_save_value(save_cat.DROPPED, vessel_id.ToString()));

                    Log.ShowStackInfo();
                    // detach_handlers();
                    FMRS_SAVE_Util.Instance.StartAndFocusVessel(loadgame, load_vessel);
//                    FlightDriver.StartAndFocusVessel(loadgame, load_vessel);
                    // attach_handlers();
                    return;
                }
            }
           
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving jump_to_vessel(Guid vessel_id)");
#endif
        }


        /*************************************************************************************************************************/
        public void jump_to_vessel(string main)
        {
            Game loadgame;
            int load_vessel;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.jump_to_vessel(string)", "entering jump_to_vessel(string main)");
            if (Debug_Active)  Log.Info("Jump to Main");
#endif

            if (!_SAVE_Switched_To_Dropped)
                return;

            save_landed_vessel(true, false);

            loadgame = load_snapshot(main_save_name, fmrs_folder(HighLogic.SaveFolder));

            if (loadgame != null && loadgame.compatible && loadgame.flightState != null)
            {
#if DEBUG
                if (Debug_Active)  Log.Info("try to load gamefile FMRS_main_save");
#endif

                for (load_vessel = 0; load_vessel < loadgame.flightState.protoVessels.Count && loadgame.flightState.protoVessels[load_vessel].vesselID != _SAVE_Main_Vessel; load_vessel++) ;
                if (load_vessel < loadgame.flightState.protoVessels.Count)
                {
#if DEBUG
                    if (Debug_Active) Log.Info("FMRS_main_save found, main vessel found, try to load");
#endif
                    _SAVE_Switched_To_Savefile = main_save_name;
                    _SAVE_Switched_To_Dropped = false;
                    FMRS_SAVE_Util.Instance.StartAndFocusVessel(loadgame, load_vessel);
//                    FlightDriver.StartAndFocusVessel(loadgame, load_vessel);
                }
#if DEBUG
                else
                    if (Debug_Active) Log.Info("main vessel not found in savefile");
#endif
            }
#if DEBUG
            else
                if (Debug_Active)  Log.Info("Loading gamefile FMRS_main_save failed");

           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving jump_to_vessel(string main)");
#endif
        }


        /*************************************************************************************************************************/
        public void jump_to_vessel(Guid vessel_id, string save_file)
        {
            Game loadgame;
            int load_vessel;
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.jump_to_vessel(Guid, string)", "entering jump_to_vessel(string vessel_id,string save_file " + vessel_id.ToString() + " " + save_file);
            if (Debug_Active)  Log.Info("Jump to " + save_file);
#endif
            loadgame = load_snapshot(save_file, fmrs_folder(HighLogic.SaveFolder));

            if (loadgame != null && loadgame.compatible && loadgame.flightState != null)
            {
#if DEBUG
                if (Debug_Active)  Log.Info("try to load gamefile " + save_file);
#endif

                for (load_vessel = 0; load_vessel < loadgame.flightState.protoVessels.Count && loadgame.flightState.protoVessels[load_vessel].vesselID != vessel_id; load_vessel++) ;
                if (load_vessel < loadgame.flightState.protoVessels.Count)
                {
                    if (vessel_id != _SAVE_Main_Vessel)
                    {
                        _SAVE_Switched_To_Savefile = save_file;
                        _SAVE_Switched_To_Dropped = true;
                    }
                    else
                        _SAVE_Switched_To_Dropped = false;
                    FMRS_SAVE_Util.Instance.StartAndFocusVessel(loadgame, load_vessel);
//                    FlightDriver.StartAndFocusVessel(loadgame, load_vessel);
                }
            }
#if DEBUG
            else
                if (Debug_Active)  Log.Info("Loading gamefile " + save_file + " failed");

           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving jump_to_vessel(string vessel_id,string save_file)");
#endif
        }


        /*************************************************************************************************************************/
        public void main_vessel_changed(string save_file)
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.main_vessel_changed", "enter main_vessel_changed(string save_file) " + save_file);
            if (Debug_Active) Log.Info("switching main vessel");
#endif
            ProtoVessel temp_proto;
            Game loadgame = GamePersistence.LoadGame(save_file, HighLogic.SaveFolder + "/FMRS", false, false);

            if (loadgame != null && loadgame.compatible && loadgame.flightState != null)
            {
                temp_proto = loadgame.flightState.protoVessels.Find(p => p.vesselID == _SAVE_Main_Vessel);
                if (temp_proto != null)
                {
                    if (Vessels_dropped.ContainsKey(temp_proto.vesselID))
                        delete_dropped_vessel(temp_proto.vesselID);

                    Vessels_dropped.Add(temp_proto.vesselID, quicksave_file_name);
                    Vessels_dropped_names.Add(temp_proto.vesselID, temp_proto.vesselName);
                    Vessels.Add(temp_proto.vesselID);
                }
#if DEBUG
                else
                    if (Debug_Active)  Log.Info("main vessel not found");
#endif

                if (Vessels_dropped.ContainsKey(FlightGlobals.ActiveVessel.id))
                    delete_dropped_vessel(FlightGlobals.ActiveVessel.id);

                _SAVE_Main_Vessel = FlightGlobals.ActiveVessel.id;
            }
#if DEBUG
            else
                if (Debug_Active)  Log.Info("unable to load savefile");

           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving main_vessel_changed(string save_file)");
#endif
        }


        /*************************************************************************************************************************/
        public void vessel_create_routine(Vessel input)
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.vessel_create_routine", "enter vessel_create_routine(Vessel input) " + input.id.ToString());
            if (Debug_Active) Log.Info("Vessel created");
#endif
            if (!staged_vessel)
            {
                Time_Trigger_Staging = Planetarium.GetUniversalTime() + 1;
                separated_vessel = true;
                timer_staging_active = true;
                timer_cuto_active = true;
                Time_Trigger_Cuto = Planetarium.GetUniversalTime();
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving vessel_create_routine(Vessel input)");
#endif
        }


        /*************************************************************************************************************************/
        void vessel_on_rails(Vessel vessel)
        {
            List<ProtoCrewMember> member_list = new List<ProtoCrewMember>();
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.vessel_on_rails", "enter vessel_on_rails(Vessel vessel) " + vessel.id.ToString());
            if (Debug_Active) Log.Info("Vessel will be on rails: " + vessel.vesselName);
#endif
            if (Vessels_dropped.ContainsKey(vessel.id) && !_SAVE_Switched_To_Dropped)
            {
#if DEBUG
                if (Debug_Active)  Log.Info("this vessel is listed in dropped dict");
#endif

                foreach (ProtoPartSnapshot part_snapshot in vessel.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoCrewMember member in part_snapshot.protoModuleCrew)
                        member_list.Add(member);
                    foreach (ProtoCrewMember member in member_list)
                    {
#if DEBUG
                        if (Debug_Active)  Log.Info("remove crew member " + member.name);
#endif
                        part_snapshot.RemoveCrew(member);
                    }
                    member_list.Clear();
                }
            }

            if (_SAVE_Switched_To_Dropped)
            {
                if (loaded_vessels.Contains(vessel.id) && vessel.id != FlightGlobals.ActiveVessel.id && !vessel.Landed && !vessel.Splashed)
                {
#if DEBUG
                    if (Debug_Active) Log.Info("loaded_vessels: removing " + vessel.id.ToString());
#endif
                    loaded_vessels.Remove(vessel.id);

#if DEBUG
                    if (Debug_Active)  Log.Info("vessel " + vessel.name + " removed from loaded_vessels");
#endif
                }

                if (vessel.id == _SAVE_Main_Vessel)
                {
                    if (ThrottleReplay != null)
                    {
                        vessel.OnFlyByWire -= new FlightInputCallback(ThrottleReplay.flybywire);
                        ThrottleReplay.EndReplay();
                    }
                }
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave vessel_on_rails(Vessel vessel)");
#endif
        }


        /*************************************************************************************************************************/
        void vessel_off_rails(Vessel vessel)
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.vessel_off_rails", "enter vessel_off_rails(Vessel vessel) " + vessel.id.ToString());
            if (Debug_Active)  Log.Info("Vessel will be off rails: " + vessel.vesselName);
#endif

            if (_SAVE_Switched_To_Dropped)
            {
                Vessel temp_vessel;
                temp_vessel = FlightGlobals.Vessels.Find(v => v.id == vessel.id);
                if (temp_vessel != null)
                    if (temp_vessel.loaded)
                        if (Vessels_dropped.ContainsKey(vessel.id))
                            if (Vessels_dropped[vessel.id] == _SAVE_Switched_To_Savefile)
                                if (!loaded_vessels.Contains(vessel.id))
                                {
#if DEBUG
                                    if (Debug_Active) Log.Info("Vessel will be off rails: adding to loaded_vessels");
#endif
                                    loaded_vessels.Add(vessel.id);
                                }

                if (vessel.id == FlightGlobals.ActiveVessel.id)
                {
                    List<Vessel> temp_vessel_list = new List<Vessel>();
                    temp_vessel_list = FlightGlobals.Vessels.FindAll(v => Vessels_dropped.ContainsKey(v.id) && v.loaded);
                    foreach (Vessel v in temp_vessel_list)
                        if (Vessels_dropped[v.id] == _SAVE_Switched_To_Savefile)
                            if (!loaded_vessels.Contains(v.id))
                            {
#if DEBUG
                                if (Debug_Active) Log.Info("Vessel will be off rails: adding to loaded_vessels");
#endif
                                loaded_vessels.Add(v.id);
                            }
                }
#if DEBUG
                if (Debug_Active) Log.Info("loaded_vessels: " + loaded_vessels.Count.ToString());
#endif
            }
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leave vessel_off_rails(Vessel vessel)");
#endif
        }
    }
}
