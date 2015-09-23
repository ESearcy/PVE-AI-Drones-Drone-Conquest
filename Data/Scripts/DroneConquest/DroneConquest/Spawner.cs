using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    class Spawner
    {
        Dictionary<ConquestDrones,string> map = new Dictionary<ConquestDrones, string>();
        Dictionary<ConquestDrones, string> mapCustom = new Dictionary<ConquestDrones, string>();
        public Spawner()
        {
            map.Add(ConquestDrones.SmallOne, "-DC-Stinger");
            map.Add(ConquestDrones.SmallTwo, "-DC-Praetorian");
            map.Add(ConquestDrones.SmallThree, "-DC-Swarmer");
            map.Add(ConquestDrones.MediumOne, "-DC-Tusker");
            map.Add(ConquestDrones.MediumTwo, "-DC-Buzzer");
            map.Add(ConquestDrones.MediumThree, "-DC-Tusker");
            map.Add(ConquestDrones.LargeOne, "-DC-Flagship");
            map.Add(ConquestDrones.LargeTwo, "-DC-Flagship");
            map.Add(ConquestDrones.LargeThree, "-DC-Flagship");

            mapCustom.Add(ConquestDrones.SmallOne, "-DC-001");
            mapCustom.Add(ConquestDrones.SmallTwo, "-DC-002");
            mapCustom.Add(ConquestDrones.SmallThree, "-DC-003");
            mapCustom.Add(ConquestDrones.MediumOne, "-DC-004");
            mapCustom.Add(ConquestDrones.MediumTwo, "-DC-005");
            mapCustom.Add(ConquestDrones.MediumThree, "-DC-006");
            mapCustom.Add(ConquestDrones.LargeOne, "-DC-FlagShip");
            mapCustom.Add(ConquestDrones.LargeTwo, "-DC-007");
            mapCustom.Add(ConquestDrones.LargeThree, "-DC-008");
        }

        private Vector3D GetPositionWithinAnyPlayerViewDistance(Vector3D pos)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            var playerViewDistance = MyAPIGateway.Session.SessionSettings.ViewDistance;
            Vector3D position = Vector3D.Zero;
            Util.GetInstance().Log("Player View Distance: " + playerViewDistance, "Spawner.txt");
            if (players.Any())
            {
                position = (players.OrderBy(x=>(x.GetPosition()-pos).Length()).First().GetPosition() + new Vector3D(0, 0, playerViewDistance*.85));
            }

            return position;
        }

        public ConquestDrone SpawnShip(ConquestDrones type, Vector3D location)
        {
            try
            {
                

                var t = MyDefinitionManager.Static.GetPrefabDefinition(map[type]);
                var customT = MyDefinitionManager.Static.GetPrefabDefinition(mapCustom[type]);

                if (customT != null)
                {
                    t = customT;
                    Util.GetInstance().Log("SPAWNING CUSTOM: " + mapCustom[type], "Spawner.txt");
                }

                if (t == null)
                {
                    Util.GetInstance().Log("Failed To Load Ship: " + map[type], "Spawner.txt");
                    return null;
                }

                var s = t.CubeGrids;
                s = (MyObjectBuilder_CubeGrid[]) s.Clone();

                if (s.Length == 0)
                {
                    return null;
                }

                

                Vector3I min = Vector3I.MaxValue;
                Vector3I max = Vector3I.MinValue;

                s[0].CubeBlocks.ForEach(b => min = Vector3I.Min(b.Min, min));
                s[0].CubeBlocks.ForEach(b => max = Vector3I.Max(b.Min, max));
                float size = new Vector3(max - min).Length();

                var freeplace = MyAPIGateway.Entities.FindFreePlace(location, size*5f);
                if (freeplace == null)
                    return null;

                var newPosition = (Vector3D) freeplace;

                

                var grid = s[0];
                if (grid == null)
                {
                    Util.GetInstance().Log("A CubeGrid is null!", "Spawner.txt");
                    return null;
                }

                List<IMyCubeGrid> shipMade = new List<IMyCubeGrid>();

                var spawnpoint = GetPositionWithinAnyPlayerViewDistance(newPosition);
                var safespawnpoint = MyAPIGateway.Entities.FindFreePlace(spawnpoint, size * 5f);
                spawnpoint = safespawnpoint is Vector3D ? (Vector3D) safespawnpoint : new Vector3D();

                //to - from
                var direction = newPosition - spawnpoint;
                var finalSpawnPoint = (direction/direction.Length())* (MyAPIGateway.Session.SessionSettings.ViewDistance*.85);

                MyAPIGateway.PrefabManager.SpawnPrefab(shipMade, map[type], finalSpawnPoint, Vector3.Forward, Vector3.Up, Vector3.Zero, default(Vector3), null, SpawningOptions.None, 0L, true);
                //MyAPIGateway.PrefabManager.SpawnPrefab(shipMade, map[type], newPosition, Vector3.Forward, Vector3.Up);
                

                foreach (var ship in shipMade)
                {
                    ship.Physics.ForceActivate();
                    ship.DisplayName = "";
                    ship.Name = "";

                    try
                    {
                        if (type != ConquestDrones.LargeOne)
                            ConquestDroneManager.GetInstance().AddDrone(new ConquestDrone(ship, BroadcastingTypes.Antenna));
                        else
                        {
                            ConquestDroneManager.GetInstance().AddMothership(new MothershipDrone(ship, BroadcastingTypes.Antenna));
                        }
                        Util.GetInstance().Log("Turning the grid to a drone was a success!!", "Spawner.txt");
                    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Entities.RemoveEntity(ship);
                        Util.GetInstance().Log("The grid was a fake!!!", "Spawner.txt");
                    }
                }
                //var gridBuilder = (Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeGrid) grid.Clone();

                //gridBuilder.PositionAndOrientation =
                //    (new VRage.MyPositionAndOrientation(newPosition,
                //        grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up));

                
                
                //MyAPIGateway.Entities.RemapObjectBuilder(grid);
                //var tmp = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);
                
                
                ////MyAPIGateway.Entities.AddEntity(tmp);
                ////MyAPIGateway.Entities.RegisterForUpdate(tmp);
                ////MyAPIGateway.Entities.RegisterForDraw(tmp);

                //if(!tmp.InScene)
                //    Util.GetInstance().Log("Not In Scene", "Spawner.txt");
                //else
                //    Util.GetInstance().Log("In Scene", "Spawner.txt");

                //tmp.NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                //ConquestDrone drone = new ConquestDrone(tmp, BroadcastingTypes.Antenna);
                return null;
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
            return null;
        }
    }
}
