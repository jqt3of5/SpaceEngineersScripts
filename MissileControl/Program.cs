using Sandbox.ModAPI.Ingame;
using VRage;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private static string _missleStatusTag = "MissileStatus";
        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Main is only called once with arguments when the script is first ran. Then ever after it's empty
            //Cache the values we gather from the arguments.
            if (!string.IsNullOrEmpty(argument))
            {
                //NOP. 
            }

            string status = "Unknown";
            var connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);

            if (connectors.Any(c => c.IsConnected || c.Status != MyShipConnectorStatus.Unconnected))
            {
                status = "Docked";
            }

            var tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks);

            double fillRatio = 0;
            foreach (var tank in tanks)
            {
                fillRatio += tank.FilledRatio;
            }

            fillRatio /= tanks.Count;
            
            IGC.SendBroadcastMessage(_missleStatusTag, new MyTuple<string, int, string>(status, (int)(fillRatio*100.0), "None"));
        }
    }
}