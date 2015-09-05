using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.ModAPI;
using VRageMath;

namespace DroneConquest
{
    class Spawner
    {
        Dictionary<ConquestDrones,string> map = new Dictionary<ConquestDrones, string>(); 
        public Spawner()
        {
            map.Add(ConquestDrones.SmallOne, "-DC-Stinger");
            map.Add(ConquestDrones.SmallTwo, "-DC-Praetorian");
            map.Add(ConquestDrones.SmallThree, "-DC-Swarmer");
            map.Add(ConquestDrones.MediumOne, "-DC-Tusker");
            map.Add(ConquestDrones.MediumTwo, "-DC-Buzzer");
            map.Add(ConquestDrones.MediumThree, "-DC-ToBeNamed");
            map.Add(ConquestDrones.LargeOne, "-DC-Flagship");
            map.Add(ConquestDrones.LargeTwo, "-DC-Mothership");
            map.Add(ConquestDrones.LargeThree, "-DC-ToBeNamed");
        }
        public ConquestDrone SpawnShip(ConquestDrones type, Vector3D location)
        {
            try
            {
                VRage.ObjectBuilders.MyObjectBuilder_EntityBase val;
                var t = MyDefinitionManager.Static.GetPrefabDefinition(map[type]);
                
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
                MyAPIGateway.PrefabManager.SpawnPrefab(shipMade, map[type], newPosition, Vector3.Forward, Vector3.Up);

                foreach (var ship in shipMade)
                {
                    ship.DisplayName = "";
                    (ship).Name = "";
                    try
                    {
                        if (type != ConquestDrones.LargeOne)
                            ConquestDroneManager.GetInstance()
                                .AddDrone(new ConquestDrone(ship, BroadcastingTypes.Antenna));
                        else
                        {
                            ConquestDroneManager.GetInstance()
                                .AddMothership(new MothershipDrone(ship, BroadcastingTypes.Antenna));
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
