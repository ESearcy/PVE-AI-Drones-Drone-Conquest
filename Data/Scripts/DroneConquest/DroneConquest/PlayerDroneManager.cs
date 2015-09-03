using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    

    public class PlayerDroneManager
    {
        Dictionary<long, PlayerDroneSquad> DroneOwners = new Dictionary<long, PlayerDroneSquad>();
        Dictionary<long, Vector3D> _knownPlayerLocations = new Dictionary<long, Vector3D>(); 
        private static PlayerDroneManager _instance = null;
        private int DronesPerPlayerSquad = 4;

        public static PlayerDroneManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PlayerDroneManager();
            }
            return _instance;
        }

        public HashSet<Drone> GetDrones()
        {
            var set = new HashSet<Drone>(DroneOwners.Values.SelectMany(x => x.GetDrones()));
            return set;
        }

        private int ticks = 0;
        public void Update()
        {
            UpdateKnownPlayerLocations();

            if (Util.GameSettings != null)
            {
                DronesPerPlayerSquad = Util.GameSettings.MaxPlayerDroneCount;
            }
            foreach (var pair in DroneOwners)
            {
                ParallelUpdateDrones(pair);
            }
            

            ticks++;
        }

        private void ParallelUpdateDrones(KeyValuePair<long, PlayerDroneSquad> keyValuePair)
        {
            Vector3D location = Vector3D.Zero;
            if (_knownPlayerLocations.ContainsKey(keyValuePair.Key))
            {
                location = _knownPlayerLocations[keyValuePair.Key];
                //Util.GetInstance().Notify("following player");
            }
            else
            {
                _knownPlayerLocations.Add(keyValuePair.Key, keyValuePair.Value.GetLocation());
                location = keyValuePair.Value.GetLocation();
                //Util.GetInstance().Notify("following myself");
                
            }
            keyValuePair.Value.Update(location);
        }

        private void UpdateKnownPlayerLocations()
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {
                if (!_knownPlayerLocations.ContainsKey(player.PlayerID))
                {
                    _knownPlayerLocations.Add(player.PlayerID, player.GetPosition());
                }
                else
                {
                    _knownPlayerLocations.Remove(player.PlayerID);
                    _knownPlayerLocations.Add(player.PlayerID, player.GetPosition());

                }
            }
        }

        public void AddDrone(PlayerDrone drone)
        {
            if (DroneOwners.Keys.Contains(drone.GetOwnerId()))
            {
                if (DroneOwners[drone.GetOwnerId()].DroneCount() < DronesPerPlayerSquad)
                {
                    DroneOwners[drone.GetOwnerId()].AddDrone(drone);
                    Util.GetInstance().Log("[PlayerDronemanager.AddDrone] squad existed: drone added!");
                }
            }
            else
            {
                var sq = new PlayerDroneSquad(drone.GetOwnerId());
                sq.SetOwner(drone.GetOwnerId());
                sq.AddDrone(drone);
                Util.GetInstance().Log("[PlayerDronemanager.AddDrone] squad created: drone added!");
                DroneOwners.Add(drone.GetOwnerId(), sq);
            }
        }


        public void StopAllDrones()
        {
            foreach (var squad in DroneOwners.Values)
            {
                squad.StopAllDrones();
            }
        }

        public void ClearAllDrones()
        {
            foreach (var squad in DroneOwners.Values)
            {
                squad.ClearAllDrones();
            }
        }
    }
}
