using System;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.ModAPI;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.Ingame.IMyCubeGrid;


namespace DroneConquest
{
    public class PlayerDrone : Drone
    {

        private ActionTypes _currentOrder = ActionTypes.Guard;
        private bool _isOnline = true;
        Standing standing = Standing.Hostile;
        private Vector3D SentryLocation;
        private int ticks = 0;

        public void Update(Vector3D location)
        {
            try
            {
                if (ticks%200 == 0)
                    TakeInPlayerNameLineArgs();
                Util.GetInstance().Log("[Drone.Guard] Start: " + myNumber);

                if (ticks%20 == 0)
                    FindNearbyStuff();

                if (_isOnline)
                {
                    switch (_currentOrder)
                    {
                        case ActionTypes.Guard:
                            if (mode == DroneModes.AtRange)
                            {
                                if (standing == Standing.Hostile)
                                    Guard(location);
                                else
                                    Orbit(location);
                            }
                            else
                            {
                                if (standing == Standing.Hostile)
                                    AgressiveFormation(location);
                                else
                                    Formation(location);
                            }
                            break;
                        case ActionTypes.Orbit:
                            Orbit(location);
                            break;
                        case ActionTypes.Return:
                            navigation.Follow(location);
                            break;
                        case ActionTypes.Sentry:
                            Guard(SentryLocation);
                            break;
                    }
                }
                else
                {
                    _beaconName = "Offline";
                    //Stop();
                    NameBeacon();
                }
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
            Util.GetInstance().Log("[Drone.Guard] End: " + myNumber);
            NameBeacon();
            ticks++;
        }

        private Vector3D myRelativeFormation = Vector3D.Zero;
        Random r = new Random();
        private void Formation(Vector3D location)
        {
            

            GetRelativeVector();

            var formationVector = location + myRelativeFormation;
            navigation.AvoidNearbyEntities();

            Util.GetInstance().Log("Formation position: pos:" + (formationVector + myRelativeFormation), "formations.txt");
            _beaconName = "FR";
            navigation.Follow(formationVector, 0);
            NameBeacon();
        }

        private void GetRelativeVector()
        {
            if (myRelativeFormation == Vector3D.Zero)
            {
                int x = r.Next(70, 100);
                int y = r.Next(70, 100);
                int z = r.Next(70, 100);

                Util.GetInstance().Log("Relative formation position: x:" + x + " y:" + y + " z:" + z, "formations.txt");

                x = x * ((r.Next(2) > 0) ? 1 : -1);
                y = y * ((r.Next(2) > 0) ? 1 : -1);
                z = z * ((r.Next(2) > 0) ? 1 : -1);
                myRelativeFormation = new Vector3D(x, y, z);
            }
        }

        private void AgressiveFormation(Vector3D location)
        {
            GetRelativeVector();

            var formationVector = location + myRelativeFormation;
            navigation.AvoidNearbyEntities();

            if (FindNearbyAttackTarget())
            {
                Util.GetInstance().Log("In Combat: " +location, "formations.txt");
                _beaconName = "FR/AT";
                return;
            }

            Util.GetInstance().Log("Formation position: pos:" + (formationVector + myRelativeFormation), "formations.txt");

            navigation.Follow(formationVector, 0);
            NameBeacon();
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
                        //_entitiesNearDcDrones.Add(closeItem)
                        AddNearbyAsteroid((IMyVoxelBase)closeItem);
                    }
                }
                catch
                {
                    //This catches duplicate key entries being put in KnownEntities.
                }
            }
        }
        private void TakeInPlayerNameLineArgs()
        {
            try
            {
                if (ShipControls != null && ((IMyCubeBlock) ShipControls).IsWorking)
                {
                    string[] args = ((IMyRemoteControl) ShipControls).CustomName.Trim().Split('#');
                    foreach (var arg in args)
                    {
                        string[] commandValue = arg.Split(':');

                        if (commandValue.Length == 2)
                        {
                            switch (commandValue[0].ToLower())
                            {
                                case "orbitrange":
                                    SetOrbitRadius(commandValue);
                                    break;
                                case "broadcast":
                                    SetBroadcastingType(commandValue);
                                    break;
                                case "order":
                                    SetOrderType(commandValue);
                                    break;
                                case "on/off":
                                    EnableDisable(commandValue);
                                    break;
                                case "standing":
                                    SetStanding(commandValue);
                                    break;
                                case "mode":
                                    SetMode(commandValue);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
        }
        DroneModes mode = DroneModes.AtRange;
        private void SetMode(string[] args)
        {
            if (args.Length != 2 && args[1].Length > 1)
                return;

            if (args[1].ToLower().Contains("atrange"))
            {
                mode = DroneModes.AtRange;
            }
            else if (args[1].ToLower().Contains("fighter"))
            {
                mode = DroneModes.Fighter;
            }
        }


        private void SetStanding(string[] args)
        {
            if (args.Length != 2 && args[1].Length > 1)
                return;

            if (args[1].ToLower().Contains("hostile"))
            {
                standing = Standing.Hostile;
            }
            else if (args[1].ToLower().Contains("passive"))
            {
                standing = Standing.Passive;
            }

            Util.GetInstance().Log("[PlayerDrone.Update] updating orbitrange to " + args[1]);
        }


        private void SetOrbitRadius(string[] args)
        {
            if (args.Length != 2 && args[1].Length>1)
                return;

            double radius = navigation.FollowRange;
            if(double.TryParse(args[1], out radius))
                Util.GetInstance().Log("[PlayerDrone.Update] updating orbitrange to "+args[1]);
            navigation.FollowRange = (float)radius;
        }

        private void SetBroadcastingType(string[] args)
        {
            if (args.Length != 2)
                return;

            if (args[1].ToLower().Contains("beacon"))
            {
                broadcastingType = BroadcastingTypes.Beacon;
                Util.GetInstance().Log("[PlayerDrone.Update] updating broadcastingtype to " + args[1]);
            }
            else if(args[1].ToLower().Contains("antenna"))
            {
                broadcastingType = BroadcastingTypes.Antenna;
                Util.GetInstance().Log("[PlayerDrone.Update] updating broadcastingtype to " + args[1]);
            }
        }

        

        private void SetOrderType(string[] args)
        {
            if (args.Length != 2)
                return;

            switch (args[1].ToLower().Trim())
            {
                case "return":
                    _currentOrder = ActionTypes.Return;
                    Util.GetInstance().Log("[PlayerDrone.Update] updating order to " + args[1]);
                    break;

                case "guard":
                    _currentOrder = ActionTypes.Return;
                    Util.GetInstance().Log("[PlayerDrone.Update] updating order to " + args[1]);
                    break;

                case "sentry":
                    if (_currentOrder != ActionTypes.Sentry)
                        SentryLocation = Ship.GetPosition();

                    _currentOrder = ActionTypes.Sentry;

                    Util.GetInstance().Log("[PlayerDrone.Update] updating order to " + args[1]);
                    break;
                case "orbit":
                    _currentOrder = ActionTypes.Orbit;
                    Util.GetInstance().Log("[PlayerDrone.Update] updating order to " + args[1]);
                    break;
            }

        }

        private void EnableDisable(string[] args)
        {
            if (args.Length != 2)
                return;

            if (args[1].ToLower().Contains("on"))
            {
                Util.GetInstance().Log("[PlayerDrone.Update] updating on/off to " + args[1]);
                _isOnline = true;
            }
            else if(args[1].ToLower().Contains("off"))
            {
                Util.GetInstance().Log("[PlayerDrone.Update] updating on/off to " + args[1]);
                _isOnline = false;
            }
        }


        public PlayerDrone(IMyEntity entity, BroadcastingTypes broadcasting)
            : base(entity, broadcasting)
        {
            SentryLocation = entity.GetPosition();
            Type = typeof(PlayerDrone);

        }
    }
}
