using Mono.Cecil;
using Nebula.Roles.Complex;
using Plana.Roles.Impostor;
using Virial.Helpers;

namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class Trainee : DefinedRoleTemplate, DefinedRole, DefinedAssignable, IAssignableDocument, HasCitation
{
    private Trainee() : base("trainee", new(255, 225, 123), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [LearningWayOption,LearningCoolDownOption, LearningNeedRoundOption, RoleFilterOption,LearnAliveCrewOption]) { }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Trainee.png")!.AsImage();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, Nebula.Roles.Roles.GetRole(arguments.Get(1, -1)), arguments.Skip(1).ToArray());
    static private readonly ValueConfiguration<int> LearningWayOption = NebulaAPI.Configurations.Configuration("options.role.trainee.learningway", ["options.role.trainee.learnafterSeconds", "options.role.trainee.learnafterRounds", "options.role.trainee.both"], 2);
    static private readonly FloatConfiguration LearningCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.trainee.cooldown", (30f, 360f, 30f), 60f, FloatConfigurationDecorator.Second, () => LearningWayOption.GetValue() == 0 || LearningWayOption.GetValue() == 2);
    static private readonly IntegerConfiguration LearningNeedRoundOption = NebulaAPI.Configurations.Configuration("options.role.trainee.needround", (1, 8, 1), 1, () => LearningWayOption.GetValue() == 1 || LearningWayOption.GetValue() == 2);
    public static readonly SimpleRoleFilterConfiguration RoleFilterOption = new SimpleRoleFilterConfiguration("options.role.trainee.abilityFilter")
    {
        RolePredicate = (DefinedRole r) => !r.IsSystemRole && r.IsSpawnableInSomeForm() && !r.IsMadmate&&r!=Insomniacs.MyRole&&r!=Aide.MyRole&&r!=MyRole&&r.Category==RoleCategory.CrewmateRole,
        ScrollerTag = "badTraineeFilter",
        InvertOption = true,
        PreviewOnlySpawnableRoles = true
    };
    internal static BoolConfiguration LearnAliveCrewOption = NebulaAPI.Configurations.Configuration("options.role.trainee.learnalivecrew", true);

    static public Trainee MyRole = new Trainee();
    bool IAssignableDocument.HasTips => true;
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole,RuntimeAssignable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        static internal Virial.Media.Image? buttonImage = NebulaAPI.AddonAsset.GetResource("TraineeSkill.png")?.AsImage(115f);
        ModAbilityButton? skillButton;
        public GamePlayer? selectplayer;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExtendedRole == null ? [MyRole] : MyExtendedRole==Aide.MyRole?[MyExtendedRole]:[MyRole, MyExtendedRole];
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => ExtendedRoleInstance == null ? Array.Empty<IPlayerAbility>() : ExtendedRoleInstance.MyAbilities;
        public DefinedRole? MyExtendedRole { get; set; }
        public RuntimeRole? ExtendedRoleInstance { get; set; }
        bool enableColorDecoration = true;
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole)
        {
            return MyExtendedRole == null ? abilityRole == MyRole : abilityRole == MyExtendedRole;
        }
        void DecorateTraineeTargetColor(PlayerDecorateNameEvent ev)
        {
            try
            {
                if ((AmOwner||(selectplayer?.AmOwner??false))&&enableColorDecoration==true)
                {
                    if (ev.Player != null && selectplayer != null && selectplayer.PlayerId == ev.Player.PlayerId)
                    {
                        ev.Color = Aide.MyRole.RoleColor;
                    }
                    if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                    {
                        ev.Color = Aide.MyRole.RoleColor;
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        string? RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort, bool canSeeAllInfo)
        {
            if ((AmOwner || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false)) && MyExtendedRole != null)
            {
                if (MyExtendedRole==Aide.MyRole)
                {
                    return MyExtendedRole.DisplayColoredName;
                }
                return MyExtendedRole.DisplayColoredName +" " + Language.Translate("role.trainee.aide").Color(Aide.MyRole.UnityColor);
            }
            return null;
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner&&MyExtendedRole==null)
            {
                try
                {
                    GameObject lockSprite = null;
                    bool used = false;
                    selectplayer = null;
                    ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && selectplayer == null, MyRole.RoleColor.ToUnityColor(), false, false);
                    skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                    skillButton.SetImage(buttonImage);
                    (skillButton as ModAbilityButtonImpl).UseCoolDownSupport = false;
                    skillButton.Availability = (ModAbilityButton button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove && !lockSprite.AsBoolFast();
                    skillButton.Visibility = (ModAbilityButton button) => !this.MyPlayer.IsDead && !used;
                    if (LearningWayOption.GetValue() == 0)
                    {
                        var Cooldown = LearningCoolDownOption.GetValue();
                        skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, LearningCoolDownOption).SetAsAbilityTimer().Start();
                    }
                    else if (LearningWayOption.GetValue() == 2)
                    {
                        skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, LearningCoolDownOption).SetAsAbilityTimer().Start();
                        lockSprite = skillButton.AddLockedOverlay();
                        var num = 0;
                        skillButton.OnMeeting = (button) =>
                        {
                            num++;
                            if (num >= LearningNeedRoundOption)
                            {
                                lockSprite.Destroy();
                                lockSprite = null;
                            }
                        };
                    }
                    else if (LearningWayOption.GetValue() == 1)
                    {
                        lockSprite = skillButton.AddLockedOverlay();
                        var num = 0;
                        skillButton.OnMeeting = (button) =>
                        {
                            num++;
                            if (num >= LearningNeedRoundOption)
                            {
                                lockSprite.Destroy();
                                lockSprite = null;
                            }
                        };
                    }
                    skillButton.SetLabel("trainee.learn");
                    skillButton.OnClick = delegate (ModAbilityButton button)
                    {
                        used = true;
                        var player = myTracker.CurrentTarget;
                        var targrtRoleCategory = player.Role.Role.Category;
                        if (MyPlayer.IsTrueCrewmate&&targrtRoleCategory==RoleCategory.CrewmateRole&&!player.IsMadmate)
                        {
                            ShareSelectPlayer(MyPlayer, player);
                            var nextRole = player.Role.Role;
                            var nextArgs = player.Role.RoleArguments;
                            OnCreateRole(MyPlayer,nextRole, nextArgs);
                            player.AddModifier(Manager.MyRole, null);
                            MyPlayer.AddModifier(AideModifier.MyRole, null);
                        }
                        else
                        {
                            var roles = NebulaAPI.CurrentGame.GetAllPlayers().Except([player,MyPlayer]).Select(p => p.Role.Role).ToList();
                            if (LearnAliveCrewOption)
                            {
                                roles = NebulaAPI.CurrentGame.GetAllPlayers().Where(p => !p.IsDead).Except([player,MyPlayer]).Select(p => p.Role.Role).ToList();
                            }
                            List<DefinedRole> crewRoles = roles;
                            if (!MyPlayer.IsTrueCrewmate)
                            {
                                crewRoles = roles.Where(r => RoleFilterOption.Contains(r) && r != player.Role.Role).ToList();
                            }
                            if (crewRoles.Count != 0)
                            {
                                ShareSelectPlayer(MyPlayer,player);
                                var nextRole = crewRoles.Count > 0 ? crewRoles[UnityEngine.Random.Range(0, crewRoles.Count)] : null;
                                var nextArgs = player.Role.RoleArguments;
                                OnCreateRole(MyPlayer,nextRole, nextArgs);
                                player.AddModifier(Manager.MyRole, null);
                                MyPlayer.AddModifier(AideModifier.MyRole, null);
                            }
                            else
                            {
                                var nextRole = Aide.MyRole;
                                var nextArgs = player.Role.RoleArguments;
                                enableColorDecoration = false;
                                OnCreateRole(MyPlayer,nextRole, nextArgs);
                            }
                        }
                    };
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
        [NebulaRPC]
        public static void ShareSelectPlayer(GamePlayer myPlayer,GamePlayer targetPlayer)
        {
            Trainee.Instance instance = null;
            instance = GameOperatorManager.Instance.AllOperators.FirstOrDefault(o => o is Trainee.Instance ins && ins.MyPlayer == myPlayer) as Trainee.Instance;
            if (instance == null)
            {
                PDebug.Log("Not Found Trainee Instance");
                return;
            }
            instance.selectplayer = targetPlayer;
        }
        [NebulaRPC]
        public static void OnCreateRole(GamePlayer myPlayer, DefinedRole role, int[] arguments)
        {
            Trainee.Instance instance = null;
            instance = GameOperatorManager.Instance.AllOperators.FirstOrDefault(o => o is Trainee.Instance ins && ins.MyPlayer == myPlayer) as Trainee.Instance;
            if (instance==null)
            {
                PDebug.Log("Not Found Trainee Instance");
                return;
            }
            instance.MyExtendedRole = role;
            if (role == null)
            {
                if (instance.ExtendedRoleInstance != null)
                {
                    instance.ExtendedRoleInstance.Release();
                    instance.ExtendedRoleInstance = null;
                }
                return;
            }
            instance.ExtendedRoleInstance = instance.MyExtendedRole.CreateInstance(instance.MyPlayer, arguments ?? role.DefaultAssignableArguments ?? []);
            instance.ExtendedRoleInstance.OnActivated();
            instance.ExtendedRoleInstance.Register(instance);
            GameOperatorManager.Instance.RegisterOnReleased(() => instance.ExtendedRoleInstance.Release(), instance);

        }
        int[]? RuntimeAssignable.RoleArguments => [MyExtendedRole!=null?MyExtendedRole.Id:-1,..ExtendedRoleInstance?.RoleArguments??[]];
        public Instance(GamePlayer player, DefinedRole? role, int[] arguments) : base(player)
        {
            if (role!=null)
            {
                MyExtendedRole = role;
                ExtendedRoleInstance = MyExtendedRole.CreateInstance(MyPlayer, arguments ?? role.DefaultAssignableArguments ?? []);
                ExtendedRoleInstance.OnActivated();
                ExtendedRoleInstance.Register(this);
            }
        }
    }
    public class Manager : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation, IAssignableDocument
    {
        bool DefinedAssignable.ShowOnHelpScreen => false;
        bool DefinedAssignable.ShowOnFreeplayScreen => false;
        Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Manager.png")!.AsImage();
        private Manager() : base("manager", new(255, 225, 123), null, true, () => false)
        {

        }
        Citation? HasCitation.Citation => PCitations.PlanaANDKC;
        static public Manager MyRole = new Manager();
        bool IAssignableDocument.HasTips => true;
        RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
        public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
        {
            DefinedModifier RuntimeModifier.Modifier => MyRole;
            public Instance(GamePlayer player) : base(player)
            {
            }
            void RuntimeAssignable.OnActivated()
            {
                if (base.AmOwner)
                {
                    AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
                }
            }
            void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
            {
                if (AmOwner || canSeeAllInfo || inEndScene) name += "◇".Color(MyRole.RoleColor.ToUnityColor());
            }
        }
    }
    public class AideModifier : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation, IAssignableDocument
    {
        bool DefinedAssignable.ShowOnHelpScreen => false;
        Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Aide.png")!.AsImage();
        bool DefinedAssignable.ShowOnFreeplayScreen => false;
        private AideModifier() : base("aidemodifier", new(255, 225, 123), null, true, () => false)
        {

        }
        Citation? HasCitation.Citation => PCitations.PlanaANDKC;
        static public AideModifier MyRole = new AideModifier();
        bool IAssignableDocument.HasTips => true;
        RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
        public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
        {
            DefinedModifier RuntimeModifier.Modifier => MyRole;
            public Instance(GamePlayer player) : base(player)
            {
            }
            void RuntimeAssignable.OnActivated()
            {

            }
        }
    }
}
public class Aide : DefinedRoleTemplate, DefinedRole, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeRole>, HasCitation, IAssignableDocument
{
    private Aide() : base("aide", new(255, 225, 123), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam,null,false,false,()=>false)
    {

    }

    bool IGuessed.CanBeGuess => false;
   Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Aide.png")!.AsImage(); 
    bool DefinedAssignable.ShowOnHelpScreen =>false;

    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    static public Aide MyRole = new Aide();
    bool IAssignableDocument.HasTips => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player)
        {

        }
        void RuntimeAssignable.OnActivated()
        {

        }
    }
}