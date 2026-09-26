using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WhatTheHack.Storage;
using RimWorld;

namespace WhatTheHack
{
    public class WorldComponent_WhatTheHack : WorldComponent
    {
        public ExtendedDataStorage extendedDataStorage;


        public static Dictionary<string, Texture2D> CancelControlMechTextures = new();
        public static Dictionary<string, Texture2D> CancelControlTurretTextures = new();

        public WorldComponent_WhatTheHack(World world) : base(world)
        {
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            extendedDataStorage = world.GetComponent<ExtendedDataStorage>();
            if (extendedDataStorage != null)
                extendedDataStorage.Cleanup();

            LongEventHandler.ExecuteWhenFinished(TryGenerateCancelTextures);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // If ExtendedDataStorage is a serializable WorldComponent, it will save itself; otherwise save references here
        }

        private void TryGenerateCancelTextures()
        {
            if (CancelControlMechTextures.Count == 0)
            {
                var cancelTex = ContentFinder<Texture2D>.Get("UI/Cancel").GetReadableTexture();
                var mechs = (from td in DefDatabase<ThingDef>.AllDefs where td.race is { IsMechanoid: true } select td).ToList();
                foreach (var mechDef in mechs)
                {
                    var kind = DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(pkd => pkd.race == mechDef);
                    if (kind == null) continue;

                    if (mechDef.GetConcreteExample() is not Pawn mech)
                    {
                        Log.Warning($"[WhatTheHack] Unable to get concrete example for {mechDef.defName}");
                        continue;
                    }

                    if (mech.ageTracker?.CurKindLifeStage?.bodyGraphicData?.Graphic?.MatSouth?.mainTexture is not Texture2D mechTex)
                    {
                        Log.Warning($"[WhatTheHack] Mech {mechDef.defName} has no valid texture");
                        continue;
                    }

                    CancelControlMechTextures[mech.def.defName] = mechTex.GetReadableTexture().AddWatermark(cancelTex);
                }
            }

            if (CancelControlTurretTextures.Count != 0) return;

            var cancel = ContentFinder<Texture2D>.Get("UI/Cancel").GetReadableTexture();
            var allTurrets = (from td in DefDatabase<ThingDef>.AllDefs where td.thingClass == typeof(Building_TurretGun) select td).ToList();
            foreach (var turretDef in allTurrets)
            {
                if (turretDef.uiIcon is Texture2D iconTex)
                    CancelControlTurretTextures[turretDef.defName] = iconTex.GetReadableTexture().AddWatermark(cancel);
                else Log.Warning($"[WhatTheHack] Turret {turretDef.defName} has no valid UI icon");
            }
        }
    }
}
