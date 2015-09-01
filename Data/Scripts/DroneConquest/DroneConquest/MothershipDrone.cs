﻿using System;
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
            Type = typeof (MothershipDrone);
        }

        private int ticks = 0;

        public void Update(List<Vector3D> asteroids)
        {
            if (ticks%5 == 0)
                FindNearbyStuff();

            //where to get a list of things to build an orbit path out of...
            ReloadWeaponsAndReactors();
            Orbit(asteroids);
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