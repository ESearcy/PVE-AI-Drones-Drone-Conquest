using System;
using Sandbox.Common;
using Sandbox.ModAPI;
using VRageMath;

namespace DroneConquest
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class DroneConquestDriver : MySessionComponentBase
    {
        private bool _isLoaded = false;
        private ConquestMod manager = new ConquestMod();
        private ChatMessageHandler cHandle = new ChatMessageHandler();
        private int saveRate = 50;

        private int _ticks;

        private void Init()
        {
            _isLoaded = true;
            
            Util.GetInstance().DebuggingOn = true;
            
            MyAPIGateway.Utilities.MessageEntered += ChatMessageHandler.HandleMessage;
            _ticks = 0;
        }
        
        public override void UpdateBeforeSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            if (MyAPIGateway.Session == null)
                return;

            try
            {
                GameCommands status = cHandle.GetStatus();
                cHandle.HandleMessageContents();
                if(_ticks%saveRate==0)
                    Util.SaveLogs();

                if (cHandle.GetStatus() == GameCommands.On)
                {
                    Run();
                }
                else if (cHandle.GetStatus() == GameCommands.Off)
                {
                    manager.StopAllDrones();
                }
                else if (cHandle.GetStatus() == GameCommands.Clearing)
                {
                    manager.ClearAllDrones();
                    cHandle.SetStatus(GameCommands.On);
                }

                _ticks++;
            }
            catch (Exception e)
            {
                Util.GetInstance().LogError(e.ToString());
            }
        }

        private void Run()
        {
            if (!_isLoaded)
                Init();

            manager.Update(_ticks);
        }

        public override void Simulate()
        {
        }

        public override void UpdateAfterSimulation()
        {
        }

    }
}
