using Warudo.Core.Graphs;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using Cysharp.Threading.Tasks;

namespace Playground {
    //[NodeType(),
    [NodeType(
    Id = nameof(OnCommandNode), // Must be unique. Generate one at https://guidgenerator.com/
    Title = "OnCommand",
    Category ="Fnugus")]
    public class OnCommandNode : Node {

        protected async UniTask<AutoCompleteList> GetCommands() {
            return AutoCompleteList.Single(LoadListAsync(Commander?.Commands));
        }
            protected System.Collections.Generic.IEnumerable<Warudo.Core.Data.AutoCompleteEntry> LoadListAsync(CommandAsset.Command[] toLoad) {
                foreach (CommandAsset.Command command in toLoad) {
                    yield return new AutoCompleteEntry {
                        label = command.Name,
                        value = command.Name
                    };
                }
            }

        public bool NoCommander() {return Commander == null;}

        [DataInput]
        [HiddenIf(nameof(NoCommander))]
        [AutoComplete(nameof(GetCommands), true)]
        public String CommandName;
        public CommandAsset.Command Command;

        [DataInput]
        public CommandAsset Commander;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Commander), () => {
                if (Commander!=null) {
                    //find the corresponding command, since we only know the name
                    if (CommandName!=null) {
                        foreach (CommandAsset.Command command in Commander.Commands) {
                            if (command.Name == CommandName) {
                                Command = command;
                                break;
                            }
                        }
                    }
                    //register as an event in commanderasset
                    Commander.RegisterEvent(this);
                    Watch(Commander,nameof(Commander.Commands), () => {
                        SetupOutputPorts();
                    });
                }
                SetupOutputPorts();
            });
            Watch(nameof(CommandName), () => {
                if (CommandName != null) {
                    //find the corresponding command, since we only know the name
                    if (Commander!=null) {
                        foreach (CommandAsset.Command command in Commander.Commands) {
                            if (command.Name == CommandName) {
                                Command = command;
                                break;
                            }
                        }
                    }
                    //register as an event in commanderasset
                    Commander?.RegisterEvent(this);
                }
                SetupOutputPorts();
            });

            Commander?.RegisterEvent(this);
            SetupOutputPorts();
        } 

        [FlowOutput]
        [Label("OnCalled")]
        public Continuation OnCalled;

        public void SetupOutputPorts() { 
            DataOutputPortCollection.GetPorts().Clear();
            if (Commander != null && Command != null) {
                string commandName = Command.Name;
                foreach (CommandAsset.Command.Argument argument in Command.Arguments) {
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
                Broadcast();
            }
        }
    }
}