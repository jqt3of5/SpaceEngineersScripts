using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
namespace IngameScript
{
    public class MissileControlTubeStatus : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public MissileControlTubeStatus()
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

        Dictionary<string, List<string>> _lcdToProjectors = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> _projectorsToWelders= new Dictionary<string, List<string>>();
        Dictionary<string, string> _projectorsToDoor = new Dictionary<string, string>();
        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
            
            //Main is only called once with arguments when the script is first ran. Then ever after it's empty
            //Cache the values we gather from the arguments.
            if (!string.IsNullOrEmpty(argument))
            {
                _lcdToProjectors.Clear();
                _projectorsToWelders.Clear();
                var projectors = argument.Split(';');

                foreach (var p in projectors)
                {
                    var arguments = p.Split(',');
                    if (arguments.Length < 5)
                    {
                        Echo($"ERROR: invalid arguments. Must be: <ProjectorName>,<LCDName>,<lcdindex>,<WelderName>:<WelderName>,<DoorName>;<ProjectorName>,<LCDName>,<lcdIndex>,<WelderName>:<WelderName><DoorName>]\n was:{arguments} {updateSource}");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;
                    }

                    int lcdIndex = 0;
                    if (!int.TryParse(arguments[2], out lcdIndex))
                    {
                        Echo($"ERROR: lcd index specified but wasn't an integer; value: {arguments[2]}");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return; 
                    }

                    string projectorName = arguments[0], lcdName = arguments[1], doorName = arguments[4];
                    
                    if (!_projectorsToWelders.ContainsKey(projectorName))
                    {
                        var ps = new List<string>();
                        _projectorsToWelders[projectorName] = ps;
                    }
                    
                    var welders = arguments[3].Split(':');
                    foreach (var welder in welders)
                    {
                        _projectorsToWelders[projectorName].Add(welder); 
                    }

                    _projectorsToDoor[projectorName] = doorName; 

                    if (lcdName.Contains(":"))
                    {
                        Echo($"ERROR: lcd name cannot contain ':'. Was: {lcdName}");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        return;  
                    }

                    if (!_lcdToProjectors.ContainsKey($"{lcdName}:{lcdIndex}"))
                    {
                        var ps = new List<string>();
                        _lcdToProjectors[$"{lcdName}:{lcdIndex}"] = ps;
                    }

                    _lcdToProjectors[$"{lcdName}:{lcdIndex}"].Add(projectorName);
                }
            }

            foreach (var kvp in _lcdToProjectors)
            {
                var a = kvp.Key.Split(':');
                var lcdName = a[0];
                var lcdIndex = int.Parse(a[1]);
                var text = string.Empty;
                foreach (var projectorName in kvp.Value)
                {
                    var projector = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
                    text += $"{projectorName}: ";
                    if (projector == null)
                    {
                        text += "Not Found\n";
                    }
                    else if (!projector.IsFunctional)
                    {
                        text += "Broken\n";
                    }
                    if (projector.IsProjecting)
                    {
                        text += "Projecting\n";
                        
                    }
                    else
                    {
                        text += "Offline\n";
                    }

                    if (projector.TotalBlocks > 0)
                    {
                        var progress = (1.0f - (float)projector.RemainingBlocks/projector.TotalBlocks) * 100;
                        text += $"    Progress:{progress}";
                    }

                    if (_projectorsToDoor.ContainsKey(projectorName))
                    {
                        var doorName = _projectorsToDoor[projectorName];
                        var door = GridTerminalSystem.GetBlockWithName(doorName);
                        text += $"    Door Status: ";
                        if (door == null)
                        {
                            text += "Not Found\n";
                        }
                        else if (!door.IsFunctional)
                        {
                            text += "Broken\n";
                        }
                        else if (door.Closed)
                        {
                            text += "Closed\n";
                        }
                        else if (!door.Closed)
                        {
                            text += "Open\n";
                        }
                    }

                    if (_projectorsToWelders.ContainsKey(projectorName))
                    {
                       foreach (var welderName in _projectorsToWelders[projectorName])
                       {
                           var welder = GridTerminalSystem.GetBlockWithName(welderName) as IMyShipWelder;
                           
                           text += $"\t{welderName}: ";
                           if (welder == null)
                           {
                               text += "Not Found\n";
                           }
                           else if (!welder.Enabled)
                           {
                               text += "Disabled\n";
                           }
                           else if (welder.IsBeingHacked)
                           {
                               text += "Being Hacked\n";
                           }
                           else if (!welder.IsFunctional)
                           {
                               text += "Broken\n";
                           }
                           else if (!welder.HasLocalPlayerAccess())
                           {
                               text += "No Access\n";
                           }
                           else if (!welder.IsWorking)
                           {
                               text += "Offline\n";
                           }
                           else
                           {
                               var inventory = welder.GetInventory();
                               //TODO: Calculate resource availability better
                               if (inventory.ItemCount == 0)
                               {
                                   text += "No Resources\n";
                               }
                               else
                               {
                                   text += "Ready\n";
                               }
                           }
                       } 
                    }

                    text += "\n";
                }
                
                var display = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextSurfaceProvider;
                if (display == null)
                {
                    Echo($"ERROR: display with name {lcdName} not found or isn't a display");
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }

                var surface = display.GetSurface(lcdIndex);
                if (surface == null)
                {
                    Echo($"ERROR: surface at index {lcdIndex} not found on display {lcdName}");
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                } 
                surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                // surface.FontSize = 1;
                surface.WriteText(text);
            }
           

            // var hackedEntities = new List<IMyCubeBlock>();
            // GridTerminalSystem.GetBlocksOfType(hackedEntities, e => e.IsBeingHacked || ((e as IMyTerminalBlock)?.HasLocalPlayerAccess() ?? false));

            // var damagedBlocks = new List<IMyCubeBlock>();


            //Ideas
            //1. List active missiles and their statuses
            //1.1 Hydrogen status, armed, mode
            //2. Missile friendly tracking and following
            //3. Resource levels
            //4. Average ship Damage
            //5. Turrets remaining
            //6. Missile fuel and launch
            //7. Is the ship being hacked?

        }
    }
}