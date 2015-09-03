using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;
using IMyControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMyGyro = Sandbox.ModAPI.IMyGyro;
using IMyReactor = Sandbox.ModAPI.Ingame.IMyReactor;
using IMyShipMergeBlock = Sandbox.ModAPI.Ingame.IMyShipMergeBlock;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using IMyThrust = Sandbox.ModAPI.IMyThrust;
using ITerminalAction = Sandbox.ModAPI.Interfaces.ITerminalAction;

namespace DroneConquest
{
    public class Drone
    {
        internal BroadcastingTypes broadcastingType;
        internal DroneNavigation navigation;
        #region Shipvariables
        public bool InSquad = false;
        public IMyCubeGrid Ship;
        internal IMyControllableEntity ShipControls;
        internal long _ownerId;

        internal double HealthBlockBase = 0;
        internal string _healthPercent = 100 + "%";
        internal ITerminalAction _fireGun;
        internal ITerminalAction _fireRocket;
        internal ITerminalAction _blockOn;
        internal ITerminalAction _blockOff;

        internal string _beaconName = "CombatDrone";

        internal List<IMyTerminalBlock> beacons = new List<IMyTerminalBlock>();
        internal List<IMyTerminalBlock> antennas = new List<IMyTerminalBlock>();
        internal List<IMyEntity> _nearbyFloatingObjects = new List<IMyEntity>();

        //Weapon Controls
        internal bool _isFiringManually;
        internal List<IMySlimBlock> _allWeapons = new List<IMySlimBlock>();
        internal List<IMySlimBlock> _allReactors = new List<IMySlimBlock>();
        internal List<IMySlimBlock> _manualGuns = new List<IMySlimBlock>();
        internal List<IMySlimBlock> _manualRockets = new List<IMySlimBlock>();

        internal IMyPlayer _targetPlayer = null;
        internal IMyCubeGrid _target = null;
        internal double _maxAttackRange = 800;
        internal DateTime _createdAt = DateTime.Now;
        internal int _minTargetSize = 10;
        public IMyGridTerminalSystem GridTerminalSystem;
        #endregion

        private static int numDrones = 0;
        internal int myNumber;
        public Type Type = typeof(Drone);

        public long GetOwnerId()
        {
            return _ownerId;
        }

        public void AddNearbyFloatingItem(IMyEntity entity)
        {
            try
            {
                _nearbyFloatingObjects.Add(entity);
            }
            catch (Exception e)
            {
                Util.GetInstance().Notify(e.ToString());
                //object is already in nearby objects collection
                //This error can be ignored
            }
        }

        public void AddNearbyAsteroid(IMyVoxelBase asteroid)
        {
            navigation.AddNearbyAsteroid(asteroid);
        }

        
        Random _r = new Random();
        public Drone(IMyEntity ent, BroadcastingTypes broadcasting)
        {
            
            var ship = (IMyCubeGrid)ent;
            double maxEngagementRange = ConquestMod.MaxEngagementRange;
            broadcastingType = broadcasting;
            
            Ship = ship;
            var lstSlimBlock = new List<IMySlimBlock>();
            
            GridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ship);

            //If it has any type of cockipt
            ship.GetBlocks(lstSlimBlock, (x) => x.FatBlock is IMyShipController);
            FindWeapons();

            if (_manualRockets.Count > 0 && _manualRockets[0] != null)
            {
                var actions = new List<ITerminalAction>();
                var block = (IMySmallMissileLauncher)_manualRockets[0].FatBlock;
                block.GetActions(actions);

                _fireRocket = block.GetActionWithName("Shoot_once");
                _fireGun = block.GetActionWithName("Shoot");
                _blockOff = block.GetActionWithName("OnOff_Off");
                _blockOn = block.GetActionWithName("OnOff_On");
            }
            Util.GetInstance().Log("[Drone.IsAlive] Has Missile attack -> " + (_fireRocket != null) + " Has Gun Attack " + (_fireRocket != null), "drone.txt");

