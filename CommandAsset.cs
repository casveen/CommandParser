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
using Cysharp.Threading.Tasks;

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
using System.Collections.Generic;


namespace Playground {
    [AssetType(Id = "CommandAsset")]
    public class CommandAsset : Asset {
        public enum ParseType {FLOAT, INT, STRING};
        
        //-----------------------------COMMANDS
        [SectionAttribute("Commands",0)]
        [DataInput]
        public Command[] Commands = new Command[] {};

        [DataInput]
        [Label("Case-sensitive commands")]
        public bool caseSensitive = false;

                [DataInput]
        [Label("Command Prefix")]
        public string CommandPrefix = "\\";

        [DataInput]
        [Label("Argument Separator")]
        public string ArgumentSeparator = " ";

        //-----------------------------GROUPS 
        [SectionAttribute("Groups",1)]
        [DataInput]
        [Label("Groups")]
        public UserGroup[] groups; 
        public Dictionary<string, UserGroup> groupDict = new Dictionary<string, UserGroup>();

        [DataInput]
        [Label("Use Twitch Subsgriber Group")]
        public bool useTwitchSubscriberGroup = false;
        
        [DataInput]
        [Label("Use Twitch VIP Group")]
        public bool useTwitchVIPGroup = false;

        [DataInput]
        [Label("Use Twitch Moderator Group")]
        public bool useTwitchModeratorGroup = false;

        [DataInput]
        [Label("Use Twitch Broadcaster")]
        public bool useTwitchBroadcaster = false;

        [DataInput]
        [Label("Case-sensitive Names")]
        public bool caseSensitiveNames = false;

        

        //CLASSES
        public class Command : StructuredData<CommandAsset>, ICollapsibleStructuredData {

            [DataInput]
            [Label("Command Name")]
            public string command = null;


            [DataInput]
            [Label("Accesible To Everyone")]
            public bool accesibleToEveryone = true;

            public bool accesibleToEveryoneGetter() => accesibleToEveryone;
            
            [DataInput]
            [HiddenIf(nameof(accesibleToEveryoneGetter))]
            //[AutoCompleteResourceAttributenameof(UserGroup), null]
            [Label("Accesible To Groups")]
            [AutoComplete(nameof(GetUserGroups), true)]
            public string[] AccesibleTo; 
            //public UserGroup[] AccesibleToGroups;

            protected bool HideSubscriber() { return accesibleToEveryone || (!Parent.useTwitchSubscriberGroup); }
            protected bool HideVIP() { return accesibleToEveryone || (!Parent.useTwitchVIPGroup); }
            protected bool HideModerator() { return accesibleToEveryone || (!Parent.useTwitchModeratorGroup); }
            protected bool HideBroadcaster() { return accesibleToEveryone || (!Parent.useTwitchBroadcaster); }
            protected bool HideArguments() { return !hasArguments; }

            [DataInput]
            [HiddenIf(nameof(HideSubscriber))]
            [Label("Accesible To Subscribers")]
            public bool accesibleToSubscribers = true; 

            [DataInput]
            [HiddenIf(nameof(HideVIP))]
            [Label("Accesible To VIPs")]
            public bool accesibleToVIPs = true; 

            [DataInput]
            [HiddenIf(nameof(HideModerator))]
            [Label("Accesible To Moderators")]
            public bool accesibleToModerators = true; 

            [DataInput]
            [HiddenIf(nameof(HideBroadcaster))]
            [Label("Accesible To Broadcaster")]
            public bool accesibleToBroadcaster = true; 

            [DataInput]
            [Label("Has Arguments")]
            public bool hasArguments = false; 

            [DataInput]
            [HiddenIf(nameof(HideArguments))]
            [Label("Arguments")]
            public Argument[] arguments;
            public class Argument : StructuredData, ICollapsibleStructuredData {
                [DataInput]
                [Label("Argument Name")]
                public string ArgumentName;

                [DataInput]
                [Label("Argument Type")]
                public CommandAsset.ParseType ArgumentType;

                public string GetHeader() {
                    return ArgumentName??"<Argument name not set>";
                }
            }






            public async UniTask<AutoCompleteList> GetUserGroups() {
                return AutoCompleteList.Single(loadListAsync(Parent?.groups));
            }

            public System.Collections.Generic.IEnumerable<Warudo.Core.Data.AutoCompleteEntry> loadListAsync(UserGroup[] toLoad) {
                foreach (UserGroup ug in toLoad) {
                    yield return new AutoCompleteEntry {
                        label = ug.name,
                        value = ug.name
                    };
                }
            }
            
            //TRIGGER CHANGE IN ALL NODES!!!

            public string GetHeader() {
                return command??"<Command name not set>";
            }

            /*public class Argument : StructuredData, ICollapsibleStructuredData {
                [DataInput]
                public string argumentName;
                public string GetHeader() {
                    return argumentName??"<Argument name not set>";
                }
            }*/
        }

        
        public class UserGroup : StructuredData, ICollapsibleStructuredData {
            [DataInput]
            [Label("Name of group")]
            public string name = "";

            [DataInput]
            [Label("Name of members")]
            public string[] members;

            public string GetHeader() {
                return name??"<Group name not set>";
            }
        }

        protected override void OnCreate() {
            base.OnCreate();
            SetActive(true);
            Watch(nameof(groups), () => {
                groupDict.Clear();// XXX extremely ineffecient...
                foreach (UserGroup Group in groups) {
                    groupDict.Add(Group.name, Group);
                }
                Broadcast();
            });

        }
    }
}