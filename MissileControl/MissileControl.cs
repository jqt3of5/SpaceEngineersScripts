using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.EntityComponents.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRageMath;

namespace IngameScript
{
    public class Program : MyGridProgram
    {
        private const string _missleStatusTag = "MissileStatus";
        private const string _armMissileTag = "ArmMissile";
        private const string _detonateMissileTag = "DetonateMissile";
        private const string _startCountdownTag = "StartCountdown";
        private const string _endCountdownTag = "EndCountdown";
        //Launch the missile for the first time after being built in the tube
        private const string _launchTag = "Launch";
        private const string _isAliveTag = "IsAlive";
        private const string _autoTarget = "AutoTarget";
        private const string _setTarget = "SetTarget";
        private const string _setMode = "SetMode";


        //Missiles need a uniue names to identify them
        private string _uniueName = string.Empty;

        enum MissileStatus
        {
            Unknown,
            Docked,
            Launching,
            Following,
            Seeking,
            Idle,
            
        }

        private int DetonationProximity { get; set; } = 0;
        private MissileStatus Status { get; set; } = MissileStatus.Unknown;
        
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
            
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            for (int i = 0; i < 2; i++)
            {
                _uniueName += chars[random.Next(chars.Length)];
            }
            
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            IGC.UnicastListener.SetMessageCallback();
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

            var connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors, w => w.CubeGrid == Me.CubeGrid);
            var mergeBlock = new List<IMyShipMergeBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(mergeBlock, w => w.CubeGrid == Me.CubeGrid);
            
            var thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters, w => w.CubeGrid == Me.CubeGrid);

            var warheads = new List<IMyWarhead>();
            GridTerminalSystem.GetBlocksOfType<IMyWarhead>(warheads, w => w.CubeGrid == Me.CubeGrid);
            
            var batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, w => w.CubeGrid == Me.CubeGrid);

            var tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks, w => w.CubeGrid == Me.CubeGrid);

            var moveBlocks = new List<IMyFlightMovementBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyFlightMovementBlock>(moveBlocks, w => w.CubeGrid == Me.CubeGrid);
            
            var offensiveBlocks = new List<IMyOffensiveCombatBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyOffensiveCombatBlock>(offensiveBlocks, w => w.CubeGrid == Me.CubeGrid);
            
            var taskBlocks = new List<IMyBasicMissionBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyBasicMissionBlock>(taskBlocks, w => w.CubeGrid == Me.CubeGrid);
            
            bool counting = warheads.Any(w => w.IsCountingDown);
            float timer = 0;
            if (warheads.Any())
            {
                timer = warheads.MinBy(w => w.DetonationTime).DetonationTime;
            }

            var connected = connectors.Any(c => c.Status == MyShipConnectorStatus.Connected);
            var readyToConnect = connectors.Any(c => c.Status == MyShipConnectorStatus.Connectable);
            var docked = connected || readyToConnect;
            
            bool armed = warheads.Any(w => w.IsArmed);

            //Respond to any incoming messages
            if ((updateSource & UpdateType.IGC) > 0)
            {
                while (IGC.UnicastListener.HasPendingMessage)
                {
                    var message = IGC.UnicastListener.AcceptMessage();
                    switch (message.Tag)
                    {
                        case _isAliveTag:
                            IGC.SendUnicastMessage(message.Source, _isAliveTag, "alive");
                            break;
                        case _armMissileTag:
                            if (!docked)
                            {
                                warheads.ForEach(w => w.IsArmed = true);
                                armed = true;
                            }
                            break;
                        case _detonateMissileTag:
                            if (!docked)
                            {
                                warheads.ForEach(w => w.Detonate());
                            }

                            break;
                        case _startCountdownTag:
                            if (!docked)
                            {
                                warheads.ForEach(w =>
                                {
                                    w.DetonationTime = 10;
                                    w.StartCountdown();
                                });
                                counting = true;
                                timer = 10;
                            }
                            break;

                        case _endCountdownTag:
                            if (!docked)
                            {
                                warheads.ForEach(w =>
                                {
                                    w.StopCountdown();
                                });
                                counting = false;
                            }
                            break;
                       
                        case _autoTarget:

                            Status = MissileStatus.Seeking; 
                            if (taskBlocks.Any())
                            {
                                taskBlocks.First().Enabled = false;
                            }

                            if (offensiveBlocks.Any())
                            {
                                offensiveBlocks.First().Enabled = true;
                            }

                            break;
                        case _setMode:
                            if (offensiveBlocks.Any())
                            {
                                offensiveBlocks.First().Enabled = false;
                            }
                            Status = MissileStatus.Following;
                            if (taskBlocks.Any())
                            {
                                taskBlocks.First().Enabled = true;
                            }
                            break;
                    }
                }
            }

            switch (Status)
            {
                //"Unknown" will be the initial state, so we can setup some initial behaviors when this is created
                case MissileStatus.Unknown:
                    if (connected)
                    {
                        Status = MissileStatus.Docked;
                        //If the ship is actually connected, we want to disable most things and recharge
                        tanks.ForEach(t => t.Stockpile = true);
                        thrusters.ForEach(t => t.Enabled  = false);
                    }
                    else if (readyToConnect)
                    {
                        Status = MissileStatus.Docked;
                        //If the ship is actually connected, we want to disable most things and recharge
                        tanks.ForEach(t => t.Stockpile = true);
                        thrusters.ForEach(t => t.Enabled  = false); 
                        connectors.ForEach(c => c.Connect());
                    }
                    else
                    {
                        Status = MissileStatus.Idle;
                    }
                    break;
                case MissileStatus.Idle:
                    if (connected || readyToConnect)
                    {
                        Status = MissileStatus.Docked;
                    }
                    break;
                 case MissileStatus.Docked:
                    if (!connected || !readyToConnect)
                    {
                        Status = MissileStatus.Idle;
                    }
                    break;
                  case MissileStatus.Following:
                    
                    
                   break;
                   case MissileStatus.Seeking:
                   
                       break;
            }

            float charge = 0;
            foreach (var battery in batteries)
            {
                charge += battery.CurrentStoredPower;
            }

            charge /= batteries.Count;

            double fillRatio = 0;
            foreach (var tank in tanks)
            {
                fillRatio += tank.FilledRatio;
            }

            fillRatio /= tanks.Count;

            var text = $"Missile {_uniueName}\n";
            if (armed)
            {
                text += "ARMED\n";
            }
            if (counting)
            {
                text += $"DETONATE IN: {timer}s\n";
            }
            text += $"Status: {Status}\n";
            text += $"H2 Tank: {fillRatio:P}\n";
            text += $"Battery: {charge:P}\n";
           
            var surface = Me.GetSurface(0);
            surface.WriteText(text);

            IGC.SendBroadcastMessage(_missleStatusTag, new MyTuple<string, string, float, float, string>(_uniueName, text, (float)fillRatio, charge, ""));
        }
    }
}