            //If no cockpit the ship is either no ship or is broken.
            if (lstSlimBlock.Count != 0)
            {
                //Make the controls be the cockpit
                ShipControls = lstSlimBlock[0].FatBlock as IMyControllableEntity;

                _ownerId = ((Sandbox.ModAPI.IMyTerminalBlock)ShipControls).OwnerId;

                
                #region Activate Beacons && Antennas

                
                //Maximise radius on antennas and beacons.
                lstSlimBlock.Clear();
                ship.GetBlocks(lstSlimBlock, (x) => x.FatBlock is IMyRadioAntenna);
                foreach (var block in lstSlimBlock)
                {
                    IMyRadioAntenna antenna =
                        (IMyRadioAntenna)block.FatBlock;
                    if (antenna != null)
                    {
                        //antenna.GetActionWithName("SetCustomName").Apply(antenna, new ListReader<TerminalActionParameter>(new List<TerminalActionParameter>() { TerminalActionParameter.Get("Combat Drone " + _manualGats.Count) }));
                        antenna.SetValueFloat("Radius", antenna.GetMaximum<float>("Radius"));
                        ITerminalAction act = antenna.GetActionWithName("OnOff_On");
                        act.Apply(antenna);
                    }
                }

                lstSlimBlock = new List<IMySlimBlock>();
                ship.GetBlocks(lstSlimBlock, (x) => x.FatBlock is IMyBeacon);
                foreach (var block in lstSlimBlock)
                {
                    IMyBeacon beacon = (IMyBeacon)block.FatBlock;
                    if (beacon != null)
                    {
                        beacon.SetValueFloat("Radius", beacon.GetMaximum<float>("Radius"));
                        ITerminalAction act = beacon.GetActionWithName("OnOff_On");
                        act.Apply(beacon);
                    }
                }

                #endregion

                //SetWeaponPower(true);
                //AmmoManager.ReloadReactors(_allReactors);
                //AmmoManager.ReloadGuns(_manualGats);
                ship.GetBlocks(lstSlimBlock, x => x is IMyEntity);

                List<IMyTerminalBlock> massBlocks =
                    new List<IMyTerminalBlock>();

                GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(massBlocks);
               
                List<IMyTerminalBlock> allTerminalBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyCubeBlock>(allTerminalBlocks);
                HealthBlockBase = allTerminalBlocks.Count;

                if (ShipControls != null)
                {
                    
                    navigation = new DroneNavigation(ship, ShipControls, _nearbyFloatingObjects, maxEngagementRange);
                }

            }
            Ship.OnBlockAdded += RecalcMaxHp;
            myNumber = numDrones;
            numDrones++;
        }

        //this percent is based on IMyTerminalBlocks so it does not take into account the status of armor blocks
        //any blocks not functional decrease the overall %
        //having less blocks than when the drone was built will also result in less hp (parts destoried)
        private void CalculateDamagePercent()
        {
            try
            {
                List<IMyTerminalBlock> allTerminalBlocks =
                    new List<IMyTerminalBlock>();


                GridTerminalSystem.GetBlocksOfType<IMyCubeBlock>(allTerminalBlocks);

                
                double runningPercent = 0;
                foreach (var block in allTerminalBlocks)
                {
                    runningPercent += block.IsWorking || block.IsFunctional ? 100d : 0d;
                }
                runningPercent = runningPercent/allTerminalBlocks.Count;

                _healthPercent = ((int)((allTerminalBlocks.Count / HealthBlockBase) * (runningPercent))+"%");//*(runningPercent);
            }
            catch (Exception e)
            {
                //this is to catch the exception where the block blows up mid read bexcause its under attack or whatever
            }
        }

        private void RecalcMaxHp(IMySlimBlock obj)
        {
            List<IMyTerminalBlock> allTerminalBlocks =
                    new List<IMyTerminalBlock>();


            GridTerminalSystem.GetBlocksOfType<IMyCubeBlock>(allTerminalBlocks);

            
            double count = 0;
            foreach (var block in allTerminalBlocks)
            {
                count += block.IsWorking || block.IsFunctional ? 100d : 0d;
            }

            HealthBlockBase = allTerminalBlocks.Count;//*(runningPercent);
        }

        //add objects to this ships local known objects collection (within detection range - 2km by defualt)
       

        //Turn weapons on and off SetWeaponPower(true) turns weapons online: vice versa
        public void SetWeaponPower(bool isOn)
        {
            foreach (var w in _allWeapons)
            {
                if (isOn)
                    _blockOn.Apply(w.FatBlock);
                else
                    _blockOff.Apply(w.FatBlock);
            }
            foreach (var w in _manualGuns)
            {
                if (isOn)
                    _blockOn.Apply(w.FatBlock);
                else
                    _blockOff.Apply(w.FatBlock);
            }
            foreach (var w in _manualRockets)
            {
                if (isOn)
                    _blockOn.Apply(w.FatBlock);
                else
                    _blockOff.Apply(w.FatBlock);
            }
        }

