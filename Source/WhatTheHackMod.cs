// WhatTheHackMod.cs (replace existing DrawFactionRestrictionsUI + Dialog implementation)
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;
using static WhatTheHack.CompatibilityLoader;

namespace WhatTheHack
{
    public class WhatTheHackMod : Mod
    {
        public static WhatTheHackSettings Settings;
        private WhatTheHackSettingsTabs tabs;
        public WhatTheHackMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WhatTheHackSettings>();
            var harmony = new HarmonyLib.Harmony("memegoddess.whatthehack");
            var assembly = Assembly.GetExecutingAssembly();
            
            harmony.PatchAllUncategorized(assembly);
            
            if(ExpandedInstalled)
                harmony.PatchCategory(assembly, Expanded);
        }

        public override string SettingsCategory() => "WTH_ModName".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            tabs ??= new WhatTheHackSettingsTabs(Settings);
            tabs.DoWindowContents(inRect);
        }
    }


}
