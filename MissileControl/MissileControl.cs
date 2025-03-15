using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage;

namespace IngameScript
{
    public class MissileControl : MyGridProgram
    {
        private const string _missleStatusTag = "MissileStatus";
        private const string _armMissileTag = "ArmMissile";
        private const string _detonateMissileTag = "DetonateMissile";
        private const string _startCountdownTag = "StartCountdown";
        private const string _endCountdownTag = "EndCountdown";
        //Launch the missile for the first time after being built in the tube
        private const string _launchTag = "Launch";
        private const string _isAliveTag = "IsAlive";


        //Missiles need a uniue names to identify them
        private string _uniueName = string.Empty;
        
        public MissileControl()
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
            

                        
                        
            bool counting = warheads.Any(w => w.IsCountingDown);
            float timer = warheads.MinBy(w => w.DetonationTime).DetonationTime;

            var docked = connectors.Any(c => c.IsConnected || c.Status != MyShipConnectorStatus.Unconnected);

            string status = "Unknown";
            if (docked)
            {
                status = "Docked";
            }
            //If the detonation timer is counting down
            else if (counting)
            {
                status = "Counting";
            }
            //If it's following a target
            // else if (following)
            // {
                //Follow distance
                //Target
            // }
            //If it's seeking a target to collide
            // else if (seeking) 
            // {
                //Detonation proximity 
            // }

            bool armed = warheads.Any(w => w.IsArmed);
            if (armed)
            {
                status += "/Armed";
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

            string target = "None";

            string text = "Missile Status: \n";
            text += $"Missile {_uniueName}\n";
            text += $"Status: {status}\n";
            if (counting)
            {
                text += $"Countdown: {timer}\n";
            }
            text += $"Tank Fill: {fillRatio}%\n";
            text += $"Battery Charge: {charge}%\n";
            text += $"Target: {target}%\n";

            var surface = Me.GetSurface(0);
            surface.WriteText(text);
            
            IGC.SendBroadcastMessage(_missleStatusTag, new MyTuple<string, string, float, float, string>(_uniueName, status, (float)fillRatio, charge, target));

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
                            }
                            break;

                        case _endCountdownTag:
                            if (!docked)
                            {
                                warheads.ForEach(w =>
                                {
                                    w.StopCountdown();
                                });
                            }
                            break;
                        case _launchTag:
                            connectors.ForEach(c => c.Disconnect());
                            thrusters.ForEach(t => t.Enabled = true);

                            //Move forward a small distance, then target the closest ship to follow  
                            if (moveBlocks.Any())
                            {
                                var block = moveBlocks.First();
                                block.Enabled = true;
                                block.CollisionAvoidance = false;
                                block.FlightMode = 

                                block.SpeedLimit = 5;
                            }
                            
                            
                            break;
                    }
                    
                }
                
            }
        }
    }
}