
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRageMath;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;


namespace DroneConquest
{
    public class ConquestMod
    {
        private ConquestDroneManager cManager=null;
        private PlayerDroneManager pManager=null;
        private int _findDroneRate = 1001;
        private int _conquestUpdateRate = 13;
        public static double MaxEngagementRange = 3000;
        private int _logSaveRate = 2000;
        private int _loadSaveSettingsRate = 2000;

        public ConquestMod()
        {
            
        }
        private bool displayedHelp = false;
        public void Update(int ticks)
        {
            try
            {
                if (cManager == null)
                {
                    cManager = ConquestDroneManager.GetInstance();
                    pManager = PlayerDroneManager.GetInstance();
                }

                if (ticks%_logSaveRate == 0)
                {
                    Util.SaveLogs();
                }

                if (ticks%_loadSaveSettingsRate == 0)
                {
                    Util.GetInstance().LoadCustomGameSettings();
                    Util.GetInstance()
                        .Log("[ConquestMod.Update] loaded game settings -> player droneCount:" +
                             Util.GameSettings.MaxPlayerDroneCount + " conquest dronepersquad:" +
                             Util.GameSettings.MaxNumDronesPerConquestSquad + " conquest numdronesquads:" +
                             Util.GameSettings.MaxNumConquestSquads, "ConquestMod.txt");

                    if (!displayedHelp)
                    {
                        
                        Util.GetInstance().Help();
                        displayedHelp = true;
                    }
                }

                if (ticks%_conquestUpdateRate == 0)
                {
                    FindAllDrones();
                    //CalculateDistances();
                    pManager.Update();
                    cManager.Update();
                }

                if (ticks%_findDroneRate == 0)
                {
                    Util.GetInstance()
                        .Log(
                            "[ConquestMod.Update] loaded game settings -> player Building drones:" + GetDrones().Count,
                            "ConquestMod.txt");
                    cManager.RebuildLostDrones();
                }
            }
            catch (Exception e)

            {
                Util.GetInstance().LogError(e.ToString());
            }
        }

        private HashSet<Drone> GetDrones()
        {
            HashSet<Drone> allDrones = new HashSet<Drone>();

            foreach (var drone in pManager.GetDrones())
            {
                if (!allDrones.Contains(drone))
                    allDrones.Add(drone);
            }
            foreach (var drone in cManager.GetDrones())
            {
                if (!allDrones.Contains(drone))
                    allDrones.Add(drone);
            }
            return allDrones;
        }

        public void FindAllDrones()
        {
            Util.GetInstance().Log("[ConquestMod.FindAllDrones] Searching for drones");
            HashSet<Drone> allDrones = GetDrones();

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            List<IMyPlayer> players = new List<IMyPlayer>();

            try
            {
                MyAPIGateway.Entities.GetEntities(entities);
                MyAPIGateway.Players.GetPlayers(players);
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
                return;
            }


            //filter out any grids that are already accounted for
            foreach (IMyEntity entity in entities)
            {
                if (allDrones.All(x => x.Ship != entity))
                {
                    if (entity is IMyCubeGrid && !entity.Transparent)
                    {
                        SetUpDrone(entity);
                    }
                }
            }
        }


        private void SetUpDrone(IMyEntity entity)
        {
            IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid((IMyCubeGrid)entity);
            List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> T = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
            gridTerminal.GetBlocksOfType<IMyTerminalBlock>(T);

            var droneType = GetDroneType(T);


            if (droneType.DroneType != DroneTypes.NotADrone)
            {
                try
                {
                    
                    switch (droneType.DroneType)
                    {
                        case DroneTypes.PlayerDrone:
                            {

                                PlayerDrone dro = new PlayerDrone(entity, droneType.Broadcasting);
                                Util.GetInstance().Log("[ConquestMod.SetUpDrone] Found New Player Drone. id=" + dro.GetOwnerId());
                                pManager.AddDrone(dro);

                                break;
                            }
                        case DroneTypes.MothershipDrone:
                            {
                                MothershipDrone dro = new MothershipDrone(entity, droneType.Broadcasting);
                                entity.DisplayName = "";
                                ((IMyCubeGrid)entity).Name = "";
                                ((IMyCubeGrid)entity).ChangeGridOwnership(cManager.GetMothershipID(), MyOwnershipShareModeEnum.Faction);
                                ((IMyCubeGrid)entity).UpdateOwnership(cManager.GetMothershipID(), true);
                                Util.GetInstance().Log("[ConquestMod.SetUpDrone] found new conquest drone");

                                cManager.AddMothership(dro);
                                break;
                            }
                        case DroneTypes.ConquestDrone:
                            {
                                ConquestDrone dro = new ConquestDrone(entity, droneType.Broadcasting);
                                entity.DisplayName = "";
                                ((IMyCubeGrid)entity).Name = "";
                                ((IMyCubeGrid)entity).ChangeGridOwnership(cManager.GetMothershipID(), MyOwnershipShareModeEnum.Faction);
                                ((IMyCubeGrid)entity).UpdateOwnership(cManager.GetMothershipID(), true);
                                Util.GetInstance().Log("[ConquestMod.SetUpDrone] found new conquest drone");

                                cManager.AddDrone(dro);
                                break;
                            }
                        default:
                            {
                                //Util.Notify("broken drone type");
                                break;
                            }
                        


                    }
                }
                catch (Exception e)
                {
                    //MyAPIGateway.Entities.RemoveEntity(entity);
                    Util.GetInstance().LogError(e.ToString());
                }
            }
        }

        private string Drone = "Drone";
        private string PlayerDrone = "PlayerDrone";
        private string ConquestDrone = "ConquestDrone";
        private string MothershipDrone = "MothershipDrone";

        private DroneConstructionType GetDroneType(List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> T)
        {
            var droneType = DroneTypes.NotADrone;
            var usingBeacons = true;

            if (T.Exists(x => ((x).CustomName.Contains(Drone) && x.IsWorking)))
            {

                if (T.Exists(x => ((x).CustomName.Contains(":antenna") && x.IsWorking)))
                {
                    usingBeacons = false;
                }

                if (T.Exists(x => ((x).CustomName.Contains(PlayerDrone) && x.IsWorking)))
                {
                    droneType = DroneTypes.PlayerDrone;
                }
                else if (T.Exists(x => ((x).CustomName.Contains(MothershipDrone) && x.IsWorking) && !x.CustomName.Contains("Factory")))
                {
                    droneType = DroneTypes.MothershipDrone;
                }
                else if (T.Exists(x => ((x).CustomName.Contains(ConquestDrone) && x.IsWorking && !(x).CustomName.Contains(MothershipDrone))))
                {
                    droneType = DroneTypes.ConquestDrone;
                }
            }

            return new DroneConstructionType(usingBeacons ? BroadcastingTypes.Beacon : BroadcastingTypes.Antenna, droneType);
        
        }

        public void StopAllDrones()
        {
            pManager.StopAllDrones();
            cManager.StopAllDrones();
        }

        public void ClearAllDrones()
        {
            pManager.ClearAllDrones();
            cManager.ClearAllDrones();
        }
    }
}
