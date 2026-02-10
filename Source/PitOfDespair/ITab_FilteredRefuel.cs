using RimWorld;
using UnityEngine;
using Verse;

namespace PitOfDespair { 

public class ITab_FilteredRefuel : ITab
{
    private const float TopAreaHeight = 35f;

    private static readonly Vector2 WinSize = new Vector2(300f, 480f);

    private ThingFilterUI.UIState uiState;

    public ITab_FilteredRefuel()
    {
        size = WinSize;
        labelKey = "PD_FeedPrisoners";
    }

    private IStoreSettingsParent SelStoreSettingsParent =>
        ((ThingWithComps)SelObject).GetComp<CompFilteredRefuelable>();

    public override bool IsVisible => SelStoreSettingsParent.StorageTabVisible;

    protected override void FillTab()
    {
        var selStoreSettingsParent = SelStoreSettingsParent;
        var storeSettings = selStoreSettingsParent.GetStoreSettings();
        var rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
        GUI.BeginGroup(rect);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        var rect2 = new Rect(rect)
        {
            height = 32f
        };
        Widgets.Label(rect2, "PD_FeedPrisonersLabel".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        ThingFilter thingFilter = null;
        if (selStoreSettingsParent.GetParentStoreSettings() != null)
        {
            thingFilter = selStoreSettingsParent.GetParentStoreSettings().filter;
        }

        var rect3 = new Rect(0f, 40f, rect.width, rect.height - 40f);
        if (uiState == default)
        {
            uiState = new ThingFilterUI.UIState();
        }

        ThingFilterUI.DoThingFilterConfigWindow(rect3, uiState, storeSettings.filter, thingFilter, 8);
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.StorageTab, KnowledgeAmount.FrameDisplayed);
        GUI.EndGroup();
    }
} }