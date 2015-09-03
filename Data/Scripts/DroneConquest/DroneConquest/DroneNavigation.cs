using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMyGyro = Sandbox.ModAPI.IMyGyro;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

namespace DroneConquest
{
    class DroneNavigation
    {
        //need these set
        public readonly IMyGridTerminalSystem GridTerminalSystem;
        public IMyCubeGrid Ship;
        private IMyControllableEntity _shipControls;

        //for orbiting
        private int _currentCoord;
        private int _previousCoord = 7;
        private List<Vector3D> _coords = new List<Vector3D>();
        private int _avoidanceMod = 20;

        #region NavigationVariables

        private int _avoidNumTargets = 5;
        private float bigOrbitRange = 5000;
        public float FollowRange;
        private List<AvoidedTarget> _recentlyAvoided = new List<AvoidedTarget>();
        private List<IMyEntity> _nearbyFloatingObjects;
        private List<IMyVoxelBase> _nearbyAsteroids = new List<IMyVoxelBase>();

        //when drones are approaching a target this is the distance used to calculate weather or not
        //they need to use the special ApproachSpeedMod devisor for calculating max speed
        private const int TargetApproachModRange = 300;
        public const int ApproachSpeedMod = 6;
        public double MaxSpeed = 100;

        public bool Avoiding;
        private double _avoidanceRange = 200;

        #endregion

        #region AlignmentNavigation variables

        //alignment 
        private bool _initialized = false;
        private bool _operational = true;
        private int alignCount = 10;
        private float alignSpeedMod = .05f;

        //Things needed to align to things
        private List<IMyTerminalBlock> _gyros = new List<IMyTerminalBlock>();

        private List<string> _gyroYaw;
        private List<string> _gyroPitch;
        private List<int> _gyroYawReverse;
        private List<int> _gyroPitchReverse;

        private double _degreesToVectorYaw = 0;
        private double _degreesToVectorPitch = 0;

        private Base6Directions.Direction _shipUp = 0;
        private Base6Directions.Direction _shipLeft = 0;

        #endregion

        public DroneNavigation(IMyCubeGrid ship, IMyControllableEntity shipControls,
            List<IMyEntity> nearbyFloatingObjects, double maxEngagementRange)
        {
            //stuff passed from sip
            _shipControls = shipControls;
            Ship = ship;
            GridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ship);
            _nearbyFloatingObjects = nearbyFloatingObjects;

            var value = (ship.LocalAABB.Max - ship.LocalAABB.Center).Length();

            if (ship.Physics.Mass > 100000)
                FollowRange = 600 + value;
            else
                FollowRange = 400 + value;

            ShipOrientation();
            FindGyros();

            _initialized = true;

        }

        Vector3D previousAlign = Vector3D.Zero;
        //Working. The vector passed here must be a mean of all avoidance vectors devided by number of vectors
        //to get this
        //Take the positions of each nearby object and subtract your position from that enemy position. this will give you a Vector pointing away from target
        // to avoid multipule targets at once, Add all vectors together and devide by number of vectors
        public void AvoidTarget(Vector3D direction)
        {
            
            _shipControls.MoveAndRotate((direction+Ship.Physics.LinearVelocity)/2, Vector2.Zero, 0);
            AlignTo(previousAlign);
        }

        //yup
        public void TurnOffGyros(bool off)
        {
            for (int i = 0; i < _gyros.Count; i++)
            {
                if (((IMyGyro) _gyros[i]).GyroOverride != off)
                {
                    TerminalBlockExtentions.ApplyAction(_gyros[i], "Override");
                }
            }
        }

        public int AlignTo(Vector3D position)
        {
            for (int i = 0; i < alignCount && GyrosWork(); i++)
            {
                double realDistance = (position - Ship.GetPosition()).Length();
                DegreesToVector(position);
                PointToVector(0);
            }
            previousAlign = position;
            Util.GetInstance().Log("[DroneNavigation.AlignTo] returning pitch:"+Math.Abs(_degreesToVectorPitch) +" yaw:"+ Math.Abs(_degreesToVectorYaw));
            return (int)(Math.Abs(_degreesToVectorPitch) + Math.Abs(_degreesToVectorYaw));

        }