        private int _weaponCount;

        //locates gatling turrets and rocket luanchers
        private void FindWeapons()
        {
            if (Ship == null)
                return;


            _allWeapons.Clear();
            _allReactors.Clear();
            _manualGuns.Clear();
            _manualRockets.Clear();


            Ship.GetBlocks(_manualRockets, (x) => x.FatBlock != null && (x.FatBlock is IMySmallMissileLauncher));
            Ship.GetBlocks(_manualGuns, (x) => x.FatBlock != null && (x.FatBlock is IMySmallGatlingGun));

            Ship.GetBlocks(_allReactors, (x) => x.FatBlock != null && x.FatBlock is IMyReactor);
            Ship.GetBlocks(_allWeapons, (x) => x.FatBlock != null && (x.FatBlock is IMyUserControllableGun));

            

            _weaponCount = _allWeapons.Count(x => (x.FatBlock).IsWorking || (x.FatBlock).IsFunctional);
        }

        //All three must be true
        //Ship is not trash
        //Ship Controlls are functional
        //Weapons Exist on ship
        //There have been a few added restrictions that must be true for a ship[ to be alive
        public bool IsAlive()
        {
            string errors = "";
            bool shipWorking = true;
            try
            {
                if (ShipControls != null &&
                    (!((Sandbox.ModAPI.Ingame.IMyCubeBlock) ShipControls).IsWorking))// ||
                     //!((Sandbox.ModAPI.Ingame.IMyCubeBlock) ShipControls).IsWorking))
                {
                    errors += "Ship Controlls are down: ";
                    shipWorking = false;
                }
                if (ShipControls == null)
                {
                    errors += "Ship Controlls are down: ";
                    shipWorking = false;
                }
                
                
                List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocks(allBlocks);
                if (Ship != null && allBlocks.Count < 10)
                {
                    errors += "Ship Too Small: ";
                    shipWorking = false;
                }

                if(Ship!=null && Ship.Physics.Mass < 1000)
                {
                    errors += "Ship Too Small: ";
                    shipWorking = false;
                }

                if (Ship != null && Ship.IsTrash())
                {
                    errors += "The ship is trashed: ";
                    shipWorking = false;
                }
                if (Ship == null)
                {
                    errors += "The ship is trashed: ";
                    shipWorking = false;
                }
                
                if (Ship != null && !Ship.InScene)
                {
                    errors += "The ship is trashed: ";
                    shipWorking = false;
                }
                Util.GetInstance().Log("[Drone.IsAlive] Alive -> "+_allReactors.Count(x=>x.FatBlock.IsFunctional)+" : " + errors, "drone.txt");
                if (!shipWorking && navigation != null)
                {
                    navigation.TurnOffGyros(false);
                    ManualFire(false);
                    _beaconName = "Disabled Drone: " + errors;
                    NameBeacon();
                }
            }

            catch
            {
                shipWorking = false;
            }
            return shipWorking;
        }

        

        //Disables all beacons and antennas and deletes the ship.
        public void DeleteShip()
        {
            var lstSlimBlock = new List<IMySlimBlock>();
            Ship.GetBlocks(lstSlimBlock, (x) => x.FatBlock is IMyRadioAntenna);
            foreach (var block in lstSlimBlock)
            {
                IMyRadioAntenna antenna = (IMyRadioAntenna)block.FatBlock;
                ITerminalAction act = antenna.GetActionWithName("OnOff_Off");
                act.Apply(antenna);
            }

            lstSlimBlock = new List<IMySlimBlock>();
            Ship.GetBlocks(lstSlimBlock, (x) => x.FatBlock is IMyBeacon);
            foreach (var block in lstSlimBlock)
            {
                IMyBeacon beacon = (IMyBeacon)block.FatBlock;
                ITerminalAction act = beacon.GetActionWithName("OnOff_Off");
                act.Apply(beacon);
            }

            MyAPIGateway.Entities.RemoveEntity(Ship as IMyEntity);
            Ship = null;
        }

        private void TurnOnShip()
        {
            List<IMyTerminalBlock> thrusters = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            foreach (var thruster in thrusters)
            {
                thruster.GetActionWithName("OnOff_On").Apply(thruster);
            }

            List<IMyTerminalBlock> gyro = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyro);

