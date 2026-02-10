using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Random = System.Random;

namespace PitOfDespair{

public class ITab_PitContents : ITab
{
    private const float TopPadding = 20f;

    private const float SpaceBetweenItemsLists = 10f;

    private const float ThingRowHeight = 28f;

    private const float ThingIconSize = 28f;

    private const float ThingLeftX = 36f;

    private static readonly List<Thing> tmpSingleThing = new List<Thing>();

    private static readonly Color ThingLabelColor = ITab_Pawn_Gear.ThingLabelColor;

    private static readonly Color ThingHighlightColor = ITab_Pawn_Gear.HighlightColor;

    private readonly List<Thing> thingsToSelect = new List<Thing>();

    private float lastDrawnHeight;
    private Vector2 scrollPosition;

    public ITab_PitContents()
    {
        size = new Vector2(520f, 450f);
        labelKey = "PD_TabPrisonerContents";
    }

    public CompPit Transporter => SelThing.TryGetComp<CompPit>();

    public override bool IsVisible => Transporter != null &&
                                      (Transporter.LoadingInProgressOrReadyToLaunch || Transporter.innerContainer.Any);

    protected override void FillTab()
    {
        thingsToSelect.Clear();
        var outRect = new Rect(default, size).ContractedBy(10f);
        outRect.yMin += 20f;
        var rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastDrawnHeight, outRect.height));
        Widgets.BeginScrollView(outRect, ref scrollPosition, rect);
        var curY = 0f;
        DoItemsLists(rect, ref curY);
        lastDrawnHeight = curY;
        Widgets.EndScrollView();
        if (!thingsToSelect.Any())
        {
            return;
        }

