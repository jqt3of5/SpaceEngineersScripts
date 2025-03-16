using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game.GUI.TextPanel;

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
        private const string _launchTag = "Launch";
        private const string _autoTarget = "AutoTarget";
        private const string _setTarget = "SetTarget";
        private const string _setMode = "SetMode";

        private string[] lcds = new string[] { "LCD Panel" };
        
        private Dictionary<long, MissileStatus> _missileStatus =
            new Dictionary<long, MissileStatus>(); 
                
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
            public bool Lost { get; set; }
            public int AliveCount { get; set; }
        }

       
        public void Main(string argument, UpdateType updateSource)
        {
            
            //Main is only called once with arguments when the script is first ran. Then ever after it's empty
            //Cache the values we gather from the arguments.
            if (!string.IsNullOrEmpty(argument))
            {
                if (argument.StartsWith("launch"))
                {
                    var split = argument.Split(' ');
                    foreach (var missile in _missileStatus)
                    {
                        if (missile.Value.Name == split[1])
                        {
                            IGC.SendUnicastMessage(missile.Key, _launchTag, split[1]);
                        }
                    }
                    
                }
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
                                _missileStatus[message.Source].Name = data.Item1;
                                _missileStatus[message.Source].Status = data.Item2;
                                _missileStatus[message.Source].TankFill = data.Item3;
                                _missileStatus[message.Source].BatteryCharge = data.Item4;
                                _missileStatus[message.Source].Target = data.Item5;
                                _missileStatus[message.Source].Lost = false;
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
                        kvp.Value.Lost = true;
                    }                                        
                    else
                    {
                        IGC.SendUnicastMessage(kvp.Key, _isAliveTag, "alive?");
                        kvp.Value.AliveCount++;
                    }
                }
            }

            for (int i = 0; i < _missileStatus.Count; i++)
            {
                var kvp = _missileStatus.ElementAt(i);
                var text = string.Empty;
                if (kvp.Value.Lost)
                {
                    text += $"{kvp.Value.Name}\n";
                    text += $"LOST";
                }
                else
                {
                    text += kvp.Value.Status;
                }
                text += "\n";

                if (lcds.Length > i)
                {
                    var lcd = GridTerminalSystem.GetBlockWithName(lcds[i]) as IMyTextSurface;
                    if (lcd != null)
                    {
                        lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                        lcd.WriteText(text);
                    }
                }
            }
            
            // var surface = Me.GetSurface(0);
            // surface.WriteText(text);
        }
    }
}