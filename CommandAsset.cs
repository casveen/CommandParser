using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Cysharp.Threading.Tasks;
using Warudo.Core.Data;
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
        public bool CaseSensitive = false;

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
        public UserGroup[] Groups; 
        public Dictionary<string, UserGroup> GroupDict = new Dictionary<string, UserGroup>();

        [DataInput]
        [Label("Use Twitch Subsgriber Group")]
        public bool UseTwitchSubscriberGroup = false;
        
        [DataInput]
        [Label("Use Twitch VIP Group")]
        public bool UseTwitchVIPGroup = false;

        [DataInput]
        [Label("Use Twitch Moderator Group")]
        public bool UseTwitchModeratorGroup = false;

        [DataInput]
        [Label("Use Twitch Broadcaster")]
        public bool UseTwitchBroadcaster = false;

        [DataInput]
        [Label("Case-sensitive Names")]
        public bool CaseSensitiveNames = false;



        public class Command : StructuredData<CommandAsset>, ICollapsibleStructuredData {
            [DataInput]
            [Label("Command Name")]
            public string Name = null;


            [DataInput]
            [Label("Accesible To Everyone")]
            public bool AccesibleToEveryone = true;

            protected bool AccesibleToEveryoneGetter() => AccesibleToEveryone;
            protected async UniTask<AutoCompleteList> GetUserGroups() {
                return AutoCompleteList.Single(LoadListAsync(Parent?.Groups));
            }
            protected System.Collections.Generic.IEnumerable<Warudo.Core.Data.AutoCompleteEntry> LoadListAsync(UserGroup[] toLoad) {
                foreach (UserGroup ug in toLoad) {
                    yield return new AutoCompleteEntry {
                        label = ug.Name,
                        value = ug.Name
                    };
                }
            }
            [DataInput]
            [HiddenIf(nameof(AccesibleToEveryoneGetter))]
            [Label("Accesible To Groups")]
            [AutoComplete(nameof(GetUserGroups), true)]
            public string[] AccesibleTo; //has to be string, so as to play nice with AutoCompleteList

            protected bool HideSubscriber() { return AccesibleToEveryone || (!Parent.UseTwitchSubscriberGroup); }
            protected bool HideVIP() { return AccesibleToEveryone || (!Parent.UseTwitchVIPGroup); }
            protected bool HideModerator() { return AccesibleToEveryone || (!Parent.UseTwitchModeratorGroup); }
            protected bool HideBroadcaster() { return AccesibleToEveryone || (!Parent.UseTwitchBroadcaster); }
            protected bool HideArguments() { return !HasArguments; }

            [DataInput]
            [HiddenIf(nameof(HideSubscriber))]
            [Label("Accesible To Subscribers")]
            public bool AccesibleToSubscribers = true; 

            [DataInput]
            [HiddenIf(nameof(HideVIP))]
            [Label("Accesible To VIPs")]
            public bool AccesibleToVIPs = true; 

            [DataInput]
            [HiddenIf(nameof(HideModerator))]
            [Label("Accesible To Moderators")]
            public bool AccesibleToModerators = true; 

            [DataInput]
            [HiddenIf(nameof(HideBroadcaster))]
            [Label("Accesible To Broadcaster")]
            public bool AccesibleToBroadcaster = true; 

            [DataInput]
            [Label("Has Arguments")]
            public bool HasArguments = false; 

            [DataInput]
            [HiddenIf(nameof(HideArguments))]
            [Label("Arguments")]
            public Argument[] Arguments;
            public class Argument : StructuredData, ICollapsibleStructuredData {
                [DataInput]
                [Label("Argument Name")]
                public string Name;

                [DataInput]
                [Label("Argument Type")]
                public ParseType Type;

                public string GetHeader() {
                    return Name??"<Argument name not set>";
                }
            }

            public string GetHeader() {
                return Name??"<Command name not set>";
            }
        }

        
        public class UserGroup : StructuredData, ICollapsibleStructuredData {
            [DataInput]
            [Label("Name of group")]
            public string Name = "";

            [DataInput]
            [Label("Name of members")]
            public string[] Members;

            public string GetHeader() {
                return Name??"<Group name not set>";
            }
        }

        protected override void OnCreate() {
            base.OnCreate();
            SetActive(true);

            //tie name of group to actual group. Needed because of the AutoCompleteList restriction. An extremely inefficient implementation
            Watch(nameof(Groups), () => {
                GroupDict.Clear();
                foreach (UserGroup Group in Groups) {
                    GroupDict.Add(Group.Name, Group);
                }
                Broadcast();
            });

        }
    }
}