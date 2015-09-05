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
    public class ConquestDrone: Drone
    {
        
        public ConquestDrone(IMyEntity ent, BroadcastingTypes broadcasting) : base(ent, broadcasting)
        {
            Type = typeof (ConquestDrone);
        }

        private int ticks = 0;
        public void Update(Vector3D location)
        {
            if (ticks%20 == 0)
            {
                FindNearbyStuff();
            }
            if (ticks%100 == 0)
            {
                ReloadWeaponsAndReactors();
            }
            try
            {
                
                Patrol(location);
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
            NameBeacon();
            ticks++;
        }

        private void FindNearbyStuff()
        {
            var bs = new BoundingSphereD(Ship.GetPosition(), ConquestMod.MaxEngagementRange);
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere(ref bs);
            var closeBy = ents;//entitiesFiltered.Where(z => (z.GetPosition() - drone.GetPosition()).Length() < MaxEngagementRange).ToList();

            //var closeAsteroids = asteroids.Where(z => (z.GetPosition() - drone.GetPosition()).Length() < MaxEngagementRange).ToList();



            ClearNearbyObjects();
            foreach (var closeItem in closeBy)
            {
                try
                {
                    if (closeItem is IMyCubeGrid && !closeItem.Transparent && closeItem.Physics.Mass > 2000)
                        AddNearbyFloatingItem(closeItem);

                    if (closeItem is IMyVoxelBase)
                    {
                        ConquestDroneManager.GetInstance().AddDiscoveredAsteroid(closeItem as IMyVoxelBase);
                        AddNearbyAsteroid((IMyVoxelBase)closeItem);
                    }
                }
                catch
                {
                    //This catches duplicate key entries being put in KnownEntities.
                }
            }
        }

        public bool HasTarget()
        {
            return (_target != null || _targetPlayer != null)
            ;
        }
    }
}
