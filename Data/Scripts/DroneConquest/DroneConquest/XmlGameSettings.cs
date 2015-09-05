using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DroneConquest
{
    [XmlType("Settings")]
    public class XmlGameSettings
    {
        [XmlAttribute("Max_Number_of_Player_Drones")]
        public int MaxPlayerDroneCount { get; set; }
        [XmlAttribute("Max_Number_of_Conquest_Drone_Squads")]
        public int MaxNumConquestSquads { get; set; }
        [XmlAttribute("Max_Number_of_Conquest_Drones_Per_Squad")]
        public int MaxNumDronesPerConquestSquad { get; set; }
        [XmlAttribute("Max_Number_of_Squads_Guarding_Mothership")]
        public int MaxNumGuardingDroneSquads { get; set; }
        [XmlAttribute("Mothership_Range_Of_Influence")]
        public int ConquestInfluenceRange { get; set; }
    }
}
