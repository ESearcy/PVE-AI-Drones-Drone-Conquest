using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    public class ConquestMission
    {
        private DateTime startTime = DateTime.Now;
        public ActionTypes MissionType;
        public bool Completed;
        public bool Ongoing;
        public Vector3D Location;
        public int StartTime;
        public bool IsAsteroid;
        public IMyVoxelBase asteroid;
        private int timeout = 600;

        public bool Expired(int ticks)
        {
            string logpath = "ConquestMission.txt";
            Util.GetInstance().Log("[ConquestDroneManager.GenerateMission] checking Mission expired " + (ticks - StartTime) +" "+timeout+ " "+ (Math.Abs(ticks - StartTime) > timeout), logpath);
            return (Math.Abs((DateTime.Now - startTime).TotalSeconds) > timeout);
        }

        Random r = new Random();

        //time out is 5 for performance reasons
        public ConquestMission(bool ong, Vector3D loc, int start,ActionTypes missiont, int timeout = 0, bool isasteroid = false, IMyVoxelBase ast = null)
        {
            
            MissionType = missiont;
            Ongoing = ong;
            Location = loc;
            StartTime = start;
            IsAsteroid = isasteroid;
            this.timeout = r.Next((int) (timeout*.7),(int) (timeout*1.3));
            asteroid = ast;
        }

    }
}