            foreach (var g in gyro)
            {
                g.GetActionWithName("OnOff_On").Apply(g);
            }
        }

        //stops the drone and turns off weapons
        public void UpdateOff(int count)
        {
            ShipControls.MoveAndRotateStopped();
            ManualFire(false);
        }

        //ship location
        public Vector3D GetPosition()
        {
            return Ship.GetPosition();
        }

        //Changes grid ownership of the drone
        public void SetOwner(long id)
        {
            _ownerId = id;
            Ship.ChangeGridOwnership(id, MyOwnershipShareModeEnum.Faction);
            Ship.UpdateOwnership(id, true);
        }

        //usses ammo manager to reload the inventories of the reactors and guns (does not use cargo blcks)
        public void ReloadWeaponsAndReactors(int i =1)
        {
            Util.GetInstance().Log("Number of weapons reloading");
            ItemManager.ReloadGuns(_manualGuns);
            ItemManager.ReloadReactors(_allReactors, i);
        }


        int missileStaggeredFireIndex = 0;
        DateTime lastRocketFired = DateTime.Now;
        //turn on all weapons
        public void ManualFire(bool doFire)
        {
            SetWeaponPower(doFire);
            if (doFire)
            {
                foreach (var gun in _manualGuns)
                {
                    if (((IMyUserControllableGun)gun.FatBlock).IsShooting != true)
                        _fireGun.Apply(gun.FatBlock);
                }
                if ((DateTime.Now - lastRocketFired).TotalMilliseconds > 500)
                {
                    var launcher = _manualRockets[missileStaggeredFireIndex];
                    _fireRocket.Apply(launcher.FatBlock);
                    if (missileStaggeredFireIndex + 1 < _manualRockets.Count())
                    {
                        missileStaggeredFireIndex++;
                    }
                    else
                        missileStaggeredFireIndex = 0;
                    lastRocketFired = DateTime.Now;
                }
            }

            

            _isFiringManually = doFire;

        }

        //sets targets to null
        private void ClearTarget()
        {
            _target = null;
            _targetPlayer = null;
        }

        //this method will look for active ship grids/drones/players and keep killing them untill it cant find any within
        //its local known nearby objects
        public bool FindNearbyAttackTarget()
        {
            //return false;
            FindWeapons();
            ClearTarget();

            Dictionary<IMyEntity, IMyEntity> nearbyDrones = new Dictionary<IMyEntity, IMyEntity>();
            Dictionary<IMyEntity, IMyEntity> nearbyOnlineShips = new Dictionary<IMyEntity, IMyEntity>();
            List<IMyPlayer> nearbyPlayers = new List<IMyPlayer>();



            MyAPIGateway.Players.GetPlayers(nearbyPlayers);
            nearbyPlayers =
                nearbyPlayers.Where(
                    x => x.PlayerID != _ownerId && (x.GetPosition() - Ship.GetPosition()).Length() < 2000)
                    .ToList();

            bool playersNearby = nearbyPlayers.Any();

            Util.GetInstance().Log("[Drone.FindNearbyAttacktarget] enemy players nearby? " + playersNearby);
            for (int i = 0; i < _nearbyFloatingObjects.Count; i++)
            {
                if ((_nearbyFloatingObjects.ToList()[i].GetPosition() - Ship.GetPosition()).Length() > 10)
                {
                    var entity = _nearbyFloatingObjects.ToList()[i];
                       
                    var grid = entity as IMyCubeGrid;
                    if (grid != null)
                    {
                        var gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                        List<IMyTerminalBlock> val = new List<IMyTerminalBlock>();
                        gridTerminal.GetBlocks(val);
                        var isFriendly = GridFriendly(val);

                        List<IMyTerminalBlock> T = new List<IMyTerminalBlock>();
                        gridTerminal.GetBlocksOfType<IMyRemoteControl>(T);

                        List<IMyTerminalBlock> reactorBlocks = new List<IMyTerminalBlock>();
                        gridTerminal.GetBlocksOfType<IMyPowerProducer>(reactorBlocks);


                        bool isOnline =
                            reactorBlocks.Exists(x => (((IMyPowerProducer)x).CurrentPowerOutput > 0 && x.IsWorking) && !isFriendly);

                        bool isDrone =
                            T.Exists(
                                x =>
                                    (((IMyRemoteControl)x).CustomName.Contains("Drone#") && x.IsWorking &&
                                        !isFriendly));

                        bool isMothership =
                            T.Exists(x => x.CustomName.Contains("#ConquestMothership"));

                            

                        var droneControl =
                            (IMyEntity)
                                T.FirstOrDefault(
                                    x => ((IMyRemoteControl)x).CustomName.Contains("Drone#") && x.IsWorking &&
                                            !isFriendly);

                        var shipPower =
                            (IMyEntity)
                                reactorBlocks.FirstOrDefault(
                                    x => (((IMyPowerProducer)x).CurrentPowerOutput > 0 && x.IsWorking) && !isFriendly);

                        if (isDrone && isOnline)
                        {
                            nearbyDrones.Add(grid, droneControl);
                        }
                        else if (isOnline)
                        {
                            nearbyOnlineShips.Add(grid, shipPower ?? droneControl);
                        }
                    }
                }
            }

            if (nearbyDrones.Count > 0)
            {
                Util.GetInstance().Log("[Drone.FindNearbyAttacktarget] nearby drone count " + nearbyDrones.Count);
                var myTarget =
                    nearbyDrones
                        .OrderBy(x => (x.Key.GetPosition() - Ship.GetPosition()).Length())
                        .ToList();

                if (myTarget.Count > 0)
                {
                    var target = myTarget[0];


                    IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid((IMyCubeGrid)target.Key);
                    List<IMyTerminalBlock> T = new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocks(T);

                    if (T.Count >= _minTargetSize)
                    {
                        _target = (IMyCubeGrid)target.Key;
                        _targetPlayer = null;
                        try
                        {
                            if (!FindTargetKeyPoint(_target, _target.Physics.LinearVelocity))
                                OrbitAttackTarget(_target.GetPosition(), _target.Physics.LinearVelocity);
                            return true;
                        }
                        catch
                        { 
                        }
                    }
                }
            }

            if (nearbyOnlineShips.Count > 0)
            {
                Util.GetInstance().Log("[Drone.FindNearbyAttacktarget] nearby ship count " + nearbyOnlineShips.Count);
                var myTargets =
                    nearbyOnlineShips
                        .OrderBy(x => (x.Key.GetPosition() - Ship.GetPosition()).Length())
                        .ToList();

                foreach (var target in myTargets)
                {

                    IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid((IMyCubeGrid)target.Key);
                    List<IMyTerminalBlock> T = new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocks(T);
                    
                    if (T.Count >= _minTargetSize)
                    {
                        _target = (IMyCubeGrid)target.Key;
                        _targetPlayer = null;
                        try
                        {
                            FindTargetKeyPoint(_target, _target.Physics.LinearVelocity);
                                    
                            return true;
                        }
                        catch { OrbitAttackTarget(_target.GetPosition(), _target.Physics.LinearVelocity); }
                    }
                }
            }

            //if (playersNearby && nearbyPlayers.Count > 0)
            //{
            //    Util.GetInstance().Log("[Drone.FindNearbyAttacktarget] nearby player count " + nearbyPlayers.Count);
            //    var myTarget = nearbyPlayers.OrderBy(x => ((x).GetPosition() - Ship.GetPosition()).Length()).ToList();

            //    if (myTarget.Count > 0)
            //    {
            //        _target = null;
            //        _targetPlayer = myTarget[0];
            //        OrbitAttackTarget(_targetPlayer.GetPosition(), new Vector3D(0, 0, 0));
            //        return true;
            //    }
            //}
            return false;
        }

        private bool GridFriendly(List<IMyTerminalBlock> gridblocks)
        {
            var myfaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(((IMyTerminalBlock)ShipControls).OwnerId);
            bool isFriendly = gridblocks.Count(x => (x).OwnerId == _ownerId) > gridblocks.Count*.1;
          

            foreach (var block in gridblocks)
            {
                var temp = MyAPIGateway.Session.Factions.TryGetPlayerFaction((block).OwnerId);
                if (temp != null && myfaction != null && temp == myfaction)
                {
                        return true;
                }
            }

            return isFriendly; 

            //this shit doesnt work even though it should, maybe i just did it wrong.
            //count>=gridblocks.Count/3;
            //switch (((IMyCubeBlock)block).GetUserRelationToOwner(((IMyTerminalBlock)ShipControls).OwnerId))
            //{

            //    case MyRelationsBetweenPlayerAndBlock.FactionShare: //isFriendly = true;
            //        break;
            //    case MyRelationsBetweenPlayerAndBlock.Neutral: //isFriendly = false;
            //        break;
            //    case MyRelationsBetweenPlayerAndBlock.NoOwnership: //isFriendly = false;
            //        break;
            //    case MyRelationsBetweenPlayerAndBlock.Owner: isFriendly = true;
            //        break;
            //    case MyRelationsBetweenPlayerAndBlock.Enemies: //isFriendly = false;
            //        break;
            //}
        }

        //target key points on an enemy ship to disable them
        private bool FindTargetKeyPoint(IMyCubeGrid grid, Vector3D velocity)
        {
            //get position, get lenier velocity in each direction
            //add them like 10 times and add that to current coord
                if (grid != null)
                {
                    IMyGridTerminalSystem gridTerminal =
                        MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                    List<IMyTerminalBlock> reactorsTarget =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<IMyPowerProducer>(reactorsTarget);

                    List<IMyTerminalBlock> cockpitsTarget =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<IMyCockpit>(cockpitsTarget);

                    List<IMyTerminalBlock> allBlocksTarget =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(allBlocksTarget);

                    List<IMyTerminalBlock> missileLuanchersTarget =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<IMyMissileGunObject>(missileLuanchersTarget);

                    List<IMyTerminalBlock> batteriesTarget =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<IMyBatteryBlock>(batteriesTarget);

                    List<IMyTerminalBlock> mergeBlocks =
                        new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);

                    MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(_target);
                    List<IMySlimBlock> weaponsTarget = new List<IMySlimBlock>();

                    grid.GetBlocks(weaponsTarget,
                        (x) => x.FatBlock != null && x.FatBlock is IMyUserControllableGun);
                    //now that we have a list of reactors and guns lets primary one.
                    //try to find a working gun, if none are found then find a reactor to attack

                    //guns, rockets, cockpits,reactors,batteries
                    foreach (var merge in mergeBlocks.OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (merge != null)
                        {
                            var item = ((IMyCubeBlock)merge);
                            if (item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                    foreach (var weapon in weaponsTarget.OrderBy(x=>(x.FatBlock.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (weapon != null)
                        {
                            var item = (weapon.FatBlock);

                            if (item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                    foreach (var missile in missileLuanchersTarget.OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (missile != null)
                        {
                            var item = ((IMyCubeBlock) missile);
                            if (item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                    foreach (var reactor in reactorsTarget.OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (reactor != null)
                        {
                            var item = ((IMyCubeBlock)reactor);
                            if (item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                    foreach (var battery in batteriesTarget.OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (battery != null)
                        {
                            var item = ((IMyCubeBlock)battery);
                            if ( item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                    foreach (var cok in cockpitsTarget.OrderBy(x => (x.GetPosition() - Ship.GetPosition()).Length()))
                    {
                        if (cok != null)
                        {
                            var item = ((IMyCubeBlock)cok);
                            if (item.IsFunctional)
                            {
                                OrbitAttackTarget(item.GetPosition(), velocity);
                                return true;
                            }
                        }
                    }
                }
            return false;
        }

        public void OrbitAttackTarget(Vector3D p, Vector3D v)
        {
            var distance = (p - Ship.GetPosition()).Length();
            var m = Ship.Physics.LinearVelocity;

            //what is this you may ask? the enemy position plus their velocity, minus the drones velocity for lead shooting
            Vector3D position;
            NavInfo nav;
            
            {
                position = new Vector3D(p.X + (v.X) - m.X, p.Y + (v.Y) - m.Y, p.Z + (v.Z) - m.Z);
                //position = new Vector3D(p.X + (v.X), p.Y + (v.Y), p.Z + (v.Z));

                nav = new NavInfo(Ship.GetPosition(), position, (IMyEntity)ShipControls);
                navigation.MaxSpeed = (v.Normalize() + 10) * 1.5;
            }
            
            if (!navigation.Avoiding)
            {
                if (distance > navigation.FollowRange/2)
                    CombatOrbit(position);
            }

            var alignAngle = navigation.AlignTo(position);

            if (nav.Direction.Length() < _maxAttackRange && alignAngle < 3)
            {

                ManualFire(true);
            }
            else
            {
                ManualFire(false);
            }

            NameBeacon();
        }
 
        //Working - and damn good I might add
        //returns status means -1 = not activated, 0 = notEngaged, 1 = InCombat
        public int Guard(Vector3D position)
        {
            Util.GetInstance().Log("[Drone.Guard] Guarding:");
            ManualFire(false);

            if (Math.Abs((position - Ship.GetPosition()).Length()) < ConquestMod.MaxEngagementRange)
            {
                if (navigation.AvoidNearbyEntities())
                {
                    Util.GetInstance().Log("[Drone.Guard] " + myNumber + " Drone Avoiding");
                }
                
                var distance = (position - Ship.GetPosition()).Length();


                if (FindNearbyAttackTarget())
                {
                    Util.GetInstance().Log("[Drone.Guard] " + myNumber + " drone found a target");
                    return 1;
                }
                    

                if (distance > navigation.FollowRange*1.2)
                {
                    if (!navigation.Avoiding && navigation.Follow(position))
                    {
                        _beaconName = "Following";
                    }
                }
                else
                {
                    if (!navigation.Avoiding && navigation.Orbit(position))
                    {
                        _beaconName = "Orbiting";
                         
                    }
                }
                

            }
            else if (Ship!=null && navigation != null)
            {
                _beaconName = "Returning";
                navigation.Follow(position);
                _target = null;
            }
            NameBeacon();
            
            return 0;   
        }

        // return status codes = -1: not ready, 0: traveling, 1:EngagedEnemy 2: reachedDestination
        public int Patrol(Vector3D position)
        {
            navigation.AvoidNearbyEntities();

            var distance = (position - Ship.GetPosition()).Length();

            if (FindNearbyAttackTarget())
                return 1;

            ManualFire(false);
            if (distance > navigation.FollowRange * 1.2)
            {

                if (!navigation.Avoiding && navigation.Follow(position))
                {
                    _beaconName = "Following";
                }
            }
            else
            {

                if (!navigation.Avoiding && navigation.Orbit(position))
                {
                    _beaconName = "Orbiting";

                }
            }

            NameBeacon();
            return 0;  
        }

        //this sets the status of the ship in its beacon name or antenna name - this is user settable within in drone name
        //if drone name includes :antenna then the drone will display information on the antenna rather than the beacon
        public void NameBeacon()
        {
            try
            {
                if (broadcastingType == BroadcastingTypes.Beacon)
                {
                    FindBeacons();
                    if (beacons != null && beacons.Count > 0)
                    {
                        CalculateDamagePercent();
                        var beacon = beacons[0] as IMyBeacon;
                        beacon.SetCustomName(_beaconName +
                                             " HP: " + _healthPercent +
                                             " MS: " + (int)Ship.Physics.LinearVelocity.Normalize() + "/" + (int)navigation.MaxSpeed);
                    }
                }
                else
                    NameAntenna();
            }
            catch
            {
            }
        }

        public void NameAntenna()
        {
            FindAntennas();
            if (antennas != null && antennas.Count > 0)
            {
                CalculateDamagePercent();
                var antenna = antennas[0] as IMyRadioAntenna;
                antenna.SetCustomName(_beaconName +
                                             " HP: " + _healthPercent +
                                             " MS: " + (int)Ship.Physics.LinearVelocity.Normalize() + "/" + (int)navigation.MaxSpeed);
            }
        }

        public void FindBeacons()
        {
            GridTerminalSystem.GetBlocksOfType<IMyBeacon>(beacons);
        }

        public void FindAntennas()
        {
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
        }

       
        // for thoes pesky drones that just dont care about the safty of others
        public void Detonate()
        {
            ShipControls.MoveAndRotateStopped();
            List<IMyTerminalBlock> warHeads = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warHeads);

            foreach (var warhead in warHeads)
                warhead.GetActionWithName("StartCountdown").Apply(warhead);
        }

        public void ClearNearbyObjects()
        {
            _nearbyFloatingObjects.Clear();
        }

        public void Orbit(Vector3D lastTargetPosition)
        {
            _beaconName = "OR";
            navigation.AvoidNearbyEntities();
            navigation.Orbit(lastTargetPosition);
        }

        public void Orbit(List<Vector3D> positions)
        {
            _beaconName = "";
            navigation.MaxSpeed = 40;
            navigation.AvoidNearbyEntities();
            navigation.Orbit(positions);
        }

        public void CombatOrbit(Vector3D lastTargetPosition)
        {
            _beaconName = "OR/AT";
            navigation.AvoidNearbyEntities();
            navigation.CombatOrbit(lastTargetPosition);

        }

        public void Stop()
        {
            navigation.CompleteStop();
        }
    }
}
