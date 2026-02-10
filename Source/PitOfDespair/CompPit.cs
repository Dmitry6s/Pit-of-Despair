using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PitOfDespair
{

    [StaticConstructorOnStartup]
    public class CompPit : ThingComp, IThingHolder
    {
        private static readonly System.Reflection.MethodInfo PawnTickMethod = typeof(Pawn).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo ThingWithCompsTickMethod = typeof(ThingWithComps).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        
        // Add methods for Rare and Long ticks if they exist, or fallback to standard logic if needed.
        // For Pawn, TickRare might not be exposed or needed if we tick normally.
        // For Things, TickRare is important.
        private static readonly System.Reflection.MethodInfo ThingWithCompsTickRareMethod = typeof(ThingWithComps).GetMethod("TickRare", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo ThingWithCompsTickLongMethod = typeof(ThingWithComps).GetMethod("TickLong", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        
        // Entity methods for simple Things
        private static readonly System.Reflection.MethodInfo EntityTickMethod = typeof(Thing).GetMethod("Tick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo EntityTickRareMethod = typeof(Thing).GetMethod("TickRare", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        private static readonly System.Reflection.MethodInfo EntityTickLongMethod = typeof(Thing).GetMethod("TickLong", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        private static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        private static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/PD_ThrowPrisoner");

        private static readonly Texture2D TentacledAspectTex =
            ContentFinder<Texture2D>.Get("UI/Commands/PD_CommandTentacled");

        private static readonly Texture2D CatAspectTex = ContentFinder<Texture2D>.Get("UI/Commands/PD_CommandCats");

        private static readonly Texture2D BoneAspectTex = ContentFinder<Texture2D>.Get("UI/Commands/PD_CommandBones");

        private static readonly Texture2D NormalAspectTex = ContentFinder<Texture2D>.Get("UI/Commands/PD_CommandNormal");

        private static readonly List<CompPit> tmpTransportersInGroup = new List<CompPit>();

        public string buildingGod = "none";

        private CompLaunchable cachedCompLaunchable;
        public int groupID = -1;

        public ThingOwner innerContainer;

        public List<TransferableOneWay> leftToLoad;

        private bool notifiedCantLoadMore;

        public bool prisonerthrowingdone;

        public CompPit()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public CompProperties_Pit Props => (CompProperties_Pit)props;

        public Map Map => parent.MapHeld;

        public bool AnythingLeftToLoad => FirstThingLeftToLoad != null;

        public bool LoadingInProgressOrReadyToLaunch => groupID >= 0;

        public bool AnyInGroupHasAnythingLeftToLoad => FirstThingLeftToLoadInGroup != null;

        public CompLaunchable Launchable
        {
            get
            {
                if (cachedCompLaunchable == null)
                {
                    cachedCompLaunchable = parent.GetComp<CompLaunchable>();
                }

                return cachedCompLaunchable;
            }
        }

        public Thing FirstThingLeftToLoad
        {
            get
            {
                if (leftToLoad == null)
                {
                    return null;
                }

                foreach (var transferableOneWay in leftToLoad)
                {
                    if (transferableOneWay.CountToTransfer != 0 && transferableOneWay.HasAnyThing)
                    {
                        return transferableOneWay.AnyThing;
                    }
                }

                return null;
            }
        }

        public Thing FirstThingLeftToLoadInGroup
        {
            get
            {
                var list = TransportersInGroup(parent.Map);
                foreach (var pit in list)
                {
                    var firstThingLeftToLoad = pit.FirstThingLeftToLoad;
                    if (firstThingLeftToLoad != null)
                    {
                        return firstThingLeftToLoad;
                    }
                }

                return null;
            }
        }

        public bool AnyInGroupNotifiedCantLoadMore
        {
            get
            {
                var list = TransportersInGroup(parent.Map);
                foreach (var pit in list)
                {
                    if (pit.notifiedCantLoadMore)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool AnyPawnCanLoadAnythingNow
        {
            get
            {
                if (!AnythingLeftToLoad)
                {
                    return false;
                }

                if (!parent.Spawned)
                {
                    return false;
                }

                var allPawnsSpawned = parent.Map.mapPawns.AllPawnsSpawned;
                foreach (var pawn in allPawnsSpawned)
                {
                    if (pawn.CurJobDef == DefDatabase<JobDef>.GetNamed("PD_HaulToPit"))
                    {
                        var transporter = ((JobDriver_HaulToPit)pawn.jobs.curDriver).Transporter;
                        if (transporter != null && transporter.groupID == groupID)
                        {
                            return true;
                        }
                    }

                    if (pawn.CurJobDef != DefDatabase<JobDef>.GetNamed("PD_EnterPit"))
                    {
                        continue;
                    }

                    var transporter2 = ((JobDriver_EnterPit)pawn.jobs.curDriver).Transporter;
                    if (transporter2 != null && transporter2.groupID == groupID)
                    {
                        return true;
                    }
                }

                var list = TransportersInGroup(parent.Map);
                foreach (var pawn in allPawnsSpawned)
                {
                    if (pawn.mindState.duty == null || pawn.mindState.duty.transportersGroup != groupID)
                    {
                        continue;
                    }

                    var compPit = JobGiver_EnterPit.FindMyTransporter(list, pawn);
                    if (compPit != null && pawn
                            .CanReach((LocalTargetInfo)compPit.parent, PathEndMode.Touch, Danger.Deadly))
                    {
                        return true;
                    }
                }

                foreach (var pawn in allPawnsSpawned)
                {
                    if (!pawn.IsColonist)
                    {
                        continue;
                    }

                    foreach (var compPit in list)
                    {
                        if (LoadPitJobUtility.HasJobOnTransporter(pawn, compPit))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref groupID, "groupID");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Collections.Look(ref leftToLoad, "leftToLoad", LookMode.Deep);
            Scribe_Values.Look(ref notifiedCantLoadMore, "notifiedCantLoadMore");
            Scribe_Values.Look(ref prisonerthrowingdone, "prisonerthrowingdone");
        }

        public override void CompTick()
        {
            base.CompTick();
            // Manually tick things in the inner container
            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                var thing = innerContainer[i];
                if (thing.Destroyed)
                {
                    innerContainer.RemoveAt(i);
                    continue;
                }

                if (thing is Pawn pawn)
                {
                    // Pawns always tick normally
                    PawnTickMethod.Invoke(pawn, null);
                }
                else if (thing is ThingWithComps twc)
                {
                    if (twc.def.tickerType == TickerType.Normal)
                    {
                        ThingWithCompsTickMethod.Invoke(twc, null);
                    }
                    else if (twc.def.tickerType == TickerType.Rare && parent.IsHashIntervalTick(250))
                    {
                        ThingWithCompsTickRareMethod.Invoke(twc, null);
                    }
                    else if (twc.def.tickerType == TickerType.Long && parent.IsHashIntervalTick(2000))
                    {
                        ThingWithCompsTickLongMethod.Invoke(twc, null);
                    }
                }
                else
                {
                    // Simple things without comps
                     if (thing.def.tickerType == TickerType.Normal)
                    {
                        EntityTickMethod.Invoke(thing, null);
                    }
                    else if (thing.def.tickerType == TickerType.Rare && parent.IsHashIntervalTick(250))
                    {
                        EntityTickRareMethod.Invoke(thing, null);
                    }
                    else if (thing.def.tickerType == TickerType.Long && parent.IsHashIntervalTick(2000))
                    {
                        EntityTickLongMethod.Invoke(thing, null);
                    }
                }
            }
            
            if (parent.IsHashIntervalTick(60))
            {
                var refuelable = parent.GetComp<CompFilteredRefuelable>();
                var hasFuel = refuelable != null && refuelable.HasFuel;
                var restEffectiveness = Props.restEffectiveness;
                
                foreach (var thing in innerContainer)
                {
                    if (thing is not Pawn pawn || pawn.Dead || pawn.needs == null)
                    {
                        continue;
                    }

                    // Обработка сытости
                    if (pawn.needs.food != null)
                    {
                        if (hasFuel)
                        {
                            // Постепенно увеличиваем сытость до 0.5 (половина), но не выше
                            if (pawn.needs.food.CurLevel < 0.5f)
                            {
                                pawn.needs.food.CurLevel = Mathf.Min(0.5f, pawn.needs.food.CurLevel + 0.01f);
                            }
                        }
                        else
                        {
                            // Без топлива сытость медленно падает
                            pawn.needs.food.CurLevel *= 0.99f;
                        }
                    }

                    // Обработка отдыха
                    if (pawn.needs.rest != null && hasFuel && restEffectiveness > 0f)
                    {
                        // Восстанавливаем отдых с учетом эффективности ямы
                        var restGain = 0.001f * restEffectiveness;
                        pawn.needs.rest.CurLevel = Mathf.Min(1f, pawn.needs.rest.CurLevel + restGain);
                    }
                }
            }

            if (!parent.IsHashIntervalTick(60) || !parent.Spawned || !LoadingInProgressOrReadyToLaunch ||
                !AnyInGroupHasAnythingLeftToLoad || AnyInGroupNotifiedCantLoadMore || AnyPawnCanLoadAnythingNow)
            {
                return;
            }

            Messages.Message("PD_FinishedLoadingPit".Translate(), parent, MessageTypeDefOf.CautionInput);
            leftToLoad.Clear();
            prisonerthrowingdone = false;
        }

        public List<CompPit> TransportersInGroup(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return null;
            }

            PitUtility.GetTransportersInGroup(groupID, map, tmpTransportersInGroup);
            return tmpTransportersInGroup;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            if (!prisonerthrowingdone)
            {
                var loadGroup = new Command_LoadPit();
                var selectedTransportersCount = 0;
                for (var i = 0; i < Find.Selector.NumSelected; i++)
                {
                    if (Find.Selector.SelectedObjectsListForReading[i] is not Thing thing || thing.def != parent.def)
                    {
                        continue;
                    }

                    selectedTransportersCount++;
                }

                loadGroup.defaultLabel = "PD_ThrowPrisoner".Translate();
                loadGroup.defaultDesc = "PD_ThrowPrisonerDesc".Translate();
                loadGroup.icon = LoadCommandTex;
                loadGroup.transComp = this;
                yield return loadGroup;
            }

            yield return new Command_Action
            {
                action = delegate
                {
                    Messages.Message("PD_ConsecratingCthulhu".Translate(), parent, MessageTypeDefOf.CautionInput);
                    buildingGod = "cthulhu";
                    DoSmokePuff();
                    var unused = parent.Graphic;
                },
                defaultLabel = "PD_ConsecrateCthulhu".Translate(),
                defaultDesc = "PD_ConsecrateCthulhuDesc".Translate(),
                icon = TentacledAspectTex
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    Messages.Message("PD_ConsecratingBast".Translate(), parent, MessageTypeDefOf.CautionInput);
                    buildingGod = "bast";
                    DoSmokePuff();
                    var unused = parent.Graphic;
                },
                defaultLabel = "PD_ConsecrateBast".Translate(),
                defaultDesc = "PD_ConsecrateBastDesc".Translate(),
                icon = CatAspectTex
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    Messages.Message("PD_ConsecratingBones".Translate(), parent, MessageTypeDefOf.CautionInput);
                    buildingGod = "bones";
                    DoSmokePuff();
                    var unused = parent.Graphic;
                },
                defaultLabel = "PD_ConsecrateBones".Translate(),
                defaultDesc = "PD_ConsecrateBonesDesc".Translate(),
                icon = BoneAspectTex
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    Messages.Message("PD_UnConsecrating".Translate(), parent, MessageTypeDefOf.CautionInput);
                    buildingGod = "none";
                    DoSmokePuff();
                    var unused = parent.Graphic;
                },
                defaultLabel = "PD_UnConsecrate".Translate(),
                defaultDesc = "PD_UnConsecrateDesc".Translate(),
                icon = NormalAspectTex
            };
        }

        public void DoSmokePuff()
        {
            var position = parent.Position;
            var map = parent.Map;
            var radius = 1.5f;
            var smoke = DamageDefOf.Smoke;
           
            GenExplosion.DoExplosion(position, map, radius, smoke, null, -1, -1f, null, null, null, null,
                null,0,1,GasType.BlindSmoke);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            base.PostDeSpawn(map, mode);
            if (CancelLoad(map))
            {
                Messages.Message("PD_ThrowPrisonerCancelled".Translate(), MessageTypeDefOf.NegativeEvent);
            }

            innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
        }

        public override string CompInspectStringExtra()
        {
            return "PD_PrisonersInside".Translate() + ": " + innerContainer.ContentsString.CapitalizeFirst();
        }

        public void AddToTheToLoadList(TransferableOneWay t, int count)
        {
            if (!t.HasAnyThing || t.CountToTransfer <= 0)
            {
                return;
            }

            if (leftToLoad == null)
            {
                leftToLoad = new List<TransferableOneWay>();
            }

            if (TransferableUtility.TransferableMatching(t.AnyThing, leftToLoad,
                    TransferAsOneMode.PodsOrCaravanPacking) != null)
            {
                Log.Error("Transferable already exists.");
                return;
            }

            var transferableOneWay = new TransferableOneWay();
            leftToLoad.Add(transferableOneWay);
            transferableOneWay.things.AddRange(t.things);
            transferableOneWay.AdjustTo(count);
        }

        public void Notify_ThingAdded(Thing t)
        {
            SubtractFromToLoadList(t, t.stackCount);
        }

        public void Notify_ThingAddedAndMergedWith(Thing t, int mergedCount)
        {
            SubtractFromToLoadList(t, mergedCount);
        }

        public bool CancelLoad()
        {
            return CancelLoad(Map);
        }

        public bool CancelLoad(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return false;
            }

            TryRemoveLord(map);
            var list = TransportersInGroup(map);
            foreach (var pit in list)
            {
                pit.CleanUpLoadingVars(map);
            }

            CleanUpLoadingVars(map);
            return true;
        }

        public void TryRemoveLord(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return;
            }

            var lord = PitUtility.FindLord(groupID, map);
            if (lord != null)
            {
                map.lordManager.RemoveLord(lord);
            }
        }

        public void CleanUpLoadingVars(Map map)
        {
            groupID = -1;
            innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
            if (leftToLoad != null)
            {
                leftToLoad.Clear();
            }
        }

        private void SubtractFromToLoadList(Thing t, int count)
        {
            if (leftToLoad == null)
            {
                return;
            }

            var transferableOneWay =
                TransferableUtility.TransferableMatchingDesperate(t, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                return;
            }

            transferableOneWay.AdjustBy(-count);
            if (transferableOneWay.CountToTransfer <= 0)
            {
                leftToLoad.Remove(transferableOneWay);
            }

            if (AnyInGroupHasAnythingLeftToLoad)
            {
                return;
            }

            Messages.Message("PD_FinishedLoadingPit".Translate(), parent, MessageTypeDefOf.TaskCompletion);
            prisonerthrowingdone = false;
        }

        private void SelectPreviousInGroup()
        {
            var list = TransportersInGroup(Map);
            var num = list.IndexOf(this);
            CameraJumper.TryJumpAndSelect(list[GenMath.PositiveMod(num - 1, list.Count)].parent);
        }

        private void SelectAllInGroup()
        {
            var list = TransportersInGroup(Map);
            var selector = Find.Selector;
            selector.ClearSelection();
            foreach (var pit in list)
            {
                selector.Select(pit.parent);
            }
        }

        private void SelectNextInGroup()
        {
            var list = TransportersInGroup(Map);
            var num = list.IndexOf(this);
            CameraJumper.TryJumpAndSelect(list[(num + 1) % list.Count].parent);
        }
    }
}