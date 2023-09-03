using Warudo.Core.Graphs;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Playground {
    //[NodeType(),
    [NodeType(
    Id = nameof(CommandParserNode), // Must be unique. Generate one at https://guidgenerator.com/
    Title = "CommandParser",
    Category ="Fnugus")]
    public class CommandParserNode : Node {
        //protected Dictionary<string, string> ParseDict = new Dictionary<string, string>();

        protected bool HideSubscriber() { return !(Commander?.UseTwitchSubscriberGroup??false); }
        protected bool HideModerator() { return !(Commander?.UseTwitchModeratorGroup??false); }
        protected bool HideBroadcaster() { return !(Commander?.UseTwitchBroadcaster??false); }
        protected bool HideVIP() { return !(Commander?.UseTwitchVIPGroup??false); }

        [DataInput]
        public string Username = "";
        [DataInput]
        public string Message = "";
        [DataInput]
        [HiddenIf(nameof(HideSubscriber))]
        public bool IsSubscriber = false;

        [DataInput]
        [HiddenIf(nameof(HideBroadcaster))]
        public bool IsBroadcaster = false;
        [DataInput]
        [HiddenIf(nameof(HideModerator))]
        public bool IsModerator = false;
        [DataInput]
        [HiddenIf(nameof(HideVIP))]
        public bool IsVIP = false;

        [DataInput]
        public CommandAsset Commander;

        [FlowInput]
        public Continuation Enter() {
            //parse command, check if user can use the command, find arguments and invoke appropriate flow.
            //note that the actual parsing happens "in" the outputports, not here.
            StringComparison comparisonType = Commander.CaseSensitive switch {
                true  => StringComparison.InvariantCultureIgnoreCase, 
                false => StringComparison.InvariantCulture
            };
            //check if a command
            if((Commander != null) && (Message?.StartsWith(Commander.CommandPrefix)??false)) {
                string[] split = Message.Substring(1).Split(Commander.ArgumentSeparator);
                string parsedCommand = (string) split.GetValue(0);
                //find the correct command
                foreach (CommandAsset.Command command in Commander.Commands) {
                    if (String.Equals(command.Name, parsedCommand, comparisonType)) {
                        if (UserIsVerifiedToUse(command)) {
                            //command found, user is able to run it
                            //assign strings to be parsed to each argument(via a dictionary)
                            foreach (var (argument, text) in command.Arguments.Zip(split.Skip(1), (a,t) => (a,t))) {
                                string portName = command.Name + ":" + argument.Name;
                                if (Commander.ParseDict.ContainsKey(portName)) { Commander.ParseDict[portName] = text;} 
                                else {Commander.ParseDict.Add(portName,text);}
                            } 
                            InvokeFlow(command.Name); 
                            Commander.TriggerEvent(command);
                            return null;
                        } 
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
            SetupExitPorts();
            SetupOutputPorts();
        } 
 
        protected bool UserIsVerifiedToUse(CommandAsset.Command command) {
            StringComparison comparisonType = Commander.CaseSensitiveNames switch {
                true  => StringComparison.InvariantCultureIgnoreCase, 
                false => StringComparison.InvariantCulture
            };
            if (command.AccesibleToEveryone || 
                (command.AccesibleToBroadcaster && IsBroadcaster) ||
                (command.AccesibleToVIPs && IsVIP) ||
                (command.AccesibleToModerators && IsModerator) ||
                (command.AccesibleToSubscribers && IsSubscriber)) {
                    return true;
            } 
            //check if user is in one of the groups who can run the command
            foreach (string groupName in command.AccesibleTo) {
                foreach (string member in command.Parent.GroupDict[groupName].Members) {
                    if (string.Equals(Username, member, comparisonType)) {
                        return true;
                    }
                }
            }
            return false;
        }

        
        public void SetupExitPorts() {
            int exitCount = Commander?.Commands?.Length ?? 0;
            FlowOutputPortCollection.GetPorts().Clear();
            for (int i = 0; i < exitCount; i++) {
                string commandName = Commander.Commands[i].Name;
                AddFlowOutputPort(commandName, new FlowOutputProperties {
                    label = commandName,
                    description = "The flow output triggered on recieving a " + commandName + " command"
                });
            }
            Broadcast();
        }

        public void SetupOutputPorts() {
            DataOutputPortCollection.GetPorts().Clear();
            if (Commander != null) {
                foreach (CommandAsset.Command command in Commander.Commands) {
                    string commandName = command.Name;
                    foreach (CommandAsset.Command.Argument argument in command.Arguments) {
                        string argumentName = argument.Name;
                        string portName = commandName + ":" + argumentName;
                        var parseDict = Commander.ParseDict;
                        AddDataOutputPort(
                            portName,
                            argument.Type switch {
                                CommandAsset.ParseType.FLOAT =>  typeof(float),
                                CommandAsset.ParseType.INT =>    typeof(int),
                                CommandAsset.ParseType.STRING => typeof(string)
                            },
                            argument.Type switch {
                                CommandAsset.ParseType.FLOAT => 
                                    () => {
                                        float parsed = 0.0f;
                                        bool success = float.TryParse(Commander.ParseDict[portName], out parsed);
                                        return success?parsed:0;
                                    },
                                CommandAsset.ParseType.INT =>
                                    () => {
                                        int parsed = 0;
                                        bool success = int.TryParse(Commander.ParseDict[portName], out parsed);
                                        return success?parsed:0;
                                    },
                                CommandAsset.ParseType.STRING => 
                                    () => {
                                        return Commander.ParseDict.ContainsKey(portName)?Commander.ParseDict[portName]:"";
                                    } 
                            },
                            new DataOutputProperties {
                                label = portName,
                                description = "The parsed output from parsing the " + argumentName + " argument in a " + commandName + " command"
                            }
                        );
                    }
                }
            }
            Broadcast();
        }
    }
}