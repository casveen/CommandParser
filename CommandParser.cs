using UnityEngine;
using System.Collections;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Core.Data.Models;
using Animancer;
using System.Collections.Generic;
using System.Linq;
using Warudo.Core.Server;

using Warudo.Plugins.Core.Assets.Utility;
using Warudo.Plugins.Core.Assets.Cinematography;

using System;
//using Cysharp.Threading.Tasks;
using Warudo.Core;
using Warudo.Core.Data;
using RootMotion.Dynamics;
using Warudo.Core.Utils;
using Warudo.Core.Localization;
using Warudo.Plugins.Core;
using Warudo.Plugins.Core.Utils;
using Warudo.Plugins.Interactions.Mixins;
using Object = UnityEngine.Object;


namespace Playground {
    //[NodeType(),
    [NodeType(
    Id = nameof(CommandParserNode), // Must be unique. Generate one at https://guidgenerator.com/
    Title = "CommandParser",
    Category ="Fnugus")]
    public class CommandParserNode : Node {
        protected Dictionary<string, string> ParseDict = new Dictionary<string, string>();

        protected bool hideSubscriber() { return !(Commander?.useTwitchSubscriberGroup??false); }
        protected bool hideModerator() { return !(Commander?.useTwitchModeratorGroup??false); }
        protected bool hideBroadcaster() { return !(Commander?.useTwitchBroadcaster??false); }
        protected bool hideVIP() { return !(Commander?.useTwitchVIPGroup??false); }

        [DataInput]
        public string username = "";
        [DataInput]
        public string message = "";
        [DataInput]
        [HiddenIf(nameof(hideSubscriber))]
        public bool isSubscriber = false;

        [DataInput]
        [HiddenIf(nameof(hideBroadcaster))]
        public bool isBroadcaster = false;
        [DataInput]
        [HiddenIf(nameof(hideModerator))]
        public bool isModerator = false;
        [DataInput]
        [HiddenIf(nameof(hideVIP))]
        public bool isVIP = false;

        [DataInput]
        [IntegerSlider(0,3,1)]
        public int visibleArguments = 1; 

        [DataInput]
        public CommandAsset Commander;

        //private Dictionary<string, Func<int>> IntValues = new Dictionary<string, Func<int>>();
        //private Dictionary<string, Func<float>> FloatValues = new Dictionary<string, Func<float>>();
        //private Dictionary<string, Func<string>> StringValues = new Dictionary<string, Func<string>>();
        
