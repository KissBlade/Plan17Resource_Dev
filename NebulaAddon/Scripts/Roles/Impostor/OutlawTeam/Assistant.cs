using AmongUs.GameOptions;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Virial.Helpers;

namespace Plana.Roles.Impostor;

public class AssistantShower : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private AssistantShower() : base("assistant", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.CrewmateTeam, null, false, true, () => false)
    {
    }
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    string DefinedAssignable.InternalName => "assistantShower";
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public AssistantShower MyRole = new AssistantShower();
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
                MyPlayer.SetRole(Assistant.MyRole);
            }
        }
    }
}
public class Assistant : DefinedRoleTemplate, DefinedRole, HasCitation,IAssignableDocument
{
    private Assistant() : base("assistant", NebulaTeams.ImpostorTeam.Color, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam,null,false,true,()=>false)
    {
    }
    bool IGuessed.CanBeGuess => false;
    bool DefinedRole.IsMadmate => true;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public Assistant MyRole = new Assistant();
    RoleCategory DefinedCategorizedAssignable.Category
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return RoleCategory.CrewmateRole;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;
        }
    }
    bool IAssignableDocument.HasTips => true;
    RoleTeam DefinedSingleAssignable.Team
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return NebulaTeams.CrewmateTeam;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        DefinedRole RuntimeRole.ExternalRecognitionRole => AssistantShower.MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        static RemoteProcess<bool> SetAssistantCanKillState = new("SetAssistantCanKillStateRPC", (m, _) =>
        {
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                assistant.CanKill = m;
                if (assistant.CanKill)
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Impostor);
                }
                else
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Crewmate);
                }
            }
        });

        public Instance(GamePlayer player) : base(player)
        {
        }
        bool RuntimeRole.HasVanillaKillButton => CanKill;
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
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
                    SetAssistantCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
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
                if (!MyPlayer.IsDead)
                {
                    SetAssistantCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    {
                        MyPlayer.AddModifier(GuesserModifier.MyRole);
                    }
                }
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
        ModAbilityButton ShapeshiftButton;
        public void RevertPlayerToNormal(bool shouldAnimate = true)
        {
            if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted || MeetingHud.Instance.AsBoolFast())
            {
                VanillaRpc.ShapeshiftRpc.Invoke((MyPlayer, MyPlayer, shouldAnimate,OutlawLeaderShower.AssistantShapeshiftLeaveEvidence));
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ShapeshiftButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, OutlawLeaderShower.AssistantShapeshiftCooldown, OutlawLeaderShower.AssistantShapeshiftDuration, "assistant.shapeshift", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image), (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false, true).SetLabelType(ModAbilityButton.LabelType.Impostor);
                ShapeshiftButton.OnClick = (button) =>
                {
                    if (!button.IsInEffect)
                    {
                        var shapeshift = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                        MyPlayer.ToAUPlayer().NetTransform.Halt();
                        ShapeshifterMinigame shapeshifterMinigame = UnityEngine.Object.Instantiate<ShapeshifterMinigame>(shapeshift.ShapeshifterMenu);
                        shapeshifterMinigame.transform.SetParent(Camera.main.transform, false);
                        shapeshifterMinigame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                        shapeshifterMinigame.Begin(null);
                    }
                    else
                    {
                        button.InterruptEffect();
                    }
                };
                ShapeshiftButton.OnEffectStart = (button) =>
                {
                    button.SetLabel("almighty.unshapeshift");
                };
                ShapeshiftButton.OnEffectEnd = (button) =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted)
                    {
                        MyPlayer.ToAUPlayer().NetTransform.Halt();
                        RevertPlayerToNormal(!MyPlayer.Logic.InVent);
                    }
                    button.StartCoolDown();
                    button.SetLabel("assistant.shapeshift");
                };
                (ShapeshiftButton.EffectTimer as TimerImpl).SetPredicate(() =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted && ShapeshiftButton.EffectTimer.IsProgressing)
                    {
                        return true;
                    }
                    return false;
                });
                (ShapeshiftButton.CoolDownTimer as TimerImpl).SetPredicate(() =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Default && ShapeshiftButton.CoolDownTimer.IsProgressing && AmongUsLLImpl.LocalPlayer.IsKillTimerEnabled)
                    {
                        return true;
                    }
                    return false;
                });
            }
            GameOperatorManager.Instance!.Subscribe<MeetingPreStartEvent>(delegate (MeetingPreStartEvent ev)
            {
                MyPlayer.Unbox().RemoveOutfit("Shapeshift");
                MyPlayer.ToAUPlayer().CurrentOutfitType = PlayerOutfitType.Default;
                if (MyPlayer.AmOwner)
                {
                    ShapeshiftButton?.InterruptEffect();
                }
            }, this);
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                RevertPlayerToNormal(!MyPlayer.Logic.InVent);
            }
        }
    }
}