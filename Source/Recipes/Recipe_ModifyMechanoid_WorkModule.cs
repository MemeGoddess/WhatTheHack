using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WhatTheHack.Recipes
{
    internal class Recipe_ModifyMechanoid_WorkModule : Recipe_ModifyMechanoid
    {
        protected override void PostSuccessfulApply(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.PostSuccessfulApply(pawn, part, billDoer, ingredients, bill);

            if (pawn == null)
            {
                Log.Error("[WhatTheHack] PostSuccessfulApply: Pawn is null.");
                return;
            }

            var extendedDataStorage = Base.Instance?.GetExtendedDataStorage();
            if (extendedDataStorage == null)
            {
                Log.Error("[WhatTheHack] PostSuccessfulApply: ExtendedDataStorage is null.");
                return;
            }

            var pawnData = extendedDataStorage.GetExtendedDataFor(pawn);
            if (pawnData == null)
            {
                Log.Error($"[WhatTheHack] PostSuccessfulApply: Extended data for pawn {pawn.LabelShort ?? pawn.ToStringSafe()} is null.");
                return;
            }

            if (pawnData.workTypes == null)
            {
                pawnData.workTypes = new List<WorkTypeDef>();
            }

            if (bill?.recipe?.addsHediff == null)
            {
                Log.Error("[WhatTheHack] PostSuccessfulApply: Recipe or addsHediff is null.");
                return;
            }

            var modExt = bill.recipe.addsHediff.GetModExtension<DefModExtension_Hediff_WorkModule>();
            if (modExt == null)
            {
                Log.Error($"[WhatTheHack] PostSuccessfulApply: Missing DefModExtension_Hediff_WorkModule for hediff {bill.recipe.addsHediff.defName}.");
                return;
            }

            if (modExt.workTypes == null || modExt.workTypes.Count == 0)
            {
                Log.Warning($"[WhatTheHack] PostSuccessfulApply: No work types defined in {bill.recipe.addsHediff.defName}.");
                return;
            }

            // Add or update work modules
            foreach (var workType in modExt.workTypes)
            {
                if (!pawnData.workTypes.Contains(workType))
                    pawnData.workTypes.Add(workType);

                if (pawn.workSettings != null)
                {
                    // ensure work type is enabled
                    if (!pawn.workSettings.EverWork)
                        pawn.workSettings.EnableAndInitialize();

                    if (!pawn.workSettings.WorkIsActive(workType))
                        pawn.workSettings.SetPriority(workType, 3);
                    
                    if(workType != WorkTypeDefOf.Hunting)
                        pawnData.canWorkNow = true;
                }

                if (modExt.skillLevel > 0 && pawn.skills != null)
                {
                    foreach (var skillDef in workType.relevantSkills)
                    {
                        var skill = pawn.skills.GetSkill(skillDef);
                        if (skill != null)
                        {
                            skill.Level = modExt.skillLevel;
                            skill.Notify_SkillDisablesChanged();
                        }
                        else
                        {
                            Log.Warning($"[WhatTheHack] Skill {skillDef.defName} missing on {pawn.LabelShort ?? pawn.ToStringSafe()}.");
                        }
                    }
                }
            }

            Log.Message($"[WhatTheHack] Applied work module to {pawn.LabelShort}, unlocked {modExt.workTypes.Count} work types.");
        }
    }
}