        ITab_Pawn_FormingCaravan.SelectNow(thingsToSelect);
        thingsToSelect.Clear();
    }

    private void DoItemsLists(Rect inRect, ref float curY)
    {
        var transporter = Transporter;
        var rect = new Rect(0f, curY, (inRect.width - 10f) / 2f, inRect.height);
        var a = 0f;
        var position = new Rect(0f, curY, inRect.width - 10f, inRect.height);
        var curY2 = 0f;
        GUI.BeginGroup(position);
        Widgets.ListSeparator(ref curY2, position.width, "PD_PrisonersInThePit".Translate());
        var hasThings = false;
        foreach (var item in transporter.innerContainer)
        {
            hasThings = true;
            tmpSingleThing.Clear();
            tmpSingleThing.Add(item);
            var t1 = item;
            DoThingRow(item.def, item.stackCount, tmpSingleThing, position.width, ref curY2, delegate (int x)
            {
                GenDrop.TryDropSpawn(t1.SplitOff(x), SelThing.Position, SelThing.Map, ThingPlaceMode.Near, out _);
                var pawn = t1 as Pawn;
                var random = new Random();
                if (pawn == null)
                {
                    return;
                }

                pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PD_ThrownIntoPit"));
                HealthUtility.DamageUntilDowned(pawn, false);
                var brain = pawn.health.hediffSet.GetBrain();
                var num = random.NextDouble();
                switch (num)
                {
                    case > 0.75 and < 0.85:
                        {
                            var hediff = HediffMaker.MakeHediff(HediffDefOf.Dementia, pawn, brain);
                            if (!pawn.health.WouldDieAfterAddingHediff(hediff))
                            {
                                pawn.health.AddHediff(hediff);
                            }

                            break;
                        }
                    case > 0.85 and < 0.95:
                        {
                            var hediff2 = HediffMaker.MakeHediff(HediffDef.Named("PD_Psychosis"), pawn, brain);
                            if (!pawn.health.WouldDieAfterAddingHediff(hediff2))
                            {
                                pawn.health.AddHediff(hediff2);
                            }

                            break;
                        }
                    case > 0.95:
                        {
                            var hediff3 = HediffMaker.MakeHediff(HediffDef.Named("PD_BrainEatingParasites"), pawn, brain);
                            if (!pawn.health.WouldDieAfterAddingHediff(hediff3))
                            {
                                pawn.health.AddHediff(hediff3);
                            }

                            break;
                        }
                }
            });
            tmpSingleThing.Clear();
        }

        if (!hasThings)
        {
            Widgets.NoneLabel(ref curY2, rect.width);
        }

        GUI.EndGroup();
        curY += Mathf.Max(a, curY2);
    }

    private void SelectLater(List<Thing> things)
    {
        thingsToSelect.Clear();
        thingsToSelect.AddRange(things);
    }

    private void DoThingRow(ThingDef thingDef, int count, List<Thing> things, float width, ref float curY,
        Action<int> discardAction)
    {
        var rect = new Rect(0f, curY, width, 28f);
        if (count != 1)
        {
            var butRect = new Rect(rect.x + rect.width - 24f, rect.y + ((rect.height - 24f) / 2f), 24f, 24f);
            if (Widgets.ButtonImage(butRect, CaravanThingsTabUtility.AbandonSpecificCountButtonTex))
            {
                Find.WindowStack.Add(new Dialog_Slider("RemoveSliderText".Translate(thingDef.label), 1, count,
                    discardAction));
            }
        }

        rect.width -= 24f;
        var butRect2 = new Rect(rect.x + rect.width - 24f, rect.y + ((rect.height - 24f) / 2f), 24f, 24f);
        if (Widgets.ButtonImage(butRect2, CaravanThingsTabUtility.AbandonButtonTex))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "ConfirmRemoveItemDialog".Translate(thingDef.label), delegate { discardAction(count); }));
        }

        rect.width -= 24f;
        if (things.Count == 1)
        {
            Widgets.InfoCardButton(rect.width - 24f, curY, things[0]);
        }
        else
        {
            Widgets.InfoCardButton(rect.width - 24f, curY, thingDef);
        }

        rect.width -= 24f;
        if (Mouse.IsOver(rect))
        {
            GUI.color = ThingHighlightColor;
            GUI.DrawTexture(rect, TexUI.HighlightTex);
        }

        if (thingDef.DrawMatSingle != null && thingDef.DrawMatSingle.mainTexture != null)
        {
            var rect2 = new Rect(4f, curY, 28f, 28f);
            if (things.Count == 1)
            {
                Widgets.ThingIcon(rect2, things[0]);
            }
            else
            {
                Widgets.ThingIcon(rect2, thingDef);
            }
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = ThingLabelColor;
        var rect3 = new Rect(36f, curY, rect.width - 36f, rect.height);
        var text = things.Count != 1
            ? GenLabel.ThingLabel(thingDef, null, count).CapitalizeFirst()
            : things[0].LabelCap;
        Text.WordWrap = false;
        Widgets.Label(rect3, text.Truncate(rect3.width));
        Text.WordWrap = true;
        Text.Anchor = TextAnchor.UpperLeft;
        TooltipHandler.TipRegion(rect, text);
        if (Widgets.ButtonInvisible(rect, false))
        {
            SelectLater(things);
        }

        if (Mouse.IsOver(rect))
        {
            foreach (var target in things)
            {
                TargetHighlighter.Highlight(target);
            }
        }

        curY += 28f;
    }

    private void EndJobForEveryoneHauling(TransferableOneWay t)
    {
        var allPawnsSpawned = SelThing.Map.mapPawns.AllPawnsSpawned;
        foreach (var pawn in allPawnsSpawned)
        {
            if (pawn.CurJobDef != DefDatabase<JobDef>.GetNamed("PD_HaulToPit"))
            {
                continue;
            }

            var jobDriver_HaulToPit = (JobDriver_HaulToPit)pawn.jobs.curDriver;
            if (jobDriver_HaulToPit.Transporter == Transporter && jobDriver_HaulToPit.ThingToCarry != null &&
                jobDriver_HaulToPit.ThingToCarry.def == t.ThingDef)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
} }