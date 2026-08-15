using Plana.Core;

namespace Plana.Roles.Modifier;

public class Chameleon : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    private Chameleon() : base("chameleon", "CAN", new(130, 193, 111), [QuiescentTime])
    {
    }

    Citation? HasCitation.Citation => Citations.TheOtherRoles;

    static private FloatConfiguration QuiescentTime = NebulaAPI.Configurations.Configuration("options.role.chameleon.quiescentTime", (0.25f, 2f, 0.25f), 1f, FloatConfigurationDecorator.Second);

    static public Chameleon MyRole = new Chameleon();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        private UnityEngine.Vector2 lastPosition;
        private float lastMoveTime;
        [Local]
        void OnGameStarted(GameStartEvent ev)
        {
            lastMoveTime = 0f;
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                lastPosition = MyPlayer.TruePosition;
                lastMoveTime = 0f;
            }
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (lastPosition != MyPlayer.TruePosition)
            {
                lastMoveTime = 0f;
                lastPosition = MyPlayer.TruePosition;
            }
            else
            {
                lastMoveTime += ev.DeltaTime;
            }
            if (lastMoveTime > QuiescentTime)
            {
                MyPlayer.GainAttribute(MyPlayer.IsImpostor ? PlayerAttributes.InvisibleElseImpostor : PlayerAttributes.Invisible, 0.25f, false, 0, "chameleon:vanish");
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo||inEndScene) name += "Þ".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}


