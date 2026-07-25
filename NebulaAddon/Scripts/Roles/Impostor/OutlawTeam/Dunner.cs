using AmongUs.GameOptions;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Impostor;

public class DunnerShower : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private DunnerShower() : base("dunner", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.CrewmateTeam, null, false, true, () => false)
    {
    }
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    string DefinedAssignable.InternalName => "dunnerShower";
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public DunnerShower MyRole = new DunnerShower();
    bool IAssignableDocument.HasTips => true;
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)OutlawLeaderShower.MyRole).IsSpawnable;
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            return;
            if (AmOwner)
            {
                MyPlayer.SetRole(Dunner.MyRole);
            }
        }
    }
}
public class Dunner : DefinedRoleTemplate, DefinedRole, HasCitation,IAssignableDocument
{
    private Dunner() : base("dunner", NebulaTeams.ImpostorTeam.Color, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, null, false, true, () => false)
    {
    }
    bool IGuessed.CanBeGuess => false;
    bool DefinedRole.IsMadmate => true;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    RoleCategory DefinedCategorizedAssignable.Category 
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return RoleCategory.CrewmateRole;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Dunner.Instance);
            if (myplayer != null && myplayer.Role is Dunner.Instance dunner)
            {
                cankill = dunner.CanKill;
            }
            return cankill? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;
        }
    }
    RoleTeam DefinedSingleAssignable.Team
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return NebulaTeams.CrewmateTeam;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Dunner.Instance);
            if (myplayer!=null&&myplayer.Role is Dunner.Instance dunner)
            {
                cankill = dunner.CanKill;
            }
            return cankill ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
        }
    }
    bool IAssignableDocument.HasTips => true;
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public Dunner MyRole = new Dunner();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.ExternalRecognitionRole => DunnerShower.MyRole;
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        bool RuntimeRole.HasVanillaKillButton => CanKill;
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
        public Instance(GamePlayer player) : base(player)
        {
        }
        static RemoteProcess<bool> SetDunnerCanKillState = new("SetDunnerCanKillStateRPC", (m, _) =>
        {
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Dunner.Instance);
            if (myplayer != null && myplayer.Role is Dunner.Instance dunner)
            {
                dunner.CanKill = m;
                if (dunner.CanKill)
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Impostor);
                }
                else
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Crewmate);
                }
            }
        });
        public bool CanKill = false;
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;
        }

        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }
        [Local]
        void OnLeaderDead(PlayerDieEvent ev)
        {
            NebulaManager.Instance.StartDelayAction(1f, () =>
            {
                if (!MyPlayer.IsDead && ev.Player.Role is OutlawLeader.Instance)
                {
                    SetDunnerCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r=>r is GuesserModifier))
                    {
                        MyPlayer.AddModifier(GuesserModifier.MyRole);
                    }
                }
            });
        }
        [Local]
        void OnLeaderChangeRole(PlayerTryToChangeRoleEvent ev)
        {
            if (!ev.Player.IsDead&&ev.CurrentRole is OutlawLeader.Instance)
            {
                if (!MyPlayer.IsDead&&!NebulaGameManager.Instance!.AllPlayerInfo.Any(p=>!p.IsDead&&p.Role is Assistant.Instance))
                {
                    SetDunnerCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    {
                        MyPlayer.AddModifier(GuesserModifier.MyRole);
                    }
                }
            }
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (OutlawLeader.IsOutlawTeam(ev.Player))
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId != MyPlayer.PlayerId && OutlawLeader.IsOutlawTeam(ev.Player))
            {
                ev.Alternate(ev.Player.Role.Role.DisplayColoredName);
            }
        }

        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (OutlawLeader.IsOutlawTeam(ev.Target))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ObjectTracker<Virial.Game.DeadBody> cleanTracker = ObjectTrackers.ForDeadBody(this, null, base.MyPlayer, (global::Virial.Game.DeadBody d) => true, null, null, false);
                ModAbilityButton cleanButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, VirtualKeyInput.Ability, "cleaner.clean", OutlawLeaderShower.DunnerCleanCooldown, "clean", Cleaner.buttonSprite, (ModAbilityButton _) => cleanTracker.CurrentTarget != null, null, false);
                cleanButton.OnClick = delegate (ModAbilityButton button)
                {
                    NebulaGameManager instance4 = NebulaGameManager.Instance;
                    if (instance4 != null)
                    {
                        instance4.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.CleanCorpseAction);
                    }
                    Virial.Game.DeadBody currentTarget = cleanTracker.CurrentTarget;
                    if (((currentTarget != null) ? currentTarget.Player.MyKiller : null) == MyPlayer)
				{
                        new StaticAchievementToken("cleaner.common2");
                    }
                    AmongUsUtil.RpcCleanDeadBody(cleanTracker.CurrentTarget, MyPlayer.PlayerId, EventDetail.Clean);
                    if (OutlawLeaderShower.DunnerSyncKillAndCleanCoolDownOption)
                    {
                         NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                    cleanButton.StartCoolDown();
                };
                GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(delegate (PlayerKillPlayerEvent ev)
                {
                    if (ev.Murderer.AmOwner)
                    {
                        if (OutlawLeaderShower.DunnerSyncKillAndCleanCoolDownOption)
                        {
                            cleanButton.CoolDownTimer.Start(null);
                            return;
                        }
                        if (cleanButton.CoolDownTimer.CurrentTime < 5f)
                        {
                            ModAbilityButton cleanButton2 = cleanButton;
                            if (cleanButton2 == null)
                            {
                                return;
                            }
                            IVisualTimer coolDownTimer = cleanButton2.CoolDownTimer;
                            if (coolDownTimer == null)
                            {
                                return;
                            }
                            coolDownTimer.Start(new float?(5f));
                        }
                    }
                }, this, 100);
            }
        }
    }
}