        [FlowInput]
        public Continuation Enter() {
            //parse, and invoke appropriate flow.
            //check if a command
            Debug.Log("ENTER");
            if((Commander != null) && (message?.StartsWith(Commander.CommandPrefix)??false)) {
                Debug.Log("Found a command!");
                //parse  
                string[] split = message.Substring(1).Split(Commander.ArgumentSeparator);
                Debug.Log("HERES THE SPLIT");
                foreach (string s  in split) {
                    Debug.Log(s);
                }
                Debug.Log("---");

                string command = (string) split.GetValue(0);
                

                //find the correct command
                foreach (var Command in Commander.Commands) { //int i=0; i<Commander.Commands.Length; i++) {
                    string CommandName = Command.command;
                    Debug.Log("Checking command " + CommandName);
                    //var Command = Commander.Commands[i];
                    if ( String.Equals(CommandName, command, StringComparison.InvariantCultureIgnoreCase)) {
                        Debug.Log("thats the one");
                        if (UserIsVerifiedToUse(Command)) {
                            Debug.Log("verified!");
                            ///Command found, user is able to run it 
                            //Parse arguments
                            Debug.Log("parsing args");
                            var ArgAndText = Command.arguments.Zip(split.Skip(1), (a,t) => (a,t));
                            foreach (var (Argument, Text) in ArgAndText) {
                                //var Argument = Command.arguments[j-1];
                                Debug.Log(Argument);
                                string PortName = Command.command + ":" + Argument.ArgumentName;
                                if (ParseDict.ContainsKey(PortName)) {
                                    ParseDict[PortName] = Text;
                                } else {
                                    ParseDict.Add(PortName,Text);
                                }
                                Debug.Log("Registered " + Text + " at output " + CommandName + ":" + Argument.ArgumentName);
                            } 
                            Debug.Log("INVOKE " + CommandName);
                                //argumentText[j-1] = split[j];
                            InvokeFlow(CommandName); 
                        } else {
                            Debug.Log("not verified!");
                        }
                        //FlowOutputPort port = FlowOutputPortCollection.GetPort(CommandName);
                    }
                }
            }
            return null;
        }


        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Commander), () => {
                Watch(Commander,nameof(Commander.Commands), () => {
                    SetupExitPorts(); 
                    SetupOutputPorts();
                });
                SetupExitPorts();
                SetupOutputPorts();
            });

            Watch(nameof(visibleArguments), () => {
                SetupOutputPorts();
            });
            
            SetupExitPorts();
            SetupOutputPorts();
        } 
 
        public bool UserIsVerifiedToUse(CommandAsset.Command command) {
            if (command.accesibleToEveryone || 
                (command.accesibleToBroadcaster && isBroadcaster) ||
                (command.accesibleToVIPs && isVIP) ||
                (command.accesibleToModerators && isModerator) ||
                (command.accesibleToSubscribers && isSubscriber)) {
                    return true;
            } 
            foreach (string GroupName in command.AccesibleTo) {
                var Group = command.Parent.groupDict[GroupName];
                foreach (string member in Group.members) {
                    if ( member == username) {
                        return true;
                    }
                }
            }
            return false;
        }

        
        public void SetupExitPorts() {
            var ExitCount = Commander?.Commands?.Length ?? 0;

            FlowOutputPortCollection.GetPorts().Clear();
            for (var i = 1; i <= ExitCount; i++) {
                var commandName = Commander.Commands[i-1].command;
                AddFlowOutputPort(commandName, new FlowOutputProperties {
                    label = commandName, //"EXIT".Localized() + " " + i
                    description = "The flow output triggered on recieving a " + commandName + " command"
                });
            }

            Broadcast();
        }

        public void SetupOutputPorts() {
            Debug.Log("Setting up output ports");
            DataOutputPortCollection.GetPorts().Clear();
            if (Commander != null) {
                Debug.Log("Setting up output ports - cleared");
                foreach (CommandAsset.Command Command in Commander.Commands) {
                    
                    string CommandName = Command.command;
                    Debug.Log("Setting up output ports - command " + CommandName);
                    int i=0; 
                    foreach (CommandAsset.Command.Argument Argument in Command.arguments) {
                        string ArgumentName = Argument.ArgumentName;
                         Debug.Log("Setting up output ports - command " + CommandName + " with argument " + ArgumentName);
                        Type ArgumentType = typeof(int);
                        switch(Argument.ArgumentType) {
                            case CommandAsset.ParseType.FLOAT:
                                ArgumentType=typeof(float);
                                
                                Debug.Log(CommandName + ":" + ArgumentName);
                                AddDataOutputPort(
                                    CommandName + ":" + ArgumentName,
                                    ArgumentType,
                                    () => {
                                        float parsed = 0.0f;
                                        bool success = float.TryParse(ParseDict[CommandName + ":" + ArgumentName], out parsed);
                                        return success?parsed:0;
                                    },
                                    new DataOutputProperties {
                                        label = CommandName + ":" + ArgumentName,
                                        description = "The parsed output from parsing the " + ArgumentName + " argument in a " + CommandName + " command"
                                    }
                                );
                                break;
                            case CommandAsset.ParseType.INT: //XXX return how to parse an int!!!
                                ArgumentType=typeof(int);
                                Debug.Log(CommandName + ":" + ArgumentName);
                                AddDataOutputPort(
                                    CommandName + ":" + ArgumentName,
                                    ArgumentType,
                                    () => {
                                        int parsed = 0;
                                        bool success = int.TryParse(ParseDict[CommandName + ":" + ArgumentName], out parsed);
                                        return success?parsed:0;
                                    },
                                    new DataOutputProperties {
                                        label = CommandName + ":" + ArgumentName,
                                        description = "The parsed output from parsing the " + ArgumentName + " argument in a " + CommandName + " command"
                                    }
                                );
                                break;
                            case CommandAsset.ParseType.STRING:
                                ArgumentType=typeof(string);
                                Debug.Log(CommandName + ":" + ArgumentName);
                                AddDataOutputPort(
                                    CommandName + ":" + ArgumentName,
                                    ArgumentType,
                                    () => {
                                        Debug.Log("OUTOUT GETTING " + CommandName + ":" + ArgumentName);
                                        Debug.Log(ParseDict.ContainsKey(CommandName + ":" + ArgumentName));
                                        Debug.Log(ParseDict);
                                        return ParseDict.ContainsKey(CommandName + ":" + ArgumentName)?ParseDict[CommandName + ":" + ArgumentName]:"";
                                    },
                                    new DataOutputProperties {
                                        label = CommandName + ":" + ArgumentName,
                                        description = "The parsed output from parsing the " + ArgumentName + " argument in a " + CommandName + " command"
                                    }
                                );
                                break;
                            
                        } 
                        i++; 
                    }
                }
            }
            Broadcast();
        }
    }

    /*heartContainer = Plugin.ModHost.Assets.Load<GameObject>("Assets/Feline's Heart Containers/Heart.prefab");
GameObject go = Object.Instantiate(heartContainer);
go.transform.parent = this.GameObject.transform;*/
}