        public void PointToVector(double precision)
        {
            if (_gyros != null)
            {
                for (int i = 0; i < _gyros.Count; i++)
                {
                    try
                    {
                        var gyro = _gyros[i] as IMyGyro;
                        if (!gyro.GyroOverride)
                        {
                            gyro.GetActionWithName("Override").Apply(gyro);
                        }
                        if (Math.Abs(_degreesToVectorYaw) > precision)
                        {
                            gyro.SetValueFloat(_gyroYaw[i],
                                (float) (_degreesToVectorYaw*alignSpeedMod)*(_gyroYawReverse[i]));
                        }
                        else
                        {
                            gyro.SetValueFloat(_gyroYaw[i], 0);
                        }
                        if (Math.Abs(_degreesToVectorPitch) > precision)
                        {
                            gyro.SetValueFloat(_gyroPitch[i],
                                (float) (_degreesToVectorPitch*alignSpeedMod)*(_gyroPitchReverse[i]));
                        }
                        else
                        {
                            gyro.SetValueFloat(_gyroPitch[i], 0);
                        }
                    }
                    catch (Exception e)
                    {
                        //Util.Notify(e.ToString());
                        //This is only to catch the occasional situation where the ship tried to align to something but has between the time the method started and now has lost a gyro or whatever
                    }
                }
            }
        }

        public void ShipOrientation()
        {
            if (_shipControls != null)
            {
                var Origin = ((IMyCubeBlock) _shipControls).GetPosition();
                var Up = Origin + (((IMyCubeBlock) _shipControls).LocalMatrix.Up);
                var Forward = Origin + (((IMyCubeBlock) _shipControls).LocalMatrix.Forward);
                var Left = Origin + (((IMyCubeBlock) _shipControls).LocalMatrix.Left);

                Vector3D forwardVector = Forward - Origin;
                Vector3D upVector = Up - Origin;
                Vector3D leftVector = Left - Origin;

                leftVector.Normalize();
                forwardVector.Normalize();
                upVector.Normalize();

                _shipUp = Base6Directions.GetDirection(upVector);
                _shipLeft = Base6Directions.GetDirection(leftVector);
            }
        }

        public void CompleteStop()
        {
            StopSpin();
            _shipControls.MoveAndRotateStopped();
            
        }

