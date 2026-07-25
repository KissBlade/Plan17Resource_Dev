using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Injection;
using Nebula.Map;
using Nebula.Modules.Cosmetics;
using Nebula.Roles.Complex;
using Plana.Core;
using Virial.Events.Game.Minimap;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Impostor;

public class CrawlerEngineer : DefinedSingleAbilityRoleTemplate<CrawlerEngineer.Ability>, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, HasCitation, IAssignableDocument
{
    private CrawlerEngineer() : base("crawlerengineer", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [CrawlerHasWatching,CrawlerNoCamouflager,PatchManager.ImpostorMeetingChat])
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/CrawlerEngineer.png")!.AsImage(115f)!;
    }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/CrawlerEngineer.png")!.AsImage();
    static public readonly BoolConfiguration CrawlerHasWatching = NebulaAPI.Configurations.Configuration("role.crawlerengineer.haswatching", true);
    static public readonly BoolConfiguration CrawlerNoCamouflager= NebulaAPI.Configurations.Configuration("role.crawlerengineer.nocamouflager", false);
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueMapAbility;
    public override CrawlerEngineer.Ability CreateAbility(Player player, int[] arguments)
    {
        return new CrawlerEngineer.Ability(player, arguments.GetAsBool(0));
    }
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("CrawlerEngineer Patch");
        harmony.Patch(typeof(PlayerModInfo).GetMethod("AddOutfit"), new HarmonyMethod(typeof(CrawlerEngineer.Ability).GetMethod("BlockAddOutfit")));
        PDebug.Log("Done");
    }
    static public CrawlerEngineer MyRole = new CrawlerEngineer();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.skillImage, "role.crawlerengineer.ability.MAP");
        yield return new(Ability.MeetingSkill, "role.crawlerengineer.ability.CMK");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%HASw%", Language.Translate(CrawlerHasWatching ? "role.crawlerengineer.ability.haswatching" : "role.crawlerengineer.ability.nowatching"));
        yield return new("%NOc%", Language.Translate(CrawlerNoCamouflager ? "role.crawlerengineer.ability.nocamouflager" : "role.crawlerengineer.ability.camouflager"));
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {
        private ModAbilityButton? skillButton = null;
        internal static Virial.Media.Image MeetingSkill = NebulaAPI.AddonAsset.GetResource("CrawlerMeetingSkill.png")!.AsImage(100f)!;
        internal static Virial.Media.Image skillImage = NebulaAPI.AddonAsset.GetResource("CrawlerUsePadSkill.png")!.AsImage(100f)!;
        ShowPlayersMapLayer? mapCountLayer;
        public static Dictionary<GamePlayer,RoleTeam>? RunTeamInstances=new Dictionary<GamePlayer,RoleTeam>();
        void IGameOperator.OnReleased()
        {
            if (RunTeamInstances.ContainsKey(MyPlayer))
            {
                RunTeamInstances.Remove(MyPlayer);
            }
        }
        void OnGameStarted(GameStartEvent ev)
        {
            MarkedPlayer = new Dictionary<byte, int>();
        }
        public static bool BlockAddOutfit(GamePlayer __instance,OutfitCandidate outfit)
        {
            if (CrawlerNoCamouflager)
            {
                if (GamePlayer.LocalPlayer!.Role.GetAbility<CrawlerEngineer.Ability>()==null)
                {
                    return true;
                }
                if (outfit.Tag.ToLower().Contains("camo") || outfit.Tag.ToLower().Contains("commscfeffect"))
                {
                    if (__instance.Role.GetAbility<CrawlerEngineer.Ability>()!=null)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }
        void UpdateNameText(PlayerDecorateNameEvent ev)
        {
            if (MyPlayer.TryGetAbility<Ability>(out var a))
            {
                if (!RunTeamInstances.ContainsValue(MyPlayer.Role.Role.Team))
                {
                    RunTeamInstances.Add(MyPlayer, MyPlayer.Role.Role.Team);
                }
            }
            else if(RunTeamInstances.ContainsKey(MyPlayer))
            {
                RunTeamInstances.Remove(MyPlayer);
            }
            if (RunTeamInstances.Any(p=>p.Key!=MyPlayer&&p.Value==MyPlayer.Role.Role.Team))
            {
                return;
            }
            if (!GamePlayer.LocalPlayer!.CanKill(MyPlayer)&&!NebulaGameManager.Instance!.CanSeeAllInfo)
            {
                if (!MyPlayer.CanKill(ev.Player)&&!GamePlayer.LocalPlayer.CanKill(ev.Player)&&!ev.Player.CanKill(MyPlayer)&&!ev.Player.CanKill(GamePlayer.LocalPlayer))
                {
                    if (ev.Player.AmOwner)
                    {
                        return;
                    }
                    string text = string.Empty;
                    foreach (var modifier in ev.Player.Modifiers)
                    {
                        if (modifier.Modifier.LocalizedName.Contains("guesser"))
                        {
                            modifier.DecorateNameConstantly(ref text, true,false);
                        }
                        else if (modifier.Modifier.LocalizedName.Contains("watching"))
                        {
                            modifier.DecorateNameConstantly(ref text, true,false);
                        }
                        else if (modifier.Modifier.LocalizedName.Contains("tieBreaker"))
                        {
                            modifier.DecorateNameConstantly(ref text, true,false);
                        }
                    }
                    ev.Name += text;
                }
            }
        }
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (MyPlayer.TryGetAbility<Ability>(out var a))
            {
                if (!RunTeamInstances.ContainsValue(MyPlayer.Role.Role.Team))
                {
                    RunTeamInstances.Add(MyPlayer, MyPlayer.Role.Role.Team);
                }
            }
            else if (RunTeamInstances.ContainsKey(MyPlayer))
            {
                RunTeamInstances.Remove(MyPlayer);
            }
            if (RunTeamInstances.Any(p => p.Key != MyPlayer && p.Value == MyPlayer.Role.Role.Team))
            {
                return;
            }
            if (!GamePlayer.LocalPlayer!.CanKill(MyPlayer)&& !NebulaGameManager.Instance!.CanSeeAllInfo)
            {
                if (!MyPlayer.CanKill(ev.Player)&&!GamePlayer.LocalPlayer.CanKill(ev.Player)&&!ev.Player.CanKill(MyPlayer)&&!ev.Player.CanKill(GamePlayer.LocalPlayer))
                {
                    string text = ev.Player.Role.DisplayColoredName;
                    foreach (var modifier in ev.Player.Modifiers)
                    {
                        if (modifier.Modifier.LocalizedName.Contains("jailer"))
                        {
                            var newtext = modifier.OverrideRoleName(text, false,true);
                            if (newtext != null)
                            {
                                text = newtext;
                            }
                            break;
                        }
                    }
                    if (MarkedPlayer.TryGetValue(ev.Player.PlayerId, out int rid))
                    {
                        text+=" "+Language.Translate("role.crawlerMarkPlayerText").Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[rid].DisplayColoredName);
                    }
                    ev.Alternate(text);
                    return;
                }
                if (AmOwner&&ev.Player.IsModMadmate())
                {
                    ev.Append(Language.Translate("role.madmate.name").Color(Palette.ImpostorRed));
                }
                if (MarkedPlayer.TryGetValue(ev.Player.PlayerId,out int id))
                {
                    ev.Alternate(" "+Language.Translate("role.crawlerMarkPlayerText").Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[id].DisplayColoredName));
                }
            }
        }
        MetaScreen? LastWindow;
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        static public MetaScreen OpenRoleSelectWindow(Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), AmongUsLLImpl.HudManagerInstance.transform, new UnityEngine.Vector3(0, 0, -50f), true, false);
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            inner.Append(Nebula.Roles.Roles.AllRoles.Where(predicate), r => new CombinedWidgetOld(-1f,IMetaWidgetOld.AlignmentOption.Center,new MetaWidgetOld.HorizonalMargin(0.1f),
                new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (button, renderer, text) =>
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                        button.transform.localPosition += new Vector3(0.05f, 0f, 0f);
                        text.transform.localPosition += new Vector3(0.072f, 0f, 0f);
                        SpriteRenderer spriteRenderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new Vector3(-0.65f, 0f, -0.1f), null);
                        Image roleIcon = r.GetRoleIcon();
                        spriteRenderer.sprite = ((roleIcon != null) ? roleIcon.GetSprite() : null);
                        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        spriteRenderer.material = RoleIcon.GetRoleIconMaterial(r, 0.8f, null);
                        spriteRenderer.transform.localScale = new Vector3(0.253f, 0.253f, 1f);
                        spriteRenderer.SetBothOrder(15);
                } }), 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                while (MeetingHud.Instance.state != MeetingHud.VoteStates.Results) yield return null;
                window.CloseScreen();
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        static public MetaScreen OpenWindow(Action<DefinedRole> onSelected)
        {
            return MeetingRoleSelectWindow.OpenRoleSelectWindow(Nebula.Roles.Roles.AllRoles,r=>!r.IsSystemRole,true,Language.Translate("role.crawlerEngineer.Marktarget"), onSelected);
        }
        static Dictionary<byte, int> MarkedPlayer = new Dictionary<byte, int>();
        RemoteProcess<ValueTuple<byte,int>> AddMarkPlayerRpc = new RemoteProcess<ValueTuple<byte,int>>("markPlayer",delegate(ValueTuple<byte,int> message,bool _)
        {
            MarkedPlayer.Add(message.Item1, message.Item2);
        },true);
        void OnMeetingStart(MeetingStartEvent ev)
        {
            MarkedPlayer = new Dictionary<byte, int>();
            if (AmOwner)
            {
                NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(MeetingSkill,
                state =>
                {
                    var p = state.MyPlayer;
                    LastWindow = OpenWindow((r) =>
                    {
                        try
                        {
                            if (AmongUsLLImpl.LocalPlayer.Data.IsDead) return;
                            if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                            AddMarkPlayerRpc.Invoke(new ValueTuple<byte, int>(p.PlayerId, r.Id));
                            if (LastWindow) LastWindow!.CloseScreen();
                            LastWindow = null!;
                        }
                        catch (Exception e)
                        {
                            PDebug.Log(e);
                        }
                    }
                    );
                },
                p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && !MarkedPlayer.TryGetValue(p.MyPlayer.PlayerId, out int roleid) && !AmongUsLLImpl.LocalPlayer.Data.IsDead
                ));
            }
        }

        [Local]
        private void OnOpenMap(AbstractMapOpenEvent ev)
        {
            try
            {
                if (MyPad != null && !(ev is MapOpenAdminEvent) && !base.IsUsurped && !MeetingHud.Instance && !base.MyPlayer.IsDead)
                {
                    if (!mapCountLayer)
                    {
                        mapCountLayer = UnityHelper.CreateObject<ShowPlayersMapLayer>("CountLayer", MapBehaviour.Instance.transform, new UnityEngine.Vector3(0f, 0f, -1f), null);
                        this.BindGameObject(mapCountLayer.gameObject);
                        mapCountLayer.SetUp(p => true, null!, true,this);
                    }
                    mapCountLayer!.gameObject.SetActive(true);
                    MapBehaviour.Instance?.ColorControl?.SetColor(new(171,0,126, 255));
                    return;
                }
                if (mapCountLayer)
                {
                    mapCountLayer!.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        bool useMap;
        bool kill;
        [Local]
        void OnKill(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner)
            {
                kill = true;
            }
        }
        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (kill &&ev.Player.AmOwner)
            {
                new StaticAchievementToken("crawlerengineer.another1");
            }
        }
        [Local]
        void OnTaskRestart(TaskPhaseRestartEvent ev)
        {
            kill = false;
        }
        public Ability(Player player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                useMap = false;
                kill = false;
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability);
                skillButton.Availability = (button) => MyPlayer.CanMove;
                skillButton.Visibility = (button) => !MyPlayer.IsDead;
                skillButton.SetLabel("equip");
                skillButton.SetImage(skillImage!);
                skillButton.OnClick = (button) =>
                {
                    useMap = true;
                    if (MyPad == null)
                    {
                        new StaticAchievementToken("crawlerengineer.common1");
                    }
                    button.SetLabel(MyPad == null ? "unequip" : "equip");
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, MyPad== null));
                };
                skillButton.OnBroken = delegate (ModAbilityButton button)
                {
                    if (MyPad != null)
                    {
                        button.SetLabel("equip");
                        RpcEquip.Invoke(new ValueTuple<byte, bool>(base.MyPlayer.PlayerId, false));
                    }
                };
                skillButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                GameOperatorManager instance = GameOperatorManager.Instance!;
                if (instance != null)
                {
                    instance.Subscribe<MeetingStartEvent>(delegate (MeetingStartEvent ev)
                    {
                        skillButton.SetLabel("equip");
                        if (MyPad != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, this, 100);
                    instance.Subscribe<PlayerMurderedEvent>(ev =>
                    {
                        if (ev.Murderer.AmOwner && ev.Player.PlayerState == PlayerState.Guessed&&useMap)
                        {
                            new StaticAchievementToken("crawlerengineer.challenge1");
                        }
                    }, this);
                    instance.Subscribe<MeetingEndEvent>(ev =>
                    {
                        useMap = false;
                    }, this);
                    instance.Subscribe<PlayerDieEvent>(ev =>
                    {
                        if (ev.Player.AmOwner)
                        {
                            if (MyPad != null)
                            {
                                RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                            }
                        }
                    }, this);
                    instance.RegisterOnReleased(() =>
                    {
                        if (MyPad != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, skillButton);
                }
            }
        }
        private void EquipPad()
        {
            MyPad = new CrawlerPad(base.MyPlayer).Register(this);
        }

        private void UnequipPad()
        {
            if (MyPad!=null)
            {
                MyPad.UnUsePad();
            }
            MyPad = null!;
        }
        public CrawlerPad MyPad
        {
            get; private set;
        } = null!;
        static readonly RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipPad",
        (message, _) =>
        {
            var role = GamePlayer.GetPlayer(message.playerId)?.Role;
            var ability = role!.GetAbility<CrawlerEngineer.Ability>();
            if (ability != null)
            {
                if (message.equip)
                    ability.EquipPad();
                else
                    ability.UnequipPad();
            }
        }
        );
    }
    public class CrawlerPad : EquipableAbility
    {
        Virial.Media.MultiImage PadSprite = NebulaAPI.AddonAsset.GetResource("CrawlerPad.png")!.AsMultiImage(4,2, 100f)!;
        protected override float Size => 0.68f;
        protected override float Distance => 0.7f;
        public CrawlerPad(GamePlayer owner) : base(owner, false, "CrawlerPad")
        {
            animing = false;
            spriteIndex = 4;
            NebulaManager.Instance.StartCoroutine(CoAppear().WrapToIl2Cpp());
        }
        protected override void HudUpdate(GameHudUpdateEvent ev)
        {
            return;
        }
        int spriteIndex;
        bool animing;
        float updateSpriteTime;
        void OnUpdate(GameUpdateEvent ev)
        {
            if (animing)
            {
                return;
            }
            updateSpriteTime += Time.deltaTime;
            if (updateSpriteTime >= 0.15f)
            {
                Renderer.sprite = PadSprite.AsLoader(spriteIndex).GetSprite();
                spriteIndex++;
                if (spriteIndex >= 8)
                {
                    spriteIndex = 4;
                }
                updateSpriteTime = 0f;
            }
        }
        System.Collections.IEnumerator CoAppear()
        {
            animing = true;
            for (int i = 0; i <= 1; i++)
            {
                Renderer.sprite = PadSprite.AsLoader(i).GetSprite();
                yield return Effects.Wait(0.08f);
            }
            animing = false;
        }
        System.Collections.IEnumerator CoDisappear()
        {
            animing = true;
            for (int i = 2; i <= 3; i++)
            {
                Renderer.sprite = PadSprite.AsLoader(i).GetSprite();
                yield return Effects.Wait(0.08f);
            }
            Release();
        }
        public void UnUsePad()
        {
            NebulaManager.Instance.StartCoroutine(CoDisappear().WrapToIl2Cpp());
        }
    }
    public class ShowPlayersMapLayer : MonoBehaviour
    {
        static ShowPlayersMapLayer()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ShowPlayersMapLayer>();
        }
        bool showDead;
        private Camera camera;
        private PassiveButton clickButton;
        private CircleCollider2D collider;
        SpriteRenderer cameraDotRenderer = null!, dotRenderer = null!;
        static private readonly Image whiteCircleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.WhiteCircle.png", 100f);
        protected float CurrentMapScale;
        protected UnityEngine.Vector2 CurrentMapCenter;
        CrawlerEngineer.Ability? role;
        void Awake()
        {
            collider = UnityHelper.CreateObject<CircleCollider2D>("Click", transform, new(0f, 0f, -5f));
            collider.radius = 2f;
            collider.isTrigger = true;

            dotRenderer = UnityHelper.CreateObject<SpriteRenderer>("DotRenderer", transform, new(0f, 0f, -25f));
            dotRenderer.sprite = whiteCircleSprite.GetSprite();
            dotRenderer.color = UnityEngine.Color.green;
            dotRenderer.transform.localScale = Vector3.one * 0.45f;

            clickButton = collider.gameObject.SetUpButton(false);

            clickButton.OnMouseOver.AddListener(() =>
            {
                NebulaManager.Instance.SetHelpWidget(clickButton,
                new NoSGameObjectGUIWrapper(Virial.Media.GUIAlignment.Center, () =>
                {
                    var mesh = UnityHelper.CreateMeshRenderer("MeshRenderer", transform, new(0, -0.08f, -1), null);
                    mesh.filter.CreateRectMesh(new(2f, 1.2f));
                    mesh.renderer.sharedMaterial.mainTexture = camera.SetCameraRenderTexture(1000, 600);//200,120
                    mesh.renderer.transform.localScale = MapBehaviourExtension.GetMinimapFlippedScale();
                    return (mesh.renderer.gameObject, new(2f, 1.2f));
                }), true);
            });
            clickButton.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(clickButton));
            camera = UnityHelper.CreateRenderingCamera("ShowPlayerCamera", null, Vector3.zero, 1.75f);//1.6f
            camera.gameObject.AddComponent<IgnoreShadowCamera>().ShowNameText=true;
            camera.gameObject.AddComponent<ResetIgnoreShadowCamera>();
            cameraDotRenderer = UnityHelper.CreateObject<SpriteRenderer>("DotImage", camera.transform, new(0f, 0f, -10f), LayerExpansion.GetDefaultLayer());
            cameraDotRenderer.sprite = whiteCircleSprite.GetSprite();
            CurrentMapCenter = VanillaAsset.GetMapCenter(AmongUsLLImpl.Instance.MapId);
            CurrentMapScale = VanillaAsset.GetMapScale(AmongUsLLImpl.Instance.MapId);
        }
        void Update()
        {
            collider.enabled=camera.enabled;
            dotRenderer.enabled = camera.enabled;
            if (!camera.enabled)
            {
                return;
            }
            //z: -10§Ø§È§§§Œ§»§≥§Ì§ÀÈ]§∏§Î•‹•ø•Û§¨§¢§Î§Œ§«°¢±≥æ∞§Œ•Ø•Í•√•Ø•¨©`•…§œ-5§Ø§È§§§À÷√§±§–§Ë§§
            // ±≥æ∞•Ø•Í•√•Ø•¨©`•… :-5, æÄº∞§”µ„§Œ•Ø•Í•√•Ø: -10
            // EdgeCollider2D.EdgeRadious§¨ π§®§Ω§¶
            var screenPosAsWorld = UnityHelper.ScreenToWorldPoint(Input.mousePosition, LayerExpansion.GetUILayer());
            var worldPosOnMinimap = transform.InverseTransformPoint(screenPosAsWorld);
            var worldPos = VanillaAsset.ConvertFromMinimapPosToWorld(worldPosOnMinimap, AmongUsLLImpl.Instance.MapId);
            camera.transform.position = worldPos.AsUnityVector3();
            worldPosOnMinimap.z = -5f;
            collider.transform.localPosition = worldPosOnMinimap;
            worldPosOnMinimap.z = -25f;
            dotRenderer.transform.localPosition = worldPosOnMinimap;
        }
        public static void OptimizeMap(bool isenable,InfectedOverlay infectedOverlay)
        {
            for (int i = 0; i < infectedOverlay.transform.childCount; i++)
            {
                var transform = infectedOverlay.transform.GetChild(i);
                var spriteRenderer = transform.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    var color = spriteRenderer.color;
                    color.a = isenable ? 0.6f : 1f;
                    spriteRenderer.color = color;
                }
                for (int j = 0; j < transform.childCount; j++)
                {
                    var spriteRenderer2 = transform.GetChild(j).GetComponent<SpriteRenderer>();
                    if (spriteRenderer2 != null)
                    {
                        var color2 = spriteRenderer2.color;
                        color2.a = isenable ? 0.6f : 1f;
                        spriteRenderer2.color = color2;
                    }
                }
            }
        }
        private void OnMapInstantiated(MapInstantiateEvent ev)
        {
            OptimizeMap(role?.MyPad!=null,MapBehaviour.Instance.infectedOverlay);
        }
        public void SetUp(Predicate<IPlayerlike> showPredicate, Action<int> postAction, bool showdead,CrawlerEngineer.Ability role)
        {
            this.showPredicate = showPredicate;
            this.postShownAction = postAction;
            showDead = showdead;
            this.role = role;
            GameOperatorManager.Instance!.Subscribe<MapInstantiateEvent>(OnMapInstantiated, role);
        }
        Dictionary<byte, SpriteRenderer>? mapIcons;
        void OnEnable()
        {
            if (mapIcons!=null)
            {
                foreach (var m in mapIcons.Values)
                {
                    m.gameObject.SetActive(true);
                }
            }
            if (cameraDotRenderer)
            {
                cameraDotRenderer.enabled = true;
            }
            if (camera)
            {
                camera.enabled = true;
            }
            OptimizeMap(true, MapBehaviour.Instance.infectedOverlay);
        }
        void OnDisable()
        {
            if (mapIcons != null)
            {
                foreach (var m in mapIcons.Values)
                {
                    m.gameObject.SetActive(false);
                }
            }
            if (cameraDotRenderer)
            {
                cameraDotRenderer.enabled = false;
            }
            if (camera)
            {
                camera.enabled = false;
            }
            OptimizeMap(false, MapBehaviour.Instance.infectedOverlay);
        }
        private void OnDestroy()
        {
            if (camera)
            {
                UnityEngine.Object.Destroy(camera.gameObject);
            }
        }
        void initializeIcons(MapBehaviour __instance, PlayerControl pc = null!)
        {
            List<PlayerControl> list = new List<PlayerControl>();
            if (pc == null)
            {
                mapIcons = new Dictionary<byte, SpriteRenderer>();
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    list.Add(playerControl);
                }
            }
            else
            {
                list.Add(pc);
            }
            foreach (PlayerControl playerControl2 in list)
            {
                byte playerId = playerControl2.PlayerId;
                mapIcons![playerId] = UnityEngine.Object.Instantiate<SpriteRenderer>(__instance.HerePoint, __instance.HerePoint.transform.parent);
                PlayerMaterial.SetColors(playerControl2.ToNebulaPlayer().CurrentOutfit.outfit.ColorId, mapIcons[playerId]);
            }
        }
        public void Start()
        {
            if (mapIcons == null)
            {
                initializeIcons(MapBehaviour.Instance, null!);
            }
            foreach (byte b in mapIcons!.Keys)
            {
                NetworkedPlayerInfo playerById = GameData.Instance.GetPlayerById(b);
                PlayerMaterial.SetColors(playerById.Object.ToNebulaPlayer().CurrentOutfit.outfit.ColorId, mapIcons[b]);
                mapIcons[b].enabled = !playerById.IsDead;
            }
            IconPool = new Nebula.Utilities.ObjectPool<SpriteRenderer>(AmongUsLLImpl.ShipStatusInstance.MapPrefab.HerePoint, base.transform);
            DeadIconPool = new Nebula.Utilities.ObjectPool<SpriteRenderer>(AmongUsLLImpl.ShipStatusInstance.MapPrefab.HerePoint, base.transform);
            DeadIconPool.OnInstantiated = delegate (SpriteRenderer icon)
            {
                PlayerMaterial.SetColors(UnityEngine.Color.gray, icon);
            };
        }

        public void FixedUpdate()
        {
            try
            {
                if (!AmongUsLLImpl.ShipStatusInstance)
                {
                    return;
                }
                if (role!=null)
                {
                    if (role.MyPad == null)
                    {
                        OnDisable();
                        return;
                    }
                    else
                    {
                        OnEnable();
                    }
                }
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    if (playerControl != null)
                    {
                        byte playerId = playerControl.PlayerId;
                        if (mapIcons!.ContainsKey(playerId))
                        {
                            mapIcons[playerId].enabled = false;
                            if (!playerControl.Data.IsDead)
                            {
                                UnityEngine.Vector3 vector = playerControl.transform.position;
                                vector /= AmongUsLLImpl.ShipStatusInstance.MapScale;
                                vector.x *= Mathn.Sign(AmongUsLLImpl.ShipStatusInstance.transform.localScale.x);
                                vector.z = -1f;
                                mapIcons[playerId].transform.localPosition = vector;
                                mapIcons[playerId].enabled = true;
                            }
                            else
                            {
                                mapIcons[playerId].gameObject.SetActive(false);
                                foreach (var deadbody in GameObject.FindObjectsByType<DeadBody>(FindObjectsSortMode.None))
                                {
                                    UnityEngine.Vector3 vector = deadbody.transform.position;
                                    vector /= AmongUsLLImpl.ShipStatusInstance.MapScale;
                                    vector.x *= Mathn.Sign(AmongUsLLImpl.ShipStatusInstance.transform.localScale.x);
                                    vector.z = -1f;
                                    mapIcons[playerId].transform.localPosition = vector;
                                    PlayerMaterial.SetColors(UnityEngine.Color.gray, mapIcons[playerId]);
                                    mapIcons[playerId].enabled = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        public ShowPlayersMapLayer()
        {
        }

        private Nebula.Utilities.ObjectPool<SpriteRenderer>? IconPool;
        private Nebula.Utilities.ObjectPool<SpriteRenderer>? DeadIconPool;
        private Predicate<IPlayerlike>? showPredicate;
        private Action<int>? postShownAction;
    }
}
