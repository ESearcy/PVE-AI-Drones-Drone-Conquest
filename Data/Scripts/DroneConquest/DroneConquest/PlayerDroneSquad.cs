using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    class PlayerDroneSquad
    {
        private int _maxNumDrones = 4;
        private List<PlayerDrone> _drones = new List<PlayerDrone>();
        private readonly long _ownerId;
        private static int ID = 0;
        private int myid;
        public PlayerDroneSquad(long id)
        {
            _ownerId = id;
            myid = ID;
            ID++;
        }

        public long GetOwnerId()
        {
            return _ownerId;
        }

        public void Update(Vector3D location)
        {
            UpdateComposition();
            Util.GetInstance().Log("[PlayerDroneSquad.Update] Updating "+_drones.Count+" Drones from squad "+myid);
            
            foreach (var x in _drones)
            {
                MyAPIGateway.Parallel.Do(delegate { try { x.Update(location); } catch (Exception) { } });
                //x.Update(location);
            }
            //_drones.ForEach(x => x.Update(location));
        }

        public Vector3D GetLocation()
        {
            Vector3D position = _drones.Aggregate(Vector3D.Zero, (current, d) => current + d.GetPosition());

            return position/_drones.Count;
        }

        public IEnumerable<Drone> GetDrones()
        {
            return _drones;
        }

        private void UpdateComposition()
        {
            int val = _drones.Count;
            _drones = _drones.Where(x => x.IsAlive()).ToList();

            if (_drones.Count < val)
            {
                Util.GetInstance().Log("[PlayerDroneSquad.UpdateComposition] " + (val - _drones.Count)+" drones lost fromk player "+_ownerId);
            }
        }

        public void AddDrone(PlayerDrone drone)
        {
            _drones.Add(drone);
        }

        public int DroneCount()
        {
            UpdateComposition();
            return _drones.Count;
        }

        public void SetOwner(long getOwnerId)
        {
            foreach (var d in _drones)
            {
                d.SetOwner(getOwnerId);
            }
        }

        public void StopAllDrones()
        {
            foreach (var VARIABLE in _drones)
            {
                VARIABLE.Stop();
            }
        }

        public void ClearAllDrones()
        {
            foreach (var VARIABLE in _drones)
            {
                VARIABLE.DeleteShip();
            }
            _drones.Clear();
        }
    }
}
