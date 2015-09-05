using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    
    public class ConquestDroneSquad
    {
        private static int IDS = 0;
        public int myid;
        private List<ConquestDrone> _drones = new List<ConquestDrone>();
        private ConquestMission mission;

        public ConquestMission Mission()
        {
            return mission;
        }
        public void IssueMission(ConquestMission mis)
        {
            mission = mis;
        }

        public bool ReadyForNewMission(int ticks)
        {
            if (mission == null)
            {
                return true;
            }

            return (mission.Completed||mission.Expired(ticks)) && !InCombat();
        }

        public bool MissionOngoing()
        {
            if (mission == null)
                return false;

            return mission.Ongoing;
        }

        public ConquestDroneSquad(long getOwnerId)
        {
            myid = IDS;
            IDS++;
        }

        private int _maxNumDrones = 4;
        private readonly long _ownerId;

        public long GetOwnerId()
        {
            return _ownerId;
        }

        public void Update()
        {
            try
            {
                var location = Vector3D.Zero;
                if (mission != null)
                {
                    location = mission.Location;
                }
                UpdateComposition();
                Util.GetInstance()
                    .Log("[ConquestDroneSquad.Update] Updating " + _drones.Count + " Drones from squad " + myid);

                //update mission details

                _drones.ForEach(y => MyAPIGateway.Parallel.Do(delegate
                {
                    try
                    {
                        y.Update(location);
                    }
                    catch (Exception)
                    {
                    }
                })
            );
                //_drones.ForEach(x => x.Update(location));

            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
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

            if (Util.GameSettings != null)
                _drones = _drones.Take(Util.GameSettings.MaxNumDronesPerConquestSquad).ToList();

            if (_drones.Count < val)
            {
                Util.GetInstance().Log("[ConquestDroneSquad.UpdateComposition] " + (val - _drones.Count) + " drones lost fromk player " + _ownerId);
            }
        }

        public void AddDrone(ConquestDrone drone)
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

        public void ClearAllDrones()
        {
            foreach (var dr in _drones)
            {
                dr.DeleteShip();
            }
            _drones.Clear();
        }

        public void StopAllDrones()
        {
            foreach (var dr in _drones)
            {
                dr.Stop();
            }
        }

        public bool InCombat()
        {
            return _drones.Any(x => x.HasTarget());
        }
    }
}