        private void StopSpin()
        {
            FindGyros();
            if (_gyros != null)
            {
                for (int i = 0; i < _gyros.Count; i++)
                {
                    try
                    {
                        if (((IMyGyro)_gyros[i]).GyroOverride)
                            _gyros[i].GetActionWithName("Override").Apply(_gyros[i]);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        //recalculate orbit vectors based on the position passed in
        //also configures mothership flight path by generating 50 random points
        public void ResetOrbitCoords(Vector3D pos, float range)
        {
            if (range > 0)
            {
                var x = pos.X;
                var y = pos.Y;
                var z = pos.Z;
                Random r = new Random();
                int val = r.Next(2);
                _coords.Clear();
                var cornerRange = range*.8;

                
                _coords.Add(new Vector3D(range + x, 0 + y, 0 + z));
                _coords.Add(new Vector3D(cornerRange + x, cornerRange + y, 0 + z));
                _coords.Add(new Vector3D(0 + x, range + y, 0 + z));
                _coords.Add(new Vector3D(-cornerRange + x, cornerRange + y, 0 + z));
                _coords.Add(new Vector3D(-range + x, 0 + y, 0 + z));
                _coords.Add(new Vector3D(-cornerRange + x, -cornerRange + y, 0 + z));
                _coords.Add(new Vector3D(0 + x, -range + y, 0 + z));
                _coords.Add(new Vector3D(cornerRange + x, -cornerRange + y, 0 + z));
            }
            else
            {
                if (_coords.Count < 25)
                {
                    Random r = new Random();
                    for (int i = 0; i < 50; i++)
                    {
                        _coords.Add(new Vector3D(r.Next(100000), r.Next(100000), r.Next(100000)));
                    }
                }
            }
        }

        //Working, 
        public bool Orbit(Vector3D target)
        {
            if (!Avoiding && _shipControls != null)
            {
                //MyAPIGateway.Session.Factions.TryGetPlayerFaction();
                var distance = (target - Ship.GetPosition()).Length();
                MaxSpeed = distance/ApproachSpeedMod;

                if (MaxSpeed > 40)
                    MaxSpeed = 40;

                if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                {
                    _shipControls.MoveAndRotateStopped();
                }
                else
                {
                    //Util.Notify("cp1");/
                    //validateTarget(); //This is just to make sure our owner/target was not destoried
                    ResetOrbitCoords(target, FollowRange);


                    if (((_coords[_currentCoord + 1 >= _coords.Count ? 0 : _currentCoord + 1] - Ship.GetPosition()).Length()
                        < (_coords[_currentCoord] - Ship.GetPosition()).Length()) && (_coords[_currentCoord] - Ship.GetPosition()).Length()<FollowRange/4)
                    {
                        //Util.Notify("Skipping");
                        _currentCoord = _currentCoord + 1;
                    }

                    
                    if (_currentCoord >= _coords.Count)
                        _currentCoord = 0;

                    NavInfo nav = new NavInfo(Ship.GetPosition(), _coords[_currentCoord], (IMyEntity) _shipControls);
                    AlignTo(_coords[_currentCoord]);
                    //_toPosition - _fromPosition
                    if (nav.Direction.Length() < (_coords[_currentCoord] - _coords[_previousCoord]).Length()*.66)
                    {
                        _previousCoord = _currentCoord;
                        _currentCoord++;

                        if (_currentCoord >= _coords.Count)
                            _currentCoord = 0;
                    }

                    if (Math.Abs(nav.Direction.LengthSquared()) > 0 || Math.Abs(nav.Rotation.LengthSquared()) > 0)
                    {
                        _shipControls.MoveAndRotate(nav.Direction, nav.Rotation, nav.Roll);

                        //this calculates max speed based on distance
                        if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                        {
                            _shipControls.MoveAndRotateStopped();
                        }
                    }
                    else
                        _shipControls.MoveAndRotateStopped();
                }
                return true;
            }

            //indicates the drone was avoiding rather than orbiting
            return false;
        }

        public bool Orbit(List<Vector3D> targets)
        {
            try
            {
                if (!Avoiding && _shipControls != null)
                {
                    if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                    {
                        _shipControls.MoveAndRotateStopped();
                    }
                    else
                    {

                        ResetOrbitCoords(targets);
                        Util.GetInstance()
                        .Log(
                            "[ConquestDroneManager.GenerateMission] number coords: " + _coords.Count() + " current coord " + _currentCoord,
                            "mothershipFlightPath.txt");
                        if ((_coords[_currentCoord] - Ship.GetPosition()).Length() < FollowRange/2)
                        {
                            //Util.Notify("Skipping");
                            _currentCoord = _currentCoord + 1;
                        }

                        Util.GetInstance()
                        .Log(
                            "[ConquestDroneManager.GenerateMission] cp1",
                            "mothershipFlightPath.txt");
                        if (_currentCoord >= _coords.Count)
                            _currentCoord = 0;

                        NavInfo nav = new NavInfo(Ship.GetPosition(), _coords[_currentCoord],
                            (IMyEntity) _shipControls);
                        AlignTo(_coords[_currentCoord]);
                        //_toPosition - _fromPosition
                        Util.GetInstance()
                        .Log(
                            "[ConquestDroneManager.GenerateMission] cp2",
                            "mothershipFlightPath.txt");
                        if (nav.Direction.Length() < (_coords[_currentCoord] - _coords[_previousCoord]).Length()*.66)
                        {
                            _previousCoord = _currentCoord;
                            _currentCoord++;

                            if (_currentCoord >= _coords.Count)
                                _currentCoord = 0;
                        }

                        Util.GetInstance()
                        .Log(
                            "[ConquestDroneManager.GenerateMission] cp3",
                            "mothershipFlightPath.txt");

                        if (Math.Abs(nav.Direction.LengthSquared()) > 0 ||
                            Math.Abs(nav.Rotation.LengthSquared()) > 0)
                        {
                            _shipControls.MoveAndRotate(nav.Direction, nav.Rotation, nav.Roll);

                            //this calculates max speed based on distance
                            if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                            {
                                _shipControls.MoveAndRotateStopped();
                            }
                        }
                        else
                            _shipControls.MoveAndRotateStopped();
                    }
                    return true;

                }
            } //indicates the drone was avoiding rather than orbiting
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
            return false;
        }

        //this is to set the motherships orbit
        private void ResetOrbitCoords(List<Vector3D> positions)
        {
            try
            {
                if (positions.Any())
                {
                    Util.GetInstance()
                        .Log(
                            "[ConquestDroneManager.GenerateMission] new coorpds list --------------------------------------------",
                            "mothershipFlightPath.txt");
                    _coords.Clear();
                    _currentCoord = 0;
                    foreach (var loc in positions)
                    {
                        _coords.Add(loc + new Vector3D());
                        Util.GetInstance()
                            .Log(
                                "[ConquestDroneManager.GenerateMission] Asteroid " +
                                (loc + new Vector3D(1000, 1000, 1000)),
                                "mothershipFlightPath.txt");
                    }
                    _previousCoord = _coords.Count()-1;
                }
                else if (!_coords.Any())
                {
                    _coords.Clear();
                    _currentCoord = 0;
                    var range = ConquestDroneManager.DroneMaxRange;

                    int i = 0;
                    Random r = new Random();
                    do
                    {
                        var vect = new Vector3D(r.Next(range), r.Next(range), r.Next(range));
                        _coords.Add(Vector3D.Zero + vect);
                        Util.GetInstance()
                            .Log("[ConquestDroneManager.GenerateMission] CustomPoint " + vect,
                                "mothershipFlightPath.txt");
                        i++;
                    } while (i < 40);
                    _previousCoord = _coords.Count() - 1;
                }
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
        }

        public bool CombatOrbit(Vector3D target)
        {
            if (!Avoiding && _shipControls != null)
            {
                var distance = (target - Ship.GetPosition()).Length();
                //MaxSpeed = ;
                //MaxSpeed = (target - Ship.GetPosition()).Length() < FollowRange*.8 ? 20 : MaxSpeed;

                //oh no... a turnary inside a turnary... someone burn me on a stick... nah its okay if I do it this one time
                MaxSpeed = MaxSpeed < 10 ? 10 : 
                                distance > FollowRange ? distance / ApproachSpeedMod : 20;

                

                if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                {
                    _shipControls.MoveAndRotateStopped();
                }
                else
                {
                    //Util.Notify("cp1");
                    //validateTarget(); //This is just to make sure our owner/target was not destoried
                    ResetOrbitCoords(target, FollowRange);


                    NavInfo nav = new NavInfo(Ship.GetPosition(), _coords[_currentCoord], (IMyEntity) _shipControls);

                    //_toPosition - _fromPosition for distance or to generate a vector twords target
                    if (nav.Direction.Length() < (_coords[_currentCoord] - _coords[_previousCoord]).Length()*.66)
                    {
                        _previousCoord = _currentCoord;
                        _currentCoord++;

                        if (_currentCoord >= _coords.Count)
                            _currentCoord = 0;
                    }

                    if (Math.Abs(nav.Direction.LengthSquared()) > 0 || Math.Abs(nav.Rotation.LengthSquared()) > 0)
                    {
                        _shipControls.MoveAndRotate(nav.Direction, Vector2.Zero, nav.Roll);

                        //this calculates max speed based on distance
                        if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                        {
                            _shipControls.MoveAndRotateStopped();
                        }
                    }
                    else
                        _shipControls.MoveAndRotateStopped();
                }
                return true;
            }

            //indicates the drone was avoiding rather than orbiting
            return false;
        }

        //Working
        public bool Follow(Vector3D position)
        {
            if (Ship!=null && !Avoiding && _shipControls != null)
            {
                NavInfo nav = new NavInfo(Ship.GetPosition(), position, (IMyEntity) _shipControls);
                
                var distance = (position - Ship.GetPosition()).Length();
                MaxSpeed = distance > FollowRange ? distance/ApproachSpeedMod : 40;
                if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                {
                    _shipControls.MoveAndRotateStopped();
                }
                else
                {
                    AlignTo(position);

                    if (nav.Direction.Length() < FollowRange)
                    {
                        if (Ship.Physics.LinearVelocity.Normalize() > 0)
                        {
                            _shipControls.MoveAndRotateStopped();
                        }
                    }
                    else
                    {
                        if (Math.Abs(nav.Direction.Length()) > FollowRange)
                        {
                            _shipControls.MoveAndRotate(nav.Direction, nav.Rotation, nav.Roll);
                            AlignTo(position);
                        }
                    }

                    if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                    {
                        _shipControls.MoveAndRotateStopped();
                    }
                }
                return true;
            }

            //indicates the drone was avoiding rather than following
            return false;
        }

        public bool Follow(Vector3D position, int followDistance)
        {
            if (Ship != null && !Avoiding && _shipControls != null)
            {
                NavInfo nav = new NavInfo(Ship.GetPosition(), position, (IMyEntity)_shipControls);

                var distance = (position - Ship.GetPosition()).Length();
                MaxSpeed = distance > followDistance ? distance / ApproachSpeedMod : 40;
                if (distance > 100)
                    MaxSpeed = distance > followDistance ? distance / ApproachSpeedMod : 100;

                if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                {
                    _shipControls.MoveAndRotateStopped();
                }
                else
                {
                    AlignTo(position);

                    if (nav.Direction.Length() < followDistance)
                    {
                        if (Ship.Physics.LinearVelocity.Normalize() > 0)
                        {
                            _shipControls.MoveAndRotateStopped();
                        }
                    }
                    else
                    {
                        if (Math.Abs(nav.Direction.Length()) > followDistance)
                        {
                            _shipControls.MoveAndRotate(nav.Direction, nav.Rotation, nav.Roll);
                            AlignTo(position);
                        }
                    }

                    if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed)
                    {
                        _shipControls.MoveAndRotateStopped();
                    }
                }
                return true;
            }

            //indicates the drone was avoiding rather than following
            return false;
        }


        //calculated the pitch and roll needed to aim at the target
        public void DegreesToVector(Vector3D TV)
        {
            if (_shipControls != null)
            {
                var Origin = ((IMyCubeBlock) _shipControls).GetPosition();
                var Up = (((IMyCubeBlock) _shipControls).WorldMatrix.Up);
                var Forward = (((IMyCubeBlock) _shipControls).WorldMatrix.Forward);
                var Right = (((IMyCubeBlock) _shipControls).WorldMatrix.Right);
                // ExplainVector(Origin, "Origin");
                // ExplainVector(Up, "up");

                Vector3D OV = Origin; //Get positions of reference blocks.    
                Vector3D FV = Origin + Forward;
                Vector3D UV = Origin + Up;
                Vector3D RV = Origin + Right;

                //Get magnitudes of vectors.
                double TVOV = (OV - TV).Length();

                double TVFV = (FV - TV).Length();
                double TVUV = (UV - TV).Length();
                double TVRV = (RV - TV).Length();

                double OVUV = (UV - OV).Length();
                double OVRV = (RV - OV).Length();

                double ThetaP = Math.Acos((TVUV*TVUV - OVUV*OVUV - TVOV*TVOV)/(-2*OVUV*TVOV));
                //Use law of cosines to determine angles.    
                double ThetaY = Math.Acos((TVRV*TVRV - OVRV*OVRV - TVOV*TVOV)/(-2*OVRV*TVOV));

                double RPitch = 90 - (ThetaP*180/Math.PI); //Convert from radians to degrees.    
                double RYaw = 90 - (ThetaY*180/Math.PI);

                if (TVOV < TVFV) RPitch = 180 - RPitch; //Normalize angles to -180 to 180 degrees.    
                if (RPitch > 180) RPitch = -1*(360 - RPitch);

                if (TVOV < TVFV) RYaw = 180 - RYaw;
                if (RYaw > 180) RYaw = -1*(360 - RYaw);

                _degreesToVectorYaw = RYaw;
                _degreesToVectorPitch = RPitch;
            }

        }

        private void ExplainVector(Vector3D point, string name)
        {
            Util.GetInstance().Notify(name + ": (x) " + point.X + " (y)" + point.Y + " (z) " + point.Z);
        }

        public bool NavigationWorking()
        {
            List<string> errors = new List<string>();

            bool shipWorking = true;

            if (Ship == null)
            {
                errors.Add("Ship Grid Missing: ");
                shipWorking = false;
            }
            if (_shipControls == null)
            {
                errors.Add("Remote Control Missing: ");
                shipWorking = false;
            }
            if (!GyrosWork())
            {
                errors.Add("No Gyros Found: ");
                shipWorking = false;
            }

            return shipWorking;
        }

        //chwecking that there is atleast 1 gyro
        private bool GyrosWork()
        {
            FindGyros();
            if (_gyros.Count == 0)
                return false;
            return true;
        }

        public void AddNearbyAsteroid(IMyVoxelBase asteroid)
        {
            if (!_nearbyAsteroids.Contains(asteroid))
                _nearbyAsteroids.Add(asteroid);
        }

        internal class AvoidedTarget
        {
            public IMyEntity Entity = null;
            public int TimesAvoided = 0;
        }

        public void FindGyros()
        {
            _gyros = new List<IMyTerminalBlock>();
            _gyroYaw = new List<string>();
            _gyroPitch = new List<string>();
            _gyroYawReverse = new List<int>();
            _gyroPitchReverse = new List<int>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(_gyros);
            for (int i = 0; i < _gyros.Count; i++)
            {
                if ((_gyros[i]).IsFunctional)
                {
                    Base6Directions.Direction gyroUp = _gyros[i].Orientation.TransformDirectionInverse(_shipUp);
                    Base6Directions.Direction gyroLeft = _gyros[i].Orientation.TransformDirectionInverse(_shipLeft);


                    if (gyroUp == Base6Directions.Direction.Up)
                    {
                        _gyroYaw.Add("Yaw");
                        _gyroYawReverse.Add(1);
                    }
                    else if (gyroUp == Base6Directions.Direction.Down)
                    {
                        _gyroYaw.Add("Yaw");
                        _gyroYawReverse.Add(-1);
                    }
                    else if (gyroUp == Base6Directions.Direction.Left)
                    {
                        _gyroYaw.Add("Pitch");
                        _gyroYawReverse.Add(1);
                    }
                    else if (gyroUp == Base6Directions.Direction.Right)
                    {
                        _gyroYaw.Add("Pitch");
                        _gyroYawReverse.Add(-1);
                    }
                    else if (gyroUp == Base6Directions.Direction.Forward)
                    {
                        _gyroYaw.Add("Roll");
                        _gyroYawReverse.Add(-1);
                    }
                    else if (gyroUp == Base6Directions.Direction.Backward)
                    {
                        _gyroYaw.Add("Roll");
                        _gyroYawReverse.Add(1);
                    }

                    if (gyroLeft == Base6Directions.Direction.Up)
                    {
                        _gyroPitch.Add("Yaw");
                        _gyroPitchReverse.Add(1);
                    }
                    else if (gyroLeft == Base6Directions.Direction.Down)
                    {
                        _gyroPitch.Add("Yaw");
                        _gyroPitchReverse.Add(-1);
                    }
                    else if (gyroLeft == Base6Directions.Direction.Left)
                    {
                        _gyroPitch.Add("Pitch");
                        _gyroPitchReverse.Add(1);
                    }
                    else if (gyroLeft == Base6Directions.Direction.Right)
                    {
                        _gyroPitch.Add("Pitch");
                        _gyroPitchReverse.Add(-1);
                    }
                    else if (gyroLeft == Base6Directions.Direction.Forward)
                    {
                        _gyroPitch.Add("Roll");
                        _gyroPitchReverse.Add(-1);
                    }
                    else if (gyroLeft == Base6Directions.Direction.Backward)
                    {
                        _gyroPitch.Add("Roll");
                        _gyroPitchReverse.Add(1);
                    }
                }
            }
        }


        //Working. this sorts through the list of nearbyentities and calculates a single avoidance vector
        public bool AvoidNearbyEntities()
        {
            //avoiding and avoiding stage two are what I use as "AirBreaks" to bring acceleration down
            //most of the time avoiding requires a direction change so this makes it easier to slow down and change direction
            //rather than just going full power in an new direction sice dampaners slow down the ship much faster than max opposite power it prevents
            //drones from drifting into one another while trying to avoid
            if (Avoiding)
                Avoiding = false;

            try{

                if (_shipControls != null && Ship != null)
                {
                    if (Ship.Physics.LinearVelocity.Normalize() > MaxSpeed )
                    {
                        _shipControls.MoveAndRotateStopped();
                    }
                    else if(Ship!=null)
                    {

                        List<Vector3D> avoidanceVectors = new List<Vector3D>();
                        Vector2 avoidanceRot = Vector2.Zero;

                        //from the list of my nearby entities select
                        //only targets that have a mass > 20% my mass (otherwise theres no real need to dodge)
                        //order them by distance
                        //select only the top _avoidNumTargets
                        var topEntities =
                            _nearbyFloatingObjects
                                .Where(y => y != null && (y.Physics.Mass > Ship.Physics.Mass*.2)
                                            && (y != Ship))
                                //to rule out the chance of the ship avoiding its self... which is annoying
                                .OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length())
                                .Take(_avoidNumTargets).ToList();

                        //Dodge the _avoidNumTargets Entities found by building a single avoidance direction
                        foreach (var item in topEntities)
                        {
                            if (item != null)
                            {
                                var distance = Math.Abs((item.GetPosition() - Ship.GetPosition()).Length());
                                var enemyBoundingBoxSize = (item.GetPosition() - item.LocalAABB.Max).Length()/2;

                                if (distance < FollowRange/8)
                                {
                                    avoidanceRot += NavInfo.CalculateRotation(item.GetPosition(),
                                        _shipControls as IMyEntity);


                                    var temp = (Ship.GetPosition() - item.GetPosition());
                                    var val2 = item.Physics.Mass / Ship.Physics.Mass > 75 ? 75 : item.Physics.Mass / Ship.Physics.Mass;
                                    if (val2 < 30) val2 = 30;


                                    double[] vals = { temp.X, temp.Y, temp.Z };
                                    double min = vals.Min();
                                    if ((int)min == 0)
                                        min = 1;
                                    temp = temp/min*val2;
                                    Util.GetInstance().Log("[DroneNavigation.AvoidNearbyEntities] " + Ship.DisplayName + " avoiding Ship -> Distance: " + distance + " Mass: " + item.Physics.Mass);
                                    Util.GetInstance().Log("^^avoiding power x:" + temp.X + " y:" + temp.Y + " z:" + temp.Z);

                                    avoidanceVectors.Add(temp);
                                    // * (item.Physics.Mass / biggestMass));
                                }
                            }
                        }

                        foreach (var item in _nearbyAsteroids)
                        {
                            if (item != null)
                            {
                                var distance = Math.Abs((item.GetPosition() - Ship.GetPosition()).Length());
                                var enemyBoundingBoxSize = (item.GetPosition() - item.LocalAABB.Max).Length();

                                MaxSpeed = MaxSpeed > distance/ApproachSpeedMod ? distance/ApproachSpeedMod : MaxSpeed;
                                var detectRange = _avoidanceRange + enemyBoundingBoxSize;
                                if (distance < detectRange)
                                {
                                    avoidanceRot += NavInfo.CalculateRotation(item.GetPosition(), _shipControls as IMyEntity);
                                    var temp = (Ship.GetPosition() - item.GetPosition());
                                    var rx = (detectRange-Math.Abs(temp.X)) * (temp.X / temp.X);
                                    var y = (detectRange - Math.Abs(temp.Y)) * (temp.Y / temp.Y);
                                    var z = (detectRange - Math.Abs(temp.Z)) * (temp.Z / temp.Z);

                                    double[] vals = new[] { rx, y, z };
                                    double max = vals.Max(x=>Math.Abs(x));
                                    if (max == 0)
                                        max = vals.Average()!=0?vals.Average():1;

                                    var SpeedPowerBoostBasedOnRange = distance/100;
                                    temp = new Vector3D(rx, y, z) / max * SpeedPowerBoostBasedOnRange*20;
                                    Util.GetInstance().Log("[DroneNavigation.AvoidNearbyEntities] " + Ship.DisplayName + " avoiding asteroid -> Distance: " + (distance - detectRange));
                                    Util.GetInstance().Log("^^avoiding power x:"+ temp.X + " y:" + temp.Y + " z:" + temp.Z);
                                    avoidanceVectors.Add(temp);
                                }
                            }
                        }

                        if (avoidanceVectors.Count > 0)
                        {
                            avoidanceVectors = avoidanceVectors.OrderBy(x => x.Length()).ToList();
                            var avoidanceVector = avoidanceVectors[0];

                            for (int i = 1; i < avoidanceVectors.Count && i < 5; i++)
                            {
                                var temp = avoidanceVectors[i];
                                avoidanceVector += temp;
                            }
                            avoidanceVector = (avoidanceVector/avoidanceVectors.Count);
                            double[] vals = { avoidanceVector.X, avoidanceVector.Y, avoidanceVector.Z };
                            double max = vals.Max(x => Math.Abs(x));
                            if (max == 0)
                                max = vals.Average() != 0 ? vals.Average() : 1;
                            avoidanceVector = avoidanceVector / max * 104;
                            if (_nearbyAsteroids.Count>0)
                                Util.GetInstance().Log("[DroneNavigation.AvoidNearbyEntities] final avoiding power -> x:" + avoidanceVector.X + " y:" + avoidanceVector.Y + " z:" + avoidanceVector.Z);
                            if (MaxSpeed < 10)
                                MaxSpeed = 10;
                            AvoidTarget(avoidanceVector);
                            Avoiding = true;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                // i dont care about this error
                //Util.GetInstance().LogError(e.ToString());
            }
            return Avoiding;
        }
    }
}
