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
        public struct science_data_sent
        {
            public string id;
            public float amount;
        }
        public struct killed_kerbal_str
        {
            public string name;
            public float rep;
            public Guid vessel_id;
        }

/*************************************************************************************************************************/
        public void attach_handlers()
        {
            if (_SAVE_Switched_To_Dropped)
            {
                GameEvents.OnVesselRecoveryRequested.Add(recovery_requested_handler);
                GameEvents.Contract.onCompleted.Add(contract_routine);
                GameEvents.OnScienceRecieved.Add(science_sent_routine);
                GameEvents.onCollision.Add(crash_handler);
                GameEvents.onCrash.Add(crash_handler);
                GameEvents.onCrashSplashdown.Add(crash_handler);
                GameEvents.onCrewKilled.Add(crew_killed_handler);
                GameEvents.OnReputationChanged.Add(rep_changed);
                GameEvents.OnKSCStructureCollapsing.Add(building_destroyed);
                GameEvents.onVesselGoOffRails.Add(vessel_off_rails);
                GameEvents.onVesselSituationChange.Add(vessel_state_changed);
            }
            else
            {
                if (_SAVE_Has_Launched)
                {
                    GameEvents.onStageSeparation.Add(staging_routine);
                    GameEvents.onUndock.Add (staging_routine);
                    GameEvents.onVesselCreate.Add(vessel_create_routine);
                }
            }

            GameEvents.onVesselGoOnRails.Add(vessel_on_rails);
            GameEvents.onVesselChange.Add(vessel_change_handler);
            GameEvents.onGameSceneLoadRequested.Add(scene_change_handler);
        }


/*************************************************************************************************************************/
        public void detach_handlers()
        {
            GameEvents.onStageSeparation.Remove(staging_routine);
            GameEvents.onUndock.Remove(staging_routine);
            GameEvents.onLaunch.Remove(launch_routine);
            GameEvents.onCollision.Remove(crash_handler);
            GameEvents.onCrash.Remove(crash_handler);
            GameEvents.onCrashSplashdown.Remove(crash_handler);
            GameEvents.onVesselChange.Remove(vessel_change_handler);
            GameEvents.OnVesselRecoveryRequested.Remove(recovery_requested_handler);
            GameEvents.onGameSceneLoadRequested.Remove(scene_change_handler);
            GameEvents.onVesselCreate.Remove(vessel_create_routine);
            GameEvents.Contract.onCompleted.Remove(contract_routine);
            GameEvents.OnScienceRecieved.Remove(science_sent_routine);
            GameEvents.onCrewKilled.Remove(crew_killed_handler);
            GameEvents.OnReputationChanged.Remove(rep_changed);
            GameEvents.onVesselGoOnRails.Remove(vessel_on_rails);
            GameEvents.OnKSCStructureCollapsing.Remove(building_destroyed);
            GameEvents.onVesselGoOffRails.Remove(vessel_off_rails);
            GameEvents.onVesselSituationChange.Remove(vessel_state_changed);
        }


/*************************************************************************************************************************/
        public void staging_routine(EventReport event_input)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.staging_routine", "entering staging_routine(EventReport event_input) " + event_input.sender);
#endif

            Time_Trigger_Staging = Planetarium.GetUniversalTime();
            timer_staging_active = true;
            staged_vessel = true;
            separated_vessel = false;
            timer_cuto_active = true;
            Time_Trigger_Cuto = Planetarium.GetUniversalTime();
#if DEBUG
            if (Debug_Active)
                Log.Info("Has Staged");
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving staging_routine(EventReport event_imput)");
#endif
        }


/*************************************************************************************************************************/
        public void launch_routine(EventReport event_input)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.launch_routine", "entering launch_routine(EventReport event_imput)");
            if (Debug_Active)
                Log.Info("LAUNCH");
#endif
            if (!_SETTING_Armed)
            {
                close_FMRS();
                return;
            }

            if (!_SAVE_Has_Launched || flight_preflight)
            {
                _SAVE_Has_Launched = true;
                _SAVE_Launched_At = Planetarium.GetUniversalTime();
                fill_Vessels_list();
                GameEvents.onStageSeparation.Add(staging_routine);
                GameEvents.onUndock.Add(staging_routine);
                GameEvents.onVesselCreate.Add(vessel_create_routine);
            }

            can_restart = HighLogic.CurrentGame.Parameters.Flight.CanRestart;
            if (HighLogic.CurrentGame.Parameters.Flight.CanQuickLoad && HighLogic.CurrentGame.Parameters.Flight.CanQuickSave)
                can_q_save_load = true;
            else
                can_q_save_load = false;

            if (_SETTING_Throttle_Log)
            {
#if DEBUG
                ThrottleLogger = new FMRS_THL.FMRS_THL_Log(Debug_Active, Debug_Level_1_Active);
#else
                ThrottleLogger = new FMRS_THL.FMRS_THL_Log();
#endif
                ThrottleLogger.flush_record_file();
                ThrottleLogger.StartLog();
            }
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving launch_routine(EventReport event_imput)");
#endif
        }


