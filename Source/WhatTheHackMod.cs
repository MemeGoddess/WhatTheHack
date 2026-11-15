// WhatTheHackMod.cs (replace existing DrawFactionRestrictionsUI + Dialog implementation)
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace WhatTheHack
{
    public class WhatTheHackMod : Mod
    {
        public static WhatTheHackSettings Settings;
        private WhatTheHackSettingsTabs tabs;
        public WhatTheHackMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WhatTheHackSettings>();
        }

        public override string SettingsCategory() => "WTH_ModName".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            tabs ??= new WhatTheHackSettingsTabs(Settings);
            tabs.DoWindowContents(inRect);
        }
    }


}
