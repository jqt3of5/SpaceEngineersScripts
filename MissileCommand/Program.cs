using Sandbox.ModAPI.Ingame;
using VRage;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private readonly IMyBroadcastListener _broadcastListener;
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

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

            _broadcastListener = IGC.RegisterBroadcastListener(_missleStatusTag);
            _broadcastListener.SetMessageCallback(_missleStatusTag);
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

        private Dictionary<long, MyTuple<string, int, string>> _missileStatus =
            new Dictionary<long, MyTuple<string, int, string>>(); 
        public void Main(string argument, UpdateType updateSource)
        {
            //Main is only called once with arguments when the script is first ran. Then ever after it's empty
            //Cache the values we gather from the arguments.
            if (!string.IsNullOrEmpty(argument))
            {
                
            }

            if ((updateSource & UpdateType.IGC) > 0)
            {
                while (_broadcastListener.HasPendingMessage)
                {
                    var message = _broadcastListener.AcceptMessage();
                    if (message.Tag == _missleStatusTag)
                    {
                        //Missile Status, fuel tank status, current target 
                        if (message.Data is MyTuple<string, int, string>)
                        {
                            _missileStatus[message.Source] = (MyTuple<string, int, string>)message.Data;
                        }
                        else
                        {
                            Echo($"wrong data: {message.Data}");
                        }
                    }
                    else
                    {
                        Echo($"wrong tag: {message.Tag}");
                    }
                }
            }

            string text = "Missile Status: \n";
            foreach (var kvp in _missileStatus)
            {
                text += $"Missile ID: {kvp.Key}\n";
                text += $"Status: {kvp.Value.Item1}\n";
                text += $"Tank Fill: {kvp.Value.Item2}%\n";
                text += $"Target: {kvp.Value.Item3}%\n";
            }

            var surface = Me.GetSurface(0);
            surface.WriteText(text);
        }
    }
}