/*************************************************************************************************************************/
        public void crash_handler(EventReport report)
        {
            List<Guid> new_vessels = new List<Guid>();
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.crash_handler", "enter crash_handler (EventReport report) " + report.sender);
            if (Debug_Active)
                Log.Info("crash detected");
#endif

            /*if (FlightGlobals.ActiveVessel.state == Vessel.State.DEAD && !lost_root_part)
            {
                Log.Info("lost root part");

                lost_root_part = true;
                anz_id = FlightGlobals.ActiveVessel.id;
            }*/
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave crash_handler (EventReport report)");
#endif
        }


/*************************************************************************************************************************/
        public void vessel_change_handler(Vessel change_vessel)
        {
#if DEBUG
            if (Debug_Active)
                Debug.Log(" #### FMRS: changed to " + FlightGlobals.ActiveVessel.vesselName);
#endif
            if (!_SAVE_Switched_To_Dropped)
            {
                if (last_staging_event < Planetarium.GetUniversalTime() + 10 && last_staging_event != 0)
                {
                    if (FlightGlobals.ActiveVessel.id != _SAVE_Main_Vessel && Vessels_dropped.ContainsKey(FlightGlobals.ActiveVessel.id))
                    {
                        Vessel temp_vessel = FlightGlobals.Vessels.Find(v => v.id == _SAVE_Main_Vessel);
                        if (temp_vessel != null)
                        {
                            if (Vessels_dropped.ContainsKey(_SAVE_Main_Vessel))
                                Vessels_dropped.Remove(_SAVE_Main_Vessel);
                            if (Vessel_State.ContainsKey(_SAVE_Main_Vessel))
                                Vessel_State.Remove(_SAVE_Main_Vessel);
                            if (Vessels_dropped_names.ContainsKey(_SAVE_Main_Vessel))
                                Vessels_dropped_names.Remove(_SAVE_Main_Vessel);
                            if (Vessel_State.ContainsKey(_SAVE_Main_Vessel))
                                Vessel_State.Remove(_SAVE_Main_Vessel);

                            Vessels_dropped.Add(_SAVE_Main_Vessel, Vessels_dropped[FlightGlobals.ActiveVessel.id]);
                            Vessel_State.Add(_SAVE_Main_Vessel, vesselstate.FLY);
                            Vessels_dropped_names.Add(_SAVE_Main_Vessel, temp_vessel.vesselName);

                            foreach (ProtoCrewMember crew_member in temp_vessel.protoVessel.GetVesselCrew())
                                Kerbal_dropped.Add(crew_member.name, _SAVE_Main_Vessel);

                            if (Vessels_dropped.ContainsKey(FlightGlobals.ActiveVessel.id))
                                Vessels_dropped.Remove(FlightGlobals.ActiveVessel.id);
                            if (Vessels_dropped_names.ContainsKey(FlightGlobals.ActiveVessel.id))
                                Vessels_dropped_names.Remove(FlightGlobals.ActiveVessel.id);
                            if (Vessel_State.ContainsKey(FlightGlobals.ActiveVessel.id))
                                Vessel_State.Remove(FlightGlobals.ActiveVessel.id);

                            if (Kerbal_dropped.ContainsValue(FlightGlobals.ActiveVessel.id))
                            {
                                List<string> kerbals = new List<string>();
                                foreach (KeyValuePair<string, Guid> kerbal in Kerbal_dropped)
                                    if (kerbal.Value == FlightGlobals.ActiveVessel.id)
                                        kerbals.Add(kerbal.Key);
                                foreach (string kerbal in kerbals)
                                    Kerbal_dropped.Remove(kerbal);
                            }

                            _SAVE_Main_Vessel = FlightGlobals.ActiveVessel.id;
                        }
                    }
                }
                else
                {
                    if (!Vessels_dropped.ContainsKey(change_vessel.id))
                        close_FMRS();
                }

            }
            else
            {
                if (!Vessels_dropped.ContainsKey(FlightGlobals.ActiveVessel.id))
                {
                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm.moduleName == "FMRS_PM")
                            {
                                Guid temp_guid = new Guid("00000000-0000-0000-0000-000000000000");

                                try { temp_guid = new Guid((pm as FMRS_PM).parent_vessel); }
                                catch (Exception) { }

                                if (Vessels_dropped.ContainsKey(temp_guid))
                                    if (Vessels_dropped[temp_guid] == _SAVE_Switched_To_Savefile)
                                        anz_id = temp_guid;
                            }
                        }
                        break;
                    }
                }
            }
        }


/*************************************************************************************************************************/
        public void scene_change_handler(GameScenes input_scene)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.scene_change_handler","enter scene_change_handler(GameScenes input_scene) " + input_scene.ToString());
            if (Debug_Active)
                Log.Info("scene_change_handler");
#endif
            if (input_scene != GameScenes.FLIGHT)
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("switch to not flight scene");
#endif
                if (_SAVE_Kick_To_Main)
                    return;
