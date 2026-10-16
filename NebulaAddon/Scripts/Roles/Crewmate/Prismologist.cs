using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula.Patches;
using Nebula.Roles.Scripts;
using Plana.Core;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Roles.Crewmate;

public class Prismologist : DefinedSingleAbilityRoleTemplate<Prismologist.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private const float extendLen = 2f;
    private const float alpha = 0.2f;
    private Prismologist() : base("prismologist", new(40,255,218), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [PrismUses,PrismCooldown,VisibleNextRound]) { }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CannotLoad;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    public static readonly IntegerConfiguration PrismUses = NebulaAPI.Configurations.Configuration("options.role.prismologist.prismUses", (1, 5), 2);
    public static readonly FloatConfiguration PrismCooldown = NebulaAPI.Configurations.Configuration("options.role.prismologist.prismCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly BoolConfiguration VisibleNextRound = NebulaAPI.Configurations.Configuration("options.role.prismologist.visibleNextRound", true);
    static public Prismologist MyRole = new Prismologist();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.PlaceIcon, "role.prismologist.ability.prism");
    }

    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {
        public static Image PlaceIcon = NebulaAPI.AddonAsset.GetResource("prism.png")!.AsImage(115f)!;
        int leftUses=PrismUses.GetValue();
        Prism? firstPrism;
        List<Prism> prisms = new();
        Vector2 prismPointA, prismB;
        ModAbilityButton? placeButton;
        Dictionary<byte, PlayerDisplay> mirrors = new();
        Dictionary<byte, Dictionary<string, GameObject>> extras = new();
        Dictionary<byte, GameObject> ghosts = new();
        List<GameObject> shown = new();

        public Ability(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                placeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, PrismCooldown, "prism", PlaceIcon, _ => MyPlayer.CanMove, _ => !MyPlayer.IsDead && leftUses > 0);
                placeButton.SetLabelType(ModAbilityButton.LabelType.Crewmate).ShowUsesIcon(3, leftUses.ToString());
                placeButton.OnClick = button =>
                {
                    SetPrism();
                    button.UpdateUsesIcon(leftUses.ToString());
                    button.StartCoolDown();
                };
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    foreach (var phantom in Phantoms()) UnityEngine.Object.Destroy(phantom);
                }, this);
            }
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (prisms.Count == 0 || MyPlayer.IsDead || MeetingHud.Instance != null)
            {
                foreach (var phantom in Phantoms()) phantom.SetActive(false);
                return;
            }
            shown.Clear();
            foreach (var p in GamePlayer.AllPlayers)
            {
                if (p.IsDead || p.IsInvisible || p.Logic.InVent) continue;
                var mirrored = Reflect(p.TruePosition.ToUnityVector());
                if (mirrored != null) shown.Add(ShowMirror(p, mirrored.Value));
            }
            foreach (var body in Helpers.AllDeadBodies())
            {
                var mirrored = Reflect(body.TruePosition);
                if (mirrored != null) shown.Add(ShowCorpse(body, mirrored.Value));
            }
            foreach (var phantom in Phantoms()) if (!shown.Contains(phantom)) phantom.SetActive(false);
            foreach (var phantom in shown)
                foreach (var renderer in phantom.GetComponentsInChildren<SpriteRenderer>(true)) renderer.color = renderer.color with { a = alpha };
        }
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            leftUses = PrismUses.GetValue();
            placeButton?.UpdateUsesIcon(leftUses.ToString());
            if (firstPrism != null)
            {
                NebulaSyncObject.LocalDestroy(firstPrism.ObjectId);
                firstPrism = null;
            }
        }
        [Local]
        void OnOutfitChanged(PlayerOutfitChangeEvent ev)
        {
            if (mirrors.TryGetValue(ev.Player.PlayerId, out var mirror)) mirror.UpdateFromPlayerOutfit(ev.Player.VanillaPlayer, false, false);
        }
        void SetPrism()
        {
            Vector2 pos = MyPlayer.TruePosition.ToUnityVector();
            if (firstPrism == null)
            {
                firstPrism = Prism.Place(pos, 0);
                leftUses--;
                return;
            }
            prisms.Do(p => NebulaSyncObject.RpcDestroy(p.ObjectId));
            prisms.Clear();
            prismPointA = firstPrism.Position.ToUnityVector();
            prismB = pos;
            prisms.Add(firstPrism);
            prisms.Add(Prism.Place(pos, firstPrism.ObjectId));
            firstPrism = null;
            leftUses--;
            if (VisibleNextRound)
            {
                prisms.Do(obj => NebulaGameManager.Instance!.Scheduler.Schedule(RPCScheduler.RPCTrigger.AfterMeeting, NebulaSyncObject.RpcInstantiateDef.GetInvoker((obj.ObjectId, obj.TagHash, obj.argumentsCache, true))));

            }
        }
        Vector2? Reflect(Vector2 worldPos)
        {
            Vector2 lensVec = prismB - prismPointA;
            Vector2 lensDir = lensVec.normalized;
            float along = Vector2.Dot(worldPos - prismPointA, lensDir);
            if (along < -extendLen || along > lensVec.magnitude + extendLen) return null;
            Vector2 foot = prismPointA + lensDir * along;
            Vector2 mirrored = 2f * foot - worldPos;
            if (Vector2.Distance(mirrored, worldPos) < 0.5f) return null;
            return mirrored;
        }
        GameObject ShowMirror(GamePlayer player, Vector2 target)
        {
            if (!mirrors.TryGetValue(player.PlayerId, out var mirror))
            {
                mirror = VanillaAsset.GetPlayerDisplay();
                mirror.name = "PrismPhantom";
                Dress(mirror.gameObject);
                mirror.UpdateFromPlayerOutfit(player.VanillaPlayer, false, false);
                mirrors[player.PlayerId] = mirror;
            }
            var bodyType = player.VanillaPlayer.MyPhysics.Animations.type;
            if (mirror.Animations.type != bodyType) mirror.SetBodyType(bodyType);
            mirror.gameObject.SetActive(true);
            Vector2 delta = target - (Vector2)mirror.transform.position;
            mirror.transform.position = target.AsVector3(target.y / 1000f);
            mirror.transform.localRotation = player.VanillaPlayer.transform.localRotation;
            mirror.transform.localScale = player.VanillaPlayer.transform.localScale;
            if (delta.sqrMagnitude > 0.0001f)
            {
                var flip = mirror.Cosmetics.FlipX;
                if (delta.x < -0.01f) mirror.Cosmetics.SetFlipX(true);
                else if (delta.x > 0.01f) mirror.Cosmetics.SetFlipX(false);
                if (!mirror.Animations.IsPlayingRunAnimation() || flip != mirror.Cosmetics.FlipX) mirror.Animations.PlayRunAnimation();
                if (!mirror.Cosmetics.IsSkinPlayingRunAnim()) mirror.Cosmetics.AnimateSkinRun();
            }
            else if (mirror.Animations.IsPlayingRunAnimation() || !mirror.Animations.IsPlayingSomeAnimation())
            {
                mirror.Animations.PlayIdleAnimation();
                mirror.Cosmetics.AnimateSkinIdle();
            }
            var source = player.VanillaPlayer.transform;
            if (!extras.TryGetValue(player.PlayerId, out var extra))
            {
                extra = new();
                extras[player.PlayerId] = extra;
            }
            foreach (var pair in extra) if (source.Find(pair.Key) == null) pair.Value.SetActive(false);
            for (int i = 0; i < source.childCount; i++)
            {
                var child = source.GetChild(i);
                if (child.GetComponentInChildren<CosmeticsLayer>(true) == player.VanillaPlayer.cosmetics) continue;
                if (!extra.TryGetValue(child.name, out var copy))
                {
                    copy = UnityEngine.Object.Instantiate(child.gameObject, mirror.transform);
                    copy.name = "PrismHold_" + child.name;
                    foreach (var script in copy.GetComponentsInChildren<Behaviour>(true)) UnityEngine.Object.Destroy(script);
                    foreach (var collider in copy.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.Destroy(collider);
                    extra[child.name] = copy;
                }
                var fromT = child.GetComponentsInChildren<Transform>(true);
                var toT = copy.GetComponentsInChildren<Transform>(true);
                for (int k = 0; k < toT.Length && k < fromT.Length; k++)
                {
                    toT[k].gameObject.SetActive(fromT[k].gameObject.activeSelf);
                    toT[k].localPosition = fromT[k].localPosition;
                    toT[k].localRotation = fromT[k].localRotation;
                    toT[k].localScale = fromT[k].localScale;
                }
                var fromR = child.GetComponentsInChildren<SpriteRenderer>(true);
                var toR = copy.GetComponentsInChildren<SpriteRenderer>(true);
                for (int k = 0; k < toR.Length && k < fromR.Length; k++)
                {
                    toR[k].sprite = fromR[k].sprite;
                    toR[k].flipX = fromR[k].flipX;
                    toR[k].flipY = fromR[k].flipY;
                    toR[k].enabled = fromR[k].enabled;
                    toR[k].color = fromR[k].color;
                }
            }
            return mirror.gameObject;
        }
        GameObject ShowCorpse(DeadBody body, Vector2 target)
        {
            if (!ghosts.TryGetValue(body.ParentId, out var ghost))
            {
                ghost = UnityEngine.Object.Instantiate(body.gameObject, null);
                ghost.name = "PrismCorpse";
                foreach (var script in ghost.GetComponentsInChildren<Behaviour>(true)) UnityEngine.Object.Destroy(script);
                foreach (var collider in ghost.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.Destroy(collider);
                Dress(ghost);
                ghosts[body.ParentId] = ghost;
            }
            ghost.SetActive(true);
            ghost.transform.position = target.AsVector3(target.y / 1000f);
            var from = body.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            var to = ghost.GetComponentsInChildren<SpriteRenderer>(true);
            for (int k = 0; k < to.Length && k < from.Length; k++)
            {
                to[k].sprite = from[k].sprite;
                to[k].flipX = from[k].flipX;
                to[k].flipY = from[k].flipY;
                to[k].enabled = from[k].enabled;
            }
            return ghost;
        }
        void Dress(GameObject obj)
        {
            UnityHelper.DoForAllChildren(obj, child => { child.tag = "Untagged"; child.layer = LayerExpansion.GetPlayersLayer(); }, true);
            foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true)) renderer.maskInteraction = SpriteMaskInteraction.None;
        }
        IEnumerable<GameObject> Phantoms()
        {
            foreach (var mirror in mirrors.Values) yield return mirror.gameObject;
            foreach (var ghost in ghosts.Values) yield return ghost;
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class Prism : NebulaSyncStandardObject
    {
        static Image PrismImage = NebulaAPI.AddonAsset.GetResource("prism.png")!.AsImage(115f)!;
        static Prism()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (float[] args) => new Prism(new Virial.Compat.Vector2(args[0], args[1]), (int)args[2]));
        }
        public static Prism Place(Vector2 pos, int linkId) => (Prism)NebulaSyncObject.LocalInstantiate(MyTag, [pos.x, pos.y, linkId]).SyncObject!;
        public Prism(Virial.Compat.Vector2 pos, int linkId) : base(pos, NebulaSyncStandardObject.ZOption.Back, true, PrismImage.GetSprite(), false)
        {
            if (linkId==0) return;
            Prism? other = NebulaSyncObject.GetObject<Prism>(linkId);
            if (other == null) return;
            Vector2 pa = pos.ToUnityVector();
            Vector2 pb = other.Position.ToUnityVector();
            float dx = pb.x - pa.x;
            float dy = pb.y - pa.y;
            float len = Vector2.Distance(pa, pb);
            if (len < 0.01f) return;
            float ex = dx / len * extendLen;
            float ey = dy / len * extendLen;
            LineRenderer sandVein = UnityHelper.CreateObject<LineRenderer>("Prism", MyTransform, UnityEngine.Vector3.zero);
            sandVein.material = new(NebulaAsset.MultiplyShader);
            sandVein.startWidth = 0.07f;
            sandVein.endWidth = 0.07f;
            sandVein.startColor = MyRole.RoleColor.ToUnityColor();
            sandVein.endColor = sandVein.startColor;
            sandVein.positionCount = 2;
            sandVein.SetPositions(new Il2CppStructArray<UnityEngine.Vector3>([
                new UnityEngine.Vector3(pa.x - ex, pa.y - ey, -0.1f),
                new UnityEngine.Vector3(pb.x + ex, pb.y + ey, -0.1f)]));
        }

        public static string MyTag = "Prism";
    }
}
