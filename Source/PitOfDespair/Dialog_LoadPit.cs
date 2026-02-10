using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace PitOfDespair { 

public class Dialog_LoadPit : Window
{
    private const float TitleRectHeight = 35f;

    private const float BottomAreaHeight = 55f;

    private static readonly List<TabRecord> tabsList = new List<TabRecord>();

    private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

    private readonly Map map;

    private readonly List<CompPit> transporters;

    private float cachedCaravanMassCapacity;

    private string cachedCaravanMassCapacityExplanation;

    private float cachedCaravanMassUsage;

    private Pair<float, float> cachedDaysWorthOfFood;

    private Pair<ThingDef, float> cachedForagedFoodPerDay;

    private string cachedForagedFoodPerDayExplanation;

    private float cachedMassUsage;

    private float cachedTilesPerDay;

    // private string cachedTilesPerDayExplanation; // Unused

    private float cachedVisibility;

    private string cachedVisibilityExplanation;

    private bool caravanMassCapacityDirty = true;

    private bool caravanMassUsageDirty = true;

    private bool daysWorthOfFoodDirty = true;

    private bool foragedFoodPerDayDirty = true;

    // private TransferableOneWayWidget itemsTransfer; // Unused

    private float lastMassFlashTime = -9999f;

    private bool massUsageDirty = true;

    private TransferableOneWayWidget pawnsTransfer;

    private Tab tab;

    private bool tilesPerDayDirty = true;

    private List<TransferableOneWay> transferables;

    private bool visibilityDirty = true;

    public Dialog_LoadPit(Map map, List<CompPit> transporters)
    {
        this.map = map;
        this.transporters = new List<CompPit>();
        this.transporters.AddRange(transporters);
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

    protected override float Margin => 0f;

    private float MassCapacity
    {
        get
        {
            var num = 0f;
            for (var i = 0; i < transporters.Count; i++)
            {
                num += transporters[i].Props.massCapacity;
            }

            return num;
        }
    }

    private float CaravanMassCapacity
    {
        get
        {
            if (!caravanMassCapacityDirty)
            {
                return cachedCaravanMassCapacity;
            }

            caravanMassCapacityDirty = false;
            var stringBuilder = new StringBuilder();
            cachedCaravanMassCapacity =
                CollectionsMassCalculator.CapacityTransferables(transferables, stringBuilder);
            cachedCaravanMassCapacityExplanation = stringBuilder.ToString();

            return cachedCaravanMassCapacity;
        }
    }

    private string TransportersLabel => Find.ActiveLanguageWorker.Pluralize(transporters[0].parent.Label);

    private string TransportersLabelCap => TransportersLabel.CapitalizeFirst();

    private BiomeDef Biome => map.Biome;

    private float MassUsage
    {
        get
        {
            if (!massUsageDirty)
            {
                return cachedMassUsage;
            }

            massUsageDirty = false;
            cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true);

            return cachedMassUsage;
        }
    }

    public float CaravanMassUsage
    {
        get
        {
            if (!caravanMassUsageDirty)
            {
                return cachedCaravanMassUsage;
            }

            caravanMassUsageDirty = false;
            cachedCaravanMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload);

            return cachedCaravanMassUsage;
        }
    }

    private float TilesPerDay
    {
        get
        {
            if (!tilesPerDayDirty)
            {
                return cachedTilesPerDay;
            }

            tilesPerDayDirty = false;
            cachedTilesPerDay = 0f;
            // cachedTilesPerDayExplanation = "";

            return cachedTilesPerDay;
        }
    }

    private Pair<float, float> DaysWorthOfFood
    {
        get
        {
            if (!daysWorthOfFoodDirty)
            {
                return cachedDaysWorthOfFood;
            }

            daysWorthOfFoodDirty = false;
            var first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, map.Tile,
                IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer);
            cachedDaysWorthOfFood = new Pair<float, float>(first,
                DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, map.Tile,
                    IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload));

            return cachedDaysWorthOfFood;
        }
    }

    private Pair<ThingDef, float> ForagedFoodPerDay
    {
        get
        {
            if (!foragedFoodPerDayDirty)
            {
                return cachedForagedFoodPerDay;
            }

            foragedFoodPerDayDirty = false;
            var stringBuilder = new StringBuilder();
            var result = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables, Biome, Faction.OfPlayer,
                    stringBuilder);
            cachedForagedFoodPerDay = new Pair<ThingDef, float>(result.food, result.perDay);
            cachedForagedFoodPerDayExplanation = stringBuilder.ToString();

            return cachedForagedFoodPerDay;
        }
    }

    private float Visibility
    {
        get
        {
            if (!visibilityDirty)
            {
                return cachedVisibility;
            }

            visibilityDirty = false;
            var stringBuilder = new StringBuilder();
            cachedVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
            cachedVisibilityExplanation = stringBuilder.ToString();

            return cachedVisibility;
        }
    }

    public override void PostOpen()
    {
        base.PostOpen();
        CalculateAndRecacheTransferables();
    }

    public override void DoWindowContents(Rect inRect)
    {
        var rect = new Rect(0f, 0f, inRect.width, 35f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, "LoadTransporters".Translate(TransportersLabel));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        tabsList.Clear();
        tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate { tab = Tab.Pawns; }, tab == Tab.Pawns));
        inRect.yMin += 119f;
        Widgets.DrawMenuSection(inRect);
        TabDrawer.DrawTabs(inRect, tabsList);
        inRect = inRect.ContractedBy(17f);
        GUI.BeginGroup(inRect);
        var rect2 = inRect.AtZero();
        DoBottomButtons(rect2);
        var inRect2 = rect2;
        inRect2.yMax -= 59f;
        var anythingChanged = false;
        if (tab == Tab.Pawns)
        {
            pawnsTransfer.OnGUI(inRect2, out anythingChanged);
        }

        if (anythingChanged)
        {
            CountToTransferChanged();
        }

        GUI.EndGroup();
    }

    public override bool CausesMessageBackground()
    {
        return true;
    }

    private void AddToTransferables(Thing t)
    {
        var transferableOneWay =
            TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
        if (transferableOneWay == null)
        {
            transferableOneWay = new TransferableOneWay();
            transferables.Add(transferableOneWay);
        }

        transferableOneWay.things.Add(t);
    }

    private void DoBottomButtons(Rect rect)
    {
        var rect2 = new Rect((rect.width / 2f) - (BottomButtonSize.x / 2f), rect.height - 55f, BottomButtonSize.x,
            BottomButtonSize.y);
        if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false))
        {
            if (CaravanMassUsage > CaravanMassCapacity && CaravanMassCapacity != 0f)
            {
                if (!CheckForErrors(TransferableUtility.GetPawnsFromTransferables(transferables)))
                {
                }
            }
            else if (TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(false);
            }
        }

        var rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
        if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false))
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            CalculateAndRecacheTransferables();
        }

        var rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
        if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false))
        {
            if (transporters?.Any() == true)
            {
                transporters[0].prisonerthrowingdone = false;
                transporters[0].leftToLoad?.Clear();
            }

            Close();
        }

        if (!Prefs.DevMode)
        {
            return;
        }

        var width = 200f;
        var num = BottomButtonSize.y / 2f;
        var rect5 = new Rect(0f, rect.height - 55f, width, num);
        if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false) && DebugTryLoadInstantly())
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            Close(false);
        }

        var rect6 = new Rect(0f, rect.height - 55f + num, width, num);
        if (!Widgets.ButtonText(rect6, "Dev: Select everything", true, false))
        {
            return;
        }

        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        SetToLoadEverything();
    }

    private void CalculateAndRecacheTransferables()
    {
        transferables = new List<TransferableOneWay>();
        AddPawnsToTransferables();
        IEnumerable<TransferableOneWay> enumerable = null;
        string text = null;
        string text2 = null;
        string text3 = "FormCaravanColonyThingCountTip".Translate();
        var flag = true;
        var ignorePawnsInventoryMode = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
        var flag2 = true;
        var func = () => MassCapacity - MassUsage;
        var tile = map.Tile;
        pawnsTransfer = new TransferableOneWayWidget(enumerable, text, text2, text3, flag, ignorePawnsInventoryMode,
            flag2, func, 0f, false, tile, true, true, true, false, true);
        CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
        enumerable = transferables.Where(x => x.ThingDef.category != ThingCategory.Pawn);
        text3 = null;
        text2 = null;
        text = "FormCaravanColonyThingCountTip".Translate();
        flag2 = true;
        ignorePawnsInventoryMode = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
        flag = true;
        func = () => MassCapacity - MassUsage;
        tile = map.Tile;
        CountToTransferChanged();
    }

    private bool DebugTryLoadInstantly()
    {
        CreateAndAssignNewTransportersGroup();
        int i;
        for (i = 0; i < transferables.Count; i++)
        {
            var i1 = i;
            TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer,
                delegate (Thing splitPiece, IThingHolder _)
                {
                    transporters[i1 % transporters.Count].GetDirectlyHeldThings().TryAdd(splitPiece);
                });
        }

        return true;
    }

    private bool TryAccept()
    {
        var pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
        if (!CheckForErrors(pawnsFromTransferables))
        {
            return false;
        }

        var transportersGroup = CreateAndAssignNewTransportersGroup();
        AssignTransferablesToRandomTransporters();
        var enumerable = pawnsFromTransferables.Where(x => x.IsColonist && !x.Downed);
        if (enumerable.Any())
        {
            foreach (var item in enumerable)
            {
                item.GetLord()?.Notify_PawnLost(item, PawnLostCondition.ForcedToJoinOtherLord);
            }

            LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterPit(transportersGroup), map, enumerable);
            foreach (var item2 in enumerable)
            {
                if (item2.Spawned)
                {
                    item2.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }

        Messages.Message("PD_PrisonersBeingLoaded".Translate(), transporters[0].parent, MessageTypeDefOf.TaskCompletion,
            false);
        return true;
    }

    private void AssignTransferablesToRandomTransporters()
    {
        var transferableOneWay = transferables.MaxBy(x => x.CountToTransfer);
        var num = 0;
        foreach (var oneWay in transferables)
        {
            if (oneWay == transferableOneWay || oneWay.CountToTransfer <= 0)
            {
                continue;
            }

            transporters[num % transporters.Count]
                .AddToTheToLoadList(oneWay, oneWay.CountToTransfer);
            num++;
        }

        if (num < transporters.Count)
        {
            var num2 = transferableOneWay.CountToTransfer;
            var num3 = num2 / (transporters.Count - num);
            for (var j = num; j < transporters.Count; j++)
            {
                var num4 = j != transporters.Count - 1 ? num3 : num2;
                if (num4 > 0)
                {
                    transporters[j].AddToTheToLoadList(transferableOneWay, num4);
                }

                num2 -= num4;
            }
        }
        else
        {
            transporters[num % transporters.Count]
                .AddToTheToLoadList(transferableOneWay, transferableOneWay.CountToTransfer);
        }
    }

    private int CreateAndAssignNewTransportersGroup()
    {
        var nextTransporterGroupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
        foreach (var pit in transporters)
        {
            pit.groupID = nextTransporterGroupID;
        }

        return nextTransporterGroupID;
    }

    private bool CheckForErrors(List<Pawn> pawns)
    {
        if (!transferables.Any(x => x.CountToTransfer != 0))
        {
            Messages.Message("PD_NoPrisonersSelected".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        var num = 0;
        foreach (var oneWay in transferables)
        {
            if (oneWay.CountToTransfer != 0)
            {
                num++;
            }
        }

        var num2 = 0;
        if (transporters[0].innerContainer != null)
        {
            num2 = transporters[0].innerContainer.Count;
        }

        if (num + num2 > transporters[0].Props.maxPrisoners)
        {
            Messages.Message("PD_NoSpaceInThePit".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        var pawn = pawns.Find(x => !x.MapHeld.reachability.CanReach(x.PositionHeld, transporters[0].parent,
            PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)));
        if (pawn != null)
        {
            Messages.Message("PD_UnreacheablePit".Translate(pawn.LabelShort, pawn).CapitalizeFirst(),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        var parentMap = transporters[0].parent.Map;
        foreach (var oneWay in transferables)
        {
            if (oneWay.ThingDef.category != ThingCategory.Item)
            {
                continue;
            }

            var countToTransfer = oneWay.CountToTransfer;
            var num3 = 0;
            if (countToTransfer <= 0)
            {
                continue;
            }

            foreach (var thing in oneWay.things)
            {
                if (!parentMap.reachability.CanReach(thing.Position, transporters[0].parent, PathEndMode.Touch,
                        TraverseParms.For(TraverseMode.PassDoors)))
                {
                    continue;
                }

                num3 += thing.stackCount;
                if (num3 >= countToTransfer)
                {
                    break;
                }
            }

            if (num3 >= countToTransfer)
            {
                continue;
            }

            if (countToTransfer == 1)
            {
                Messages.Message("PD_UnreacheablePit".Translate(), MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                Messages.Message("PD_UnreacheablePit".Translate(), MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        return true;
    }

    private void AddPawnsToTransferables()
    {
        var list = CaravanFormingUtility.AllSendablePawns(map);
        foreach (var pawn in list)
        {
            if (pawn.IsPrisonerInPrisonCell() && pawn.MentalStateDef == null)
            {
                AddToTransferables(pawn);
            }
        }
    }

    private void AddItemsToTransferables()
    {
        var list = CaravanFormingUtility.AllReachableColonyItems(map);
        foreach (var thing in list)
        {
            AddToTransferables(thing);
        }
    }

    private void FlashMass()
    {
        lastMassFlashTime = Time.time;
    }

    private void SetToLoadEverything()
    {
        foreach (var oneWay in transferables)
        {
            oneWay.AdjustTo(oneWay.GetMaximumToTransfer());
        }

        CountToTransferChanged();
    }

    private void CountToTransferChanged()
    {
        massUsageDirty = true;
        caravanMassUsageDirty = true;
        caravanMassCapacityDirty = true;
        tilesPerDayDirty = true;
        daysWorthOfFoodDirty = true;
        foragedFoodPerDayDirty = true;
        visibilityDirty = true;
    }

    private enum Tab
    {
        Pawns
    }
} }