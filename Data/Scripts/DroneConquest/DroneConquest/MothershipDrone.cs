using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace DroneConquest
{
    internal class MothershipDrone : Drone
    {
        public MothershipDrone(IMyEntity ent, BroadcastingTypes broadcasting) : base(ent, broadcasting)
        {
            ReloadWeaponsAndReactors();
            Type = typeof (MothershipDrone);
        }

        private int ticks = 0;

        public void Update(List<Vector3D> asteroids)
        {
            if (ticks % 20 == 0)
            {
                FindNearbyStuff();
            }
            if (ticks % 100 == 0)
            {
                ReloadWeaponsAndReactors();
            }

            //where to get a list of things to build an orbit path out of...
            
            Orbit(asteroids.Where(x=>(x - new Vector3D(0,0,0)).Length()<ConquestDroneManager.DroneMaxRange).ToList());
            NameBeacon();
            ticks++;
        }

        private void FindNearbyStuff()
        {
            var bs = new BoundingSphereD(Ship.GetPosition(), ConquestMod.MaxEngagementRange);
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere(ref bs);
            var closeBy = ents;
            //entitiesFiltered.Where(z => (z.GetPosition() - drone.GetPosition()).Length() < MaxEngagementRange).ToList();

            //var closeAsteroids = asteroids.Where(z => (z.GetPosition() - drone.GetPosition()).Length() < MaxEngagementRange).ToList();



            ClearNearbyObjects();
            foreach (var closeItem in closeBy)
            {
                try
                {
                    if (closeItem is Sandbox.ModAPI.IMyCubeGrid)
                        AddNearbyFloatingItem(closeItem);

                    if (closeItem is IMyVoxelBase)
                    {
                        //_entitiesNearDcDrones.Add(closeItem)
                        AddNearbyAsteroid((IMyVoxelBase) closeItem);
                    }
                }
                catch
                {
                    //This catches duplicate key entries being put in KnownEntities.
                }
            }

        }
    }
}
