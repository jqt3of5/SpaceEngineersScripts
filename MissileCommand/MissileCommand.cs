using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage;

namespace IngameScript
{
    public class MissileCommand : MyGridProgram
    {
        private const string _missleStatusTag = "MissileStatus";
        private const string _armMissileTag = "ArmMissile";
        private const string _detonateMissileTag = "DetonateMissile";
        private const string _startCountdownTag = "StartCountdown";
        private const string _endCountdownTag = "EndCountdown";
        private const string _isAliveTag= "IsAlive";
                
        private readonly IMyBroadcastListener _broadcastListener;
        public MissileCommand()
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
            IGC.UnicastListener.SetMessageCallback();
        }

        public void Save()
        {
    
        }

        class MissileStatus
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public float TankFill { get; set; }
            public float BatteryCharge { get; set; }
            public string Target { get; set; }
            public string Status { get; set; }
            public int AliveCount { get; set; }
        }

        private Dictionary<long, MissileStatus> _missileStatus =
            new Dictionary<long, MissileStatus>(); 
        public void Main(string argument, UpdateType updateSource)
        {
            //Main is only called once with arguments when the script is first ran. Then ever after it's empty
            //Cache the values we gather from the arguments.
            if (!string.IsNullOrEmpty(argument))
            {
                
            }

            if ((updateSource & UpdateType.IGC) > 0)
            {
                while (IGC.UnicastListener.HasPendingMessage)
                {
                    var message = IGC.UnicastListener.AcceptMessage();
                    switch (message.Tag)
                    {
                        case _isAliveTag:
                            //Reset alive count so we don't mark this missile as dead
                            _missileStatus[message.Source].AliveCount = 0;
                            break;
                    }
                }
                
                while (_broadcastListener.HasPendingMessage)
                {
                    var message = _broadcastListener.AcceptMessage();
                    switch (message.Tag)
                    {
                        case _missleStatusTag:
                            if (message.Data is MyTuple<string, string, float, float, string>)
                            {
                                var data = (MyTuple<string, string, float, float, string>)message.Data;
                                if (!_missileStatus.ContainsKey(message.Source))
                                {
                                    _missileStatus[message.Source] =
                                        new MissileStatus()
                                        {
                                            Id = message.Source,
                                        };
                                }
                                _missileStatus[message.Source].Status = data.Item2;
                                _missileStatus[message.Source].TankFill = data.Item3;
                                _missileStatus[message.Source].BatteryCharge = data.Item4;
                                _missileStatus[message.Source].Target = data.Item5;
                                _missileStatus[message.Source].Name = data.Item1;
                                _missileStatus[message.Source].AliveCount = 0;
                            }
                            else
                            {
                                Echo($"wrong data type: {message.Data}");
                            }
                            break;
                    }
                }
            }
            else
            {
                foreach (var kvp in _missileStatus)
                {
                    //I don't know what the minimum count should be, but 3 seems like a good number
                    if (kvp.Value.AliveCount > 3)
                    {
                        kvp.Value.Status = "Lost";
                    }                                        
                    else
                    {
                        IGC.SendUnicastMessage(kvp.Key, _isAliveTag, "alive?");
                        kvp.Value.AliveCount++;
                    }
                }
            }

            string text = "Missile Status: \n";
            foreach (var kvp in _missileStatus)
            {
                text += $"{kvp.Value.Name}\n";
                text += $"Status: {kvp.Value.Status}\n";
                //Lost missiles have no other stats
                if (kvp.Value.Status != "Lost")
                {
                    text += $"Tank Fill: {kvp.Value.TankFill}%\n";
                    text += $"Battery Charge: {kvp.Value.BatteryCharge}%\n";
                    text += $"Target: {kvp.Value.Target}%\n";
                }
                text += "\n";
            }
            
            var surface = Me.GetSurface(0);
            surface.WriteText(text);
        }
    }
}