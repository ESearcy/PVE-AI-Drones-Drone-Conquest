using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneConquest
{
    public class CombatSite
    {
        public bool Expired = false;

        public int NumSquadsAssisting { get; set; }
    

        public void AddSquad()
        {
            Expired = false;
            NumSquadsAssisting++;
        }

        public CombatSite MarkForDelete()
        {
            Expired = true;
            return this;
        }
    }
}
