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
        [XmlAttribute("Max_number_of_player_drones")]
        public int MaxPlayerDroneCount { get; set; }
        [XmlAttribute("Max_number_of_conquest_drone_squads")]
        public int MaxNumConquestSquads { get; set; }
        [XmlAttribute("Max_number_of_conquest_drones_per_squad")]
        public int MaxNumDronesPerConquestSquad { get; set; }
        [XmlAttribute("Max_number_of_squads_guarding_mothership")]
        public int MaxNumGuardingDroneSquads { get; set; }
    }
}