#if DEBUG
                if (Debug_Active)
                    Log.Info("has not recovered");
#endif
                if (_SAVE_Switched_To_Dropped)
                {
#if DEBUG
                    if (Debug_Active)
                        Log.Info("scene change while flying dropped, kick to main in space center and tracking station");
#endif
                    set_recoverd_value("warning", "FMRS Info:", "You have switched scenes, while controlling a dropped vessel.@Next time, please use the 'Jump back to Main Mission' button, before switching scenes.");
                    _SAVE_Kick_To_Main = true;
                    _SAVE_Switched_To_Dropped = false;
                    save_landed_vessel(true, false);
                }
                else
                    _SETTING_Enabled = false;
            }
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave scene_change_handler(GameScenes input_scene)");
#endif
        }


/*************************************************************************************************************************/
        public void contract_routine(Contracts.Contract input_contract)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.contract_routine", "enter vcontract_routine(Contracts.Contract input_contract) " + input_contract.Title);
            if (Debug_Active)
                Log.Info("contract " + input_contract.Title + " " + input_contract.ContractState.ToString());
#endif
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("not in carreer mode, leave contract_routine(Contracts.Contract input_contract)"); 
#endif
                return;
            }

            if (!contract_complete.ContainsKey(FlightGlobals.ActiveVessel.id))
                contract_complete.Add(FlightGlobals.ActiveVessel.id, new List<Contract>());

            contract_complete[FlightGlobals.ActiveVessel.id].Add(input_contract);
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave vcontract_routine(Contracts.Contract input_contract)");
#endif
        }


/*************************************************************************************************************************/
        public void science_sent_routine(float science, ScienceSubject input_science_sub, ProtoVessel vessel, bool boolValue)
        {
            science_data_sent data;
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.science_sent_routine", "enter science_routine(float amount, ScienceSubject input_science_sub) " + science.ToString() + " " + input_science_sub.title);
            if (Debug_Active)
                Log.Info("Science received");
#endif

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
#if DEBUG
                if (Debug_Active)
                    Log.Info("not in science career mode, leave science_routine(float amount, ScienceSubject input_science_sub)");
#endif
                return;
            }
#if DEBUG
            if (Debug_Active)
                Log.Info("science sent: " + input_science_sub.id + " + " + science.ToString());
#endif
            data.id = input_science_sub.id;
            data.amount = science;
            science_sent.Add(data);
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave science_routine(float amount, ScienceSubject input_science_sub)");
#endif
        }


/*************************************************************************************************************************/
        void crew_killed_handler(EventReport report)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.crew_killed_handler", "enter crew_killed_handler(EventReport report) " + report.sender);
            if (Debug_Active)
                Log.Info("crew member killed: " + report.sender + " rep los: " + last_rep_change.ToString());
#endif

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;

            if (Kerbal_dropped.ContainsKey(report.sender))
            {
                killed_kerbal_str killed;
                killed.name = report.sender;
                killed.rep = last_rep_change;
                killed.vessel_id = Kerbal_dropped[killed.name];
                killed_kerbals.Add(killed);
#if DEBUG
                if (Debug_Active)
                    Log.Info("" + report.sender + " was in dropped stage");
#endif
            }
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave crew_killed_handler(EventReport report)");
#endif
        }


/*************************************************************************************************************************/
        void rep_changed(float rep, TransactionReasons reason)
        {
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.rep_changed", "enter rep_changed(float rep, TransactionReasons reason) " + rep.ToString() + " " + reason.ToString());
#endif
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;

            last_rep_change = rep - current_rep;
#if DEBUG
            if (Debug_Active)
                Log.Info("rep changed: " + rep.ToString() + " " + last_rep_change.ToString());
#endif

            current_rep = rep;
#if DEBUG
            //if (Debug_Level_1_Active)
                Log.PopStackInfo("leave rep_changed(float rep)");
#endif
        }


/*************************************************************************************************************************/
        private void building_destroyed(DestructibleBuilding building)
        {
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMS_Core.building_destroyed", "enter building_destroyed(DestructibleBuilding building) " + building.name);
            if (Debug_Active)
                Log.Info("" + building.name + " destroyed");
#endif
            if (!damaged_buildings.Contains(building.name))
                damaged_buildings.Add(building.name);
#if DEBUG
           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving building_destroyed(DestructibleBuilding building)");
#endif
        }


/*************************************************************************************************************************/
        private void vessel_state_changed(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> input)
        {
           // if (Debug_Level_1_Active)
                Log.PushStackInfo("FMRS_Core.vessel_state_changed", "enter vessel_state_changed(DestructibleBuilding building) " + input.host.ToString());
#if DEBUG
            if (Debug_Active)
#endif
                Log.Info("" + input.host.ToString() + " destroyed");

            

           // if (Debug_Level_1_Active)
                Log.PopStackInfo("leaving vessel_state_changed(DestructibleBuilding building)");
        }
    }
}
