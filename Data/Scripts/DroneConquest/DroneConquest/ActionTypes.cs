namespace DroneConquest
{
    public enum ActionTypes
    {
        Guard,
        Orbit,
        Return,
        Sentry,
        Assist,
        Patrol
    }

    public enum GameCommands
    {
        On,
        Off,
        Clearing,
        Reporting
    }

    public enum DroneModes
    {
        AtRange,
        Fighter
    }
    public enum ConquestDrones
    {
        SmallOne,
        SmallTwo,
        SmallThree,
        MediumOne,
        MediumTwo,
        MediumThree,
        LargeOne,
        LargeTwo,
        LargeThree
    }

    public enum DroneTypes
    {
        PlayerDrone,
        ConquestDrone,
        MothershipDrone,
        NotADrone
    }

    public enum Standing
    {
        Hostile,
        Passive
    }

    public enum BroadcastingTypes
    {
        Beacon,
        Antenna
    }

    public class DroneConstructionType
    {
        public DroneTypes DroneType = DroneTypes.NotADrone;
        public BroadcastingTypes Broadcasting = BroadcastingTypes.Beacon;

        public DroneConstructionType(BroadcastingTypes broadcasting, DroneTypes droneType)
        {
            DroneType = droneType;
            Broadcasting = broadcasting;
        }
    }
}
