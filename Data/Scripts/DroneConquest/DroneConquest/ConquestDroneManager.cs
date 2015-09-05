
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    class ConquestDroneManager
    {
        
        private List<ConquestDroneSquad> _squads = new List<ConquestDroneSquad>();
        private static ConquestDroneManager _instance;
        private Dictionary<Vector3D, int> _combatSites = new Dictionary<Vector3D, int>();
        private Dictionary<IMyVoxelBase, int> _asteroids = new Dictionary<IMyVoxelBase, int>();
        Spawner spawner = new Spawner();
        
        public int MaxNumConquestSquads = 10;
        private int DronesPerConquestSquad = 4;
        public int MaxNumGuardingDroneSquads = 3;
        public int NumGuardingDroneSquads;
        public static int DroneMaxRange = 10000;
        string logpath = "generatemission.txt";
        Random r = new Random();

        private Vector3D _mothershipLocation = Vector3D.Zero;
        private long _mothershipID = 2000;

        public static ConquestDroneManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ConquestDroneManager();
            }
            return _instance;
        }

        private int ticks;

        public void Update()
        {
            
            //spawner.SpawnShip(ConquestDrones.SmallOne, new Vector3D(0,0,0));
            //spawner.SpawnShip(ConquestDrones.SmallTwo, new Vector3D(0, 0, 0));

            NumGuardingDroneSquads = 0;
            
            

            if (Util.GameSettings != null)
            {
                DronesPerConquestSquad = Util.GameSettings.MaxNumDronesPerConquestSquad;
                MaxNumConquestSquads = Util.GameSettings.MaxNumConquestSquads;
                MaxNumGuardingDroneSquads = Util.GameSettings.MaxNumGuardingDroneSquads;
                DroneMaxRange = Util.GameSettings.ConquestInfluenceRange;
            }
            _squads = _squads.Where(x => x.DroneCount() > 0).ToList();
            
            //i cant think of a way to make this work without two loops, maybe its easy... idk
            foreach (var squad in _squads)
            {
                UpdateSquadMission(squad);
            }

            _combatSites.Clear();

            foreach (var squad in _squads)
            {
                squad.Update();
                SquadCallback(squad.InCombat(), squad.GetLocation());
            }

            _motherships = _motherships.Where(x => x.IsAlive()).ToList();

            if (_motherships.Any())
            {
                _mothershipLocation = _motherships.First().GetPosition();
                _motherships.First().Update(_asteroids.Keys.Select(x => x.GetPosition()).ToList());
            }

            ticks++;
        }

        public void UpdateSquadMission(ConquestDroneSquad squad)
        {
            ConquestMission mission = null;
            if (squad.Mission()!=null && squad.Mission().MissionType == ActionTypes.Patrol)
            {
                mission = GenerateMission(squad,true);
                if (mission != null)
                {
                    squad.IssueMission(mission);
                    squad.SetOwner(GetMothershipID());
                    Util.GetInstance().Log("[ConquestDroneManager.UpdateSquadMission] Switching from patrol mission to assist Mission " + squad.myid, logpath);
                }

            }

            if (mission == null)
            {
                mission = squad.ReadyForNewMission(ticks) ? GenerateMission(squad) : squad.Mission();
                squad.IssueMission(mission);
                squad.SetOwner(GetMothershipID());
            }
        }

        public void RebuildLostDrones()
        {
            ConquestDrones type;
                var val = r.Next(10);

            switch (val)
            {
                case 1:
                        type = ConquestDrones.SmallOne;
                        break;
                case 2:
                        type = ConquestDrones.SmallTwo;
                        break;
                case 3:
                        type = ConquestDrones.SmallThree;
                        break;
                case 4:
                        type = ConquestDrones.MediumOne;
                        break;
                //case 0:
                //        type = ConquestDrones.MediumTwo;
                //        break;
                default:
                        type = ConquestDrones.SmallTwo;
                        break;
            }

            if (_motherships.Count == 0)
            {
                
                    type = ConquestDrones.LargeOne;
            }

            var dro = GetDrones();

            if (DronesPerConquestSquad * MaxNumConquestSquads > dro.Count)
                spawner.SpawnShip(type, new Vector3D(r.Next(DroneMaxRange/2), r.Next(DroneMaxRange/2), r.Next(DroneMaxRange/2)));
            
        }

        private ConquestMission GenerateMission(ConquestDroneSquad squad, bool assistOnly = false)
        {

            bool readyForNewMission = squad.ReadyForNewMission(ticks);
            bool previousMissionGuard = squad.Mission() != null && squad.Mission().MissionType == ActionTypes.Guard;

            if (!readyForNewMission && !assistOnly)
                return null;

            if (!previousMissionGuard)
                Util.GetInstance().Log("[ConquestDroneManager.GenerateMission] PreviousMission Expired " + squad.myid, logpath);

            if (squad.Mission() != null && squad.Mission().IsAsteroid)
                _asteroids[squad.Mission().asteroid] = _asteroids[squad.Mission().asteroid] + 1;

            bool ong = false;
            Vector3D loc = Vector3D.Zero;
            int start = ticks;
            ConquestMission mission = null;


            if(!assistOnly)
            //try to patrol around the mothership if there are open positions
            if (NumGuardingDroneSquads < MaxNumGuardingDroneSquads)
            {
                loc = _mothershipLocation;
                // the 0 is to make this mission always last only one command cycle, so if the squad dies I wont have to so multipule updates removing it as a guarding squad
                mission = new ConquestMission(ong, loc, start, ActionTypes.Guard);
                //Util.GetInstance().Log("[ConquestDroneManager.GenerateMission] guard Home "+squad.myid, logpath);
                NumGuardingDroneSquads++;
                return mission;
            }

            //if any drones are in need of assistance then assist them
            foreach (var pair in _combatSites)
            {
                if (!(CalculateTargetPriority(pair.Key) >= _combatSites[pair.Key] + 1))
                    continue;

                Util.GetInstance()
                    .Log(
                        "[ConquestDroneManager.GenerateMission] Number of combat sites in need of help " +
                        _combatSites.Count(), logpath);
                loc = pair.Key*3000;
                _combatSites[pair.Key] = _combatSites[pair.Key] + 1;
                mission = new ConquestMission(ong, loc, start, ActionTypes.Assist);
                Util.GetInstance()
                    .Log("[ConquestDroneManager.GenerateMission] joining in combat " + squad.myid, logpath);

                return mission;
               
            }

            //i dont think visiting asteroids is nessessary, they explore enough to find players as is
            //if (!assistOnly)
            //{
            //    var asteroids =
            //        _asteroids.OrderBy(x => x.Value)
            //            .Where(y => (y.Key.GetPosition() - squad.GetLocation()).Length() < DroneMaxRange);
            //    //if there are nearby unexplored asteroids explore them
            //    foreach (var pair in asteroids)
            //    {
            //        int max = _asteroids.Values.Max();
            //        if (pair.Value < max || _asteroids.Count == _asteroids.Count(x=>x.Value == max))
            //        {
            //            loc = pair.Key.GetPosition();
            //            mission = new ConquestMission(ong, loc, start, ActionTypes.Assist, DroneMaxRange/15, true,
            //                pair.Key);
            //            Util.GetInstance()
            //                .Log("[ConquestDroneManager.GenerateMission] orbit asteroid " + squad.myid, logpath);
            //        }
            //        return mission;
            //    }
            //}

            if (assistOnly)
                return mission;
            //generate a random patrol route
            
            int X = r.Next(-DroneMaxRange,DroneMaxRange);
            int Y = r.Next(-DroneMaxRange,DroneMaxRange);
            int Z = r.Next(-DroneMaxRange, DroneMaxRange);

            loc = _mothershipLocation + new Vector3D(X, Y, Z);
            Util.GetInstance().Log("[ConquestDroneManager.GenerateMission] Patrol loc:" +loc+" "+ squad.myid, logpath);
            mission = new ConquestMission(ong, loc, start, ActionTypes.Patrol, DroneMaxRange/1000*60);

            return mission;
        }

        private int CalculateTargetPriority(Vector3D location)
        {
            //if (_motherships.Any())
            //{
            //    Util.GetInstance().Log("[ConquestDroneManager.CalculateTargetPriority] Number of reinforce squads:" + (5 - ((int)(_motherships[0].GetPosition() - location*3000).Length() / 5000)), logpath);
            //    return ((int) (_motherships[0].GetPosition() - location*3000).Length());
            //}
            return 1;
        }

        private void SquadCallback(bool inCombat, Vector3D location)
        {
            location = (new Vector3D((int) location.X/3000, (int) location.Y/3000, (int) location.Z/3000));
            if (inCombat)
            {
                if (!_combatSites.ContainsKey(location))
                {
                    //keep the casts
                    _combatSites.Add(location, 1);
                }
            }
        }

        public void AddDrone(ConquestDrone drone)
        {
            foreach (var squad in _squads)
            {
                if (squad.DroneCount() < DronesPerConquestSquad)
                {
                    Util.GetInstance().Log("[ConquestDronemanager.AddDrone] drone added!");
                    squad.AddDrone(drone);
                    return;
                }
            }

            if (_squads.Count < MaxNumConquestSquads)
            {
                var sq = new ConquestDroneSquad(drone.GetOwnerId());
                sq.AddDrone(drone);
                Util.GetInstance().Log("[ConquestDronemanager.AddDrone] squad created: drone added!");
                _squads.Add(sq);
                sq.SetOwner(drone.GetOwnerId());
            }
        }

        public void ClearAllDrones()
        {
            foreach (var sq in _squads)
            {
                sq.ClearAllDrones();
            }
        }

        public void StopAllDrones()
        {
            foreach (var sq in _motherships)
            {
                sq.Stop();
            }
            foreach (var sq in _squads)
            {
                sq.StopAllDrones();
            }
        }

        public void AddDiscoveredAsteroid(IMyVoxelBase asteroid)
        {
            if (!_asteroids.ContainsKey(asteroid))
            {
                _asteroids.Add(asteroid, 1);
            }
        }
        
        public long GetMothershipID()
        {
            
            return _mothershipID;
        }

        public HashSet<Drone> GetDrones()
        {
            var set = new HashSet<Drone>(_squads.SelectMany(x=>x.GetDrones()));
            return set;
        }


        public List<MothershipDrone> GetMotherships()
        {
            return _motherships;
        } 
        private List<MothershipDrone> _motherships = new List<MothershipDrone>();

        public void AddMothership(MothershipDrone dro)
        {
            dro.SetOwner(GetMothershipID());
            _motherships.Add(dro);
        }
    }
}
