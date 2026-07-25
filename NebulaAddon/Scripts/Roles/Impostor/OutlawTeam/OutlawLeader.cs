using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;
using Virial.Runtime;

namespace Plana.Roles.Impostor;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
public class OutlawTeamAllocator : AbstractModule<Game>, IGameOperator
{
    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new OutlawTeamAllocator());
    }

    private OutlawTeamAllocator()
    {
        this.RegisterPermanently(null);
    }
    private void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        IRoleTable table = ev.RoleTable;
        table.GetPlayers(OutlawLeaderShower.MyRole).Do(p => table.EditRole(p, last => (OutlawLeader.MyRole, null)));
        table.GetPlayers(AssistantShower.MyRole).Do(p => table.EditRole(p, last => (Assistant.MyRole, null)));
        table.GetPlayers(DunnerShower.MyRole).Do(p => table.EditRole(p, last => (Dunner.MyRole, null)));
    }
}
public class OutlawLeaderShower : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private OutlawLeaderShower() : base("outlawLeaderShower", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, 
        [
        new GroupConfiguration("options.role.outlaw.assistantOptions",[AssistantShapeshiftCooldown,AssistantShapeshiftDuration,AssistantShapeshiftLeaveEvidence],new(0.7f, 0.2f, 0.2f))
        ,new GroupConfiguration("options.role.outlaw.dunnerOptions",[DunnerCleanCooldown,DunnerSyncKillAndCleanCoolDownOption],new(0.7f, 0.2f, 0.2f))], true, true, null, null, () =>
    {
        return [new AllocationParameters.ExtraAssignmentInfo((DefinedRole _, int playerId) => new ValueTuple<DefinedRole, int[]>(AssistantShower.MyRole,[0, playerId]), RoleCategory.ImpostorRole),
            new AllocationParameters.ExtraAssignmentInfo((DefinedRole _,int playerId)=>new ValueTuple<DefinedRole,int[]>(DunnerShower.MyRole,[0,playerId]),RoleCategory.ImpostorRole)];
    })
    {
    }
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    DefinedRole[] DefinedRole.AdditionalRoles => [AssistantShower.MyRole,DunnerShower.MyRole];
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    bool IGuessed.CanBeGuess => false;
    static public readonly FloatConfiguration AssistantShapeshiftCooldown = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftcooldown", (0f, 60f,2.5f), 20f,FloatConfigurationDecorator.Second);
    static public readonly FloatConfiguration AssistantShapeshiftDuration = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftduration", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration AssistantShapeshiftLeaveEvidence = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftleaveEvidence", false);
    static public readonly FloatConfiguration DunnerCleanCooldown = NebulaAPI.Configurations.Configuration("options.role.outlaw.dunner.cleancooldown", (0f, 60f, 2.5f),25f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration DunnerSyncKillAndCleanCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.outlaw.dunner.syncKillAndCleanCoolDown",false, null, null);
    static public OutlawLeaderShower MyRole = new OutlawLeaderShower();
    bool IAssignableDocument.HasTips => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            return;
            if (AmOwner)
            {
                MyPlayer.SetRole(OutlawLeader.MyRole);
            }
        }
    }
}
public class OutlawLeader : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private OutlawLeader() : base("outlawLeader", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, null, false, true, () => false)
    {
    }
    public static bool IsOutlawTeam(GamePlayer p)
    {
        if (p==null)
        {
            return false;
        }
        if (p.Role is OutlawLeader.Instance)
        {
            return true;
        }
        if (p.Role is Dunner.Instance)
        {
            return true;
        }
        if (p.Role is Assistant.Instance)
        {
            return true;
        }
        return false;
    }
    bool DefinedAssignable.ShowOnHelpScreen => false;
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)OutlawLeaderShower.MyRole).IsSpawnable;
        }
    }
    static public OutlawLeader MyRole = new OutlawLeader();
    bool IAssignableDocument.HasTips => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (IsOutlawTeam(ev.Target))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (IsOutlawTeam(ev.Player))
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId!=MyPlayer.PlayerId&&IsOutlawTeam(ev.Player))
            {
                ev.Alternate(ev.Player.Role.Role.DisplayColoredName);
            }
        }
        [Local]
        [OnlyMyPlayer]
        void OnKill(PlayerMurderedEvent ev)
        {
            var sidekicks = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => !p.IsDead && (p.Role is Dunner.Instance || p.Role is Assistant.Instance));
            if (sidekicks != null && sidekicks.Count() >1)
            {
                var list = sidekicks.ToList();
                list[UnityEngine.Random.Range(0,list.Count)].Suicide(PlayerStates.Suicide, null, KillParameter.NormalKill);
            }
        }
        [OnlyHost]
        [OnlyMyPlayer]
        private void OnExiled(PlayerExiledEvent ev)
        {
            NebulaGameManager.Instance?.AllPlayerInfo.Where((Player p) => !p.IsDead &&IsOutlawTeam(p)&& p.Role.Role != MyRole).Do(delegate (Player p)
            {
                p.VanillaPlayer.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide);
            });
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
            }
        }
    }
}