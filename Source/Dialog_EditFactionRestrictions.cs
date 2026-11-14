// Dialog_EditFactionRestrictions.cs
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WhatTheHack
{
    // Dialog window:
    public class Dialog_EditFactionRestrictions : Window
    {
        private const float TabWidth = 200f;
        private const float TopHeaderHeight = 28f;
        private const float RowHeight = 22f;
        private const float RowMultiHeight = 44f;
        private const float ColumnGap = 10f;
        private const float BottomRectHeight = 44f;
        private const float scrollBarWidth = 20f;

        private readonly Dict2DRecordHandler handler;
        private readonly List<ThingDef> allMechs;
        private readonly List<string> allFactionNames;
        private readonly Func<string> getTab;
        private readonly Action<string> setTab;

        private string currentTab;
        private Vector2 tabsScroll;
        private Vector2 contentScroll;
        private Dictionary<string, Record> workingSelection;

        public override Vector2 InitialSize => new Vector2(900f, 560f);

        // Clipboard support
        private static Dictionary<string, Record> clipboardData;
        private static string clipboardSourceFaction;
        
        private Dictionary<string, bool> MultilineCache = new();


        public Dialog_EditFactionRestrictions(
            Dict2DRecordHandler handler,
            List<ThingDef> allMechs,
            List<string> allFactionNames,
            Func<string> getTab,
            Action<string> setTab)
        {
            this.handler = handler ?? new Dict2DRecordHandler();
            this.allMechs = allMechs ?? new List<ThingDef>();
            this.allFactionNames = allFactionNames ?? new List<string>();
            this.getTab = getTab ?? (() => (this.allFactionNames.Count > 0) ? this.allFactionNames[0] : "");
            this.setTab = setTab ?? (s => { });

            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseX = true;

            currentTab = this.getTab();
            if (string.IsNullOrEmpty(currentTab) && this.allFactionNames.Count > 0)
                currentTab = this.allFactionNames[0];

            EnsureHandlerDefaults();
            CloneWorkingSelection();
        }

        private void EnsureHandlerDefaults()
        {
            // ensure top-level InnerList exists and contains all expected faction keys
            if (handler.InnerList == null) handler.InnerList = new Dictionary<string, Dictionary<string, Record>>();
            foreach (var name in allFactionNames)
            {
                handler.GetOrCreateInner(name);
            }
        }

        private void CloneWorkingSelection()
        {
            var selection = handler.GetOrCreateInner(currentTab);
            GUIDrawUtility.FilterSelection(ref selection, allMechs, currentTab);

            workingSelection = new Dictionary<string, Record>(selection.Count);
            foreach (var kv in selection)
            {
                var v = kv.Value;
                workingSelection[kv.Key] = new Record(v.isSelected, v.isException, v.label);
            }
        }

        public override void DoWindowContents(Rect content)
        {
            var tabsHeight = content.height - BottomRectHeight - 12f;
            var tabsRect = new Rect(content.x, 0, TabWidth, tabsHeight);
            var rightRect = new Rect(content.x + TabWidth + ColumnGap, 0, content.width - TabWidth - ColumnGap, tabsHeight);

            DrawTabs(tabsRect);
            DrawSelectionArea(rightRect);

            // Bottom buttons
            var bottomRect = new Rect(content.x, content.yMax - BottomRectHeight, content.width, BottomRectHeight);
            DrawBottomButtons(bottomRect);
        }

        private void DrawTabs(Rect tabsRect)
        {
            Widgets.DrawMenuSection(tabsRect);
            var inner = tabsRect.ContractedBy(6f);
            float viewHeight = Math.Max(allFactionNames.Count * 34f, inner.height);
            var scrollViewRect = new Rect(0, 0, inner.width - 16f, viewHeight);

            GUI.BeginGroup(inner);
            tabsScroll = GUI.BeginScrollView(new Rect(0, 0, inner.width, inner.height), tabsScroll, scrollViewRect);

            float y = 0f;
            for (int i = 0; i < allFactionNames.Count; i++)
            {
                string fname = allFactionNames[i];
                var bRect = new Rect(0, y, scrollViewRect.width, 30f);

                bool isSelected = fname == currentTab;
                if (isSelected)
                {
                    var save = GUI.color;
                    GUI.color = new Color(0.5f, 1f, 0.5f, 1f);
                    GUI.DrawTexture(bRect, TexUI.FastFillTex);
                    GUI.color = save;
                }

                if (Widgets.ButtonText(bRect, fname))
                {
                    SaveCurrentTab();       // <-- save current tab before switching
                    currentTab = fname;
                    CloneWorkingSelection();
                }

                y += 34f;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawSelectionArea(Rect rightRect)
        {
            Widgets.DrawMenuSection(rightRect);
            var inner = rightRect.ContractedBy(6f);

            // left/right columns within selection area
            float columnWidth = (inner.width - scrollBarWidth) / 2f - ColumnGap ;
            var leftColRect = new Rect(inner.x, inner.y, columnWidth, inner.height);
            var rightColRect = new Rect(inner.x + columnWidth + ColumnGap, inner.y, columnWidth, inner.height);

            if (workingSelection == null)
                CloneWorkingSelection();

            var ordered = workingSelection.OrderBy(kv => kv.Value.label).ToList();
            var leftList = ordered.Where(kv => kv.Value.isSelected).ToList();
            var rightList = ordered.Where(kv => !kv.Value.isSelected).ToList();

            float leftHeight = leftList.Sum(kv => (MultilineCache.GetValueOrDefault(kv.Value.label, false) ? RowMultiHeight : RowHeight) + 4f);
            float rightHeight = rightList.Sum(kv => (MultilineCache.GetValueOrDefault(kv.Value.label, false) ? RowMultiHeight : RowHeight) + 4f);
            float viewHeight = Math.Max(leftHeight, rightHeight) + 8f;

            // column headers
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(leftColRect.x + 4f, leftColRect.y, leftColRect.width - 8f, 22f), "WTH_FactionRestrictions_OK".Translate());
            Widgets.Label(new Rect(rightColRect.x + 4f, rightColRect.y, rightColRect.width - 8f, 22f), "WTH_FactionRestrictions_NOK".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // scroll view covering both columns so they scroll together
            GUI.BeginGroup(inner);
            contentScroll = GUI.BeginScrollView(new Rect(0, 26f, inner.width, inner.height - 26f), contentScroll, new Rect(0, 0, inner.width - 16f, viewHeight));

            float yLeft = 0f;
            float yRight = 0f;
            float xLeft = 0f;
            float xRight = columnWidth + ColumnGap;

            // draw left (selected) items
            for (int i = 0; i < leftList.Count; i++)
            {
                var kv = leftList[i];
                if (!MultilineCache.TryGetValue(kv.Value.label, out var isMulti))
                {
                    isMulti = Text.CalcSize(kv.Value.label).x > columnWidth;
                    MultilineCache[kv.Value.label] = isMulti;
                }
                var h = isMulti ? RowMultiHeight : RowHeight;
                var rowRect = new Rect(xLeft, yLeft, columnWidth, h);
                DrawItemRow(rowRect, kv.Key, kv.Value);
                yLeft += h + 4f;
            }

            // draw right (not selected)
            for (int i = 0; i < rightList.Count; i++)
            {
                var kv = rightList[i];
                if (!MultilineCache.TryGetValue(kv.Value.label, out var isMulti))
                {
                    isMulti = Text.CalcSize(kv.Value.label).x > columnWidth;
                    MultilineCache[kv.Value.label] = isMulti;
                }
                var h = isMulti ? RowMultiHeight : RowHeight;
                var rowRect = new Rect(xRight, yRight, columnWidth, h);
                DrawItemRow(rowRect, kv.Key, kv.Value);
                yRight += h + 4f;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawItemRow(Rect rowRect, string key, Record rec)
        {
            // background
            var save = GUI.color;
            if (Mouse.IsOver(rowRect))
                Widgets.DrawHighlightSelected(rowRect);
            else
            {
                GUI.color = rec.isSelected ? new Color(0f, 0.5f, 0f, 0.08f) : new Color(0.5f, 0f, 0f, 0.06f);
                GUI.DrawTexture(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), TexUI.FastFillTex);
                GUI.color = save;
            }
            
            // label
            var labelRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, rowRect.width - 12f - 80f, rowRect.height - 2f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, !rec.label.NullOrEmpty() ? rec.label : key);
            Text.Anchor = TextAnchor.UpperLeft;

            // exception checkbox
            Widgets.Checkbox(rowRect.x + rowRect.width - rowRect.height, rowRect.y, ref rec.isException, size: rowRect.height);

            // clickable whole tile toggles selection and flips exception (mimic original)
            var worldRect = new Rect(rowRect.x + windowRect.x, rowRect.y + windowRect.y, rowRect.width - 40f, rowRect.height);
            if (Widgets.ButtonInvisible(worldRect))
            {
                rec.isSelected = !rec.isSelected;
                rec.isException = !rec.isSelected; // Align to isSelected
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }

            // small selected-toggle button for convenience
            var toggleRect = new Rect(rowRect.x + rowRect.width - 6f - 40f, rowRect.y + (rowRect.height - 20f) / 2f, 20f, 20f);
            if (Widgets.ButtonText(toggleRect, rec.isSelected ? ">" : "<"))
            {
                rec.isSelected = !rec.isSelected;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }

        private void DrawBottomButtons(Rect area)
        {
            var inner = area.ContractedBy(6f);

            float btnW = 120f;
            float spacing = 10f;
            float x = inner.x;

            // Cancel
            var cancelRect = new Rect(x, inner.y, btnW, inner.height);
            if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
            {
                Close(true);
                return;
            }
            x += btnW + spacing;

            // Reset to defaults
            var resetRect = new Rect(x, inner.y, btnW + 20f, inner.height);
            if (Widgets.ButtonText(resetRect, "WTH_ResetToDefaults".Translate("Reset")))
            {
                var sel = handler.GetOrCreateInner(currentTab);
                GUIDrawUtility.FilterSelection(ref sel, allMechs, currentTab);
                workingSelection.Clear();
                foreach (var kv in sel)
                    workingSelection[kv.Key] = new Record(kv.Value.isSelected, kv.Value.isException, kv.Value.label);

                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            x += btnW + 20f + spacing;

            // --- Copy ---
            var copyRect = new Rect(x, inner.y, btnW, inner.height);
            if (Widgets.ButtonText(copyRect, "WTH_Copy".Translate()))
            {
                clipboardData = CloneSelection(workingSelection);
                clipboardSourceFaction = currentTab;
                Messages.Message("WTH_CopyRestFrom".Translate() + $" {currentTab}", MessageTypeDefOf.TaskCompletion, false);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            x += btnW + spacing;

            // --- Paste ---
            var pasteRect = new Rect(x, inner.y, btnW, inner.height);
            if (Widgets.ButtonText(pasteRect, "WTH_Paste".Translate()))
            {
                if (clipboardData != null)
                {
                    workingSelection = CloneSelection(clipboardData);
                    Messages.Message("WTH_PasteRestFrom".Translate() + $" {clipboardSourceFaction ?? "WTH_Clipboard".Translate()} " + "WTH_To".Translate() + $" {currentTab}", MessageTypeDefOf.TaskCompletion, false);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                else
                {
                    Messages.Message("WTH_ClipboardEmpty".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            x += btnW + spacing;

            // --- Copy From dropdown ---
            var copyFromRect = new Rect(x, inner.y, btnW + 30f, inner.height);
            if (Widgets.ButtonText(copyFromRect, "WTH_CopyFrom".Translate()))
            {
                var options = new List<FloatMenuOption>();
                foreach (var fac in allFactionNames)
                {
                    if (fac == currentTab) continue;
                    options.Add(new FloatMenuOption(fac, () =>
                    {
                        var source = handler.GetOrCreateInner(fac);
                        workingSelection = CloneSelection(source);
                        Messages.Message("WTH_CopyRestFrom".Translate() + $" {fac} " + "WTH_To".Translate() + $" {currentTab}", MessageTypeDefOf.TaskCompletion, false);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }));
                }

                if (options.Count > 0)
                    Find.WindowStack.Add(new FloatMenu(options));
                else
                    Messages.Message("WTH_NoOtherFactions".Translate(), MessageTypeDefOf.RejectInput, false);
            }
            x += btnW + 30f + spacing;

            // --- OK / Save ---
            var saveRect = new Rect(inner.xMax - btnW, inner.y, btnW, inner.height);
            if (Widgets.ButtonText(saveRect, "OK".Translate()))
            {
                //handler.InnerList[currentTab] = workingSelection.ToDictionary(kv => kv.Key, kv => kv.Value);
                SaveCurrentTab();
                setTab(currentTab);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(true);
            }
        }

        private static Dictionary<string, Record> CloneSelection(Dictionary<string, Record> source)
        {
            var clone = new Dictionary<string, Record>(source.Count);
            foreach (var kv in source)
            {
                var v = kv.Value;
                clone[kv.Key] = new Record(v.isSelected, v.isException, v.label);
            }
            return clone;
        }

        private void SaveCurrentTab()
        {
            if (!string.IsNullOrEmpty(currentTab) && workingSelection != null)
            {
                handler.InnerList[currentTab] = workingSelection.ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }


    }
}
