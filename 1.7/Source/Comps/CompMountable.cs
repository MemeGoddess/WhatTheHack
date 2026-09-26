using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WhatTheHack.Comps
{
    public class CompMountable : ThingComp
    {
        // persisted
        public float drawOffset;
        public Pawn mountedTo;

        // cached, not saved
        [Unsaved] public Vector3? overrideDrawPos;
        [Unsaved] private IntVec3 lastMountedPosition = IntVec3.Zero;
        [Unsaved] private Rot4 lastMountedRotation = Rot4.North;
        [Unsaved] private int lastUpdateTick = -9999;
        private const int UpdateFrequencyTicks = 3; // tune this if you want more/less freshness

        public Pawn MountedTo => mountedTo;
        public bool Active => mountedTo != null && !parent.Destroyed;

        public void MountToPawn(Pawn pawn)
        {
            mountedTo = pawn;
            SetPowerComp();
            Configure();
            SetDrawOffset();
            UpdateDrawPos(force: true);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Uninstall();
            base.PostDestroy(mode, previousMap);
        }

        public void Uninstall()
        {
            if (mountedTo == null)
            {
                overrideDrawPos = null;
                return;
            }

            if (mountedTo.health != null && mountedTo.health.hediffSet.HasHediff(WTH_DefOf.WTH_MountedTurret))
            {
                mountedTo.health.RemoveHediff(
                    mountedTo.health.hediffSet.GetFirstHediffOfDef(WTH_DefOf.WTH_MountedTurret));
            }

            var store = Base.Instance?.GetExtendedDataStorage();
            if (store != null)
            {
                store.GetExtendedDataFor(mountedTo).turretMount = null;
            }

            mountedTo = null;
            overrideDrawPos = null;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Active)
            {
                return;
            }

            Configure();

            // update cached draw pos occasionally (keeps hot path cheap)
            UpdateDrawPos();

            LetMountedToWaitIfReserved();

            if (mountedTo.health != null && !mountedTo.health.hediffSet.HasHediff(WTH_DefOf.WTH_TurretModule))
            {
                Uninstall();
            }
        }

        /// <summary>
        /// Recompute cached override draw position when needed.
        /// Keeps frequency limited and only updates when pawn moved/rotated or when forced.
        /// </summary>
        public void UpdateDrawPos(bool force = false)
        {
            if (mountedTo == null)
            {
                overrideDrawPos = null;
                return;
            }

            // If turret parent isn't spawned (i.e., in inventory/minified), we don't need a world draw pos.
            if (!parent.Spawned)
            {
                overrideDrawPos = null;
                return;
            }

            var ticks = Find.TickManager?.TicksGame ?? 0;
            if (!force && (ticks - lastUpdateTick) < UpdateFrequencyTicks)
                return;
            lastUpdateTick = ticks;

            if (!force && mountedTo.Position == lastMountedPosition && mountedTo.Rotation == lastMountedRotation && overrideDrawPos.HasValue)
                return;

            lastMountedPosition = mountedTo.Position;
            lastMountedRotation = mountedTo.Rotation;

            try
            {
                var pos = mountedTo.DrawPos;
                pos.z += drawOffset;
                pos.y += 1f;
                overrideDrawPos = pos;
            }
            catch (System.Exception ex)
            {
                // if something odd happens, clear the cache so default draw applies
                overrideDrawPos = null;
                Log.ErrorOnce($"CompMountable.UpdateDrawPos failed for {parent}: {ex}", 9453451);
            }
        }

        private void LetMountedToWaitIfReserved()
        {
            if (!(parent is Building_TurretGun turret))
                return;
            if (turret.Map == null || !turret.Map.reservationManager.IsReservedByAnyoneOf(turret, Faction.OfPlayer))
            {
                return;
            }

            if (mountedTo.CurJobDef == JobDefOf.Wait_Combat || mountedTo.CurJobDef == WTH_DefOf.WTH_Mechanoid_Rest)
            {
                return;
            }

            mountedTo.jobs.StartJob(new Job(JobDefOf.Wait_Combat) { expiryInterval = 10000 }, JobCondition.InterruptForced);
            mountedTo.jobs.curDriver.AddFailCondition(() =>
                !turret.Map.reservationManager.IsReservedByAnyoneOf(turret, Faction.OfPlayer));
        }

        private void Configure()
        {
            if (mountedTo == null) return;

            // clarified grouping
            if ((mountedTo.Map == null && parent.Spawned) || mountedTo.Downed ||
                (mountedTo.Spawned && OutOfBounds(mountedTo, parent)))
            {
                ToInventory();
            }
            else if (mountedTo.Map != parent.Map && mountedTo.Spawned &&
                     !OutOfBounds(mountedTo, parent.GetInnerIfMinified()))
            {
                OutOfInventory();
            }

            SetPowerComp();
            LinkToMountedTo();

            if (parent.Spawned)
            {
                parent.Position = mountedTo.Position;
            }

            if (parent.Faction != mountedTo.Faction)
            {
                parent.SetFaction(mountedTo.Faction);
            }
        }

        private void LinkToMountedTo()
        {
            if (mountedTo == null) return;
            var store = Base.Instance?.GetExtendedDataStorage();
            if (store != null)
            {
                store.GetExtendedDataFor(mountedTo).turretMount = this;
            }
        }

        private bool OutOfBounds(Pawn mountedTo, Thing parentThing)
        {
            var cellRect = GenAdj.OccupiedRect(mountedTo.Position, mountedTo.Rotation, parentThing.def.Size + new IntVec2(1, 1));
            foreach (var intVec3 in cellRect)
            {
                if (!intVec3.InBounds(mountedTo.Map))
                {
                    return true;
                }
            }
            return false;
        }

        public void OutOfInventory()
        {
            parent = (ThingWithComps)parent.SplitOff(1);
            GenSpawn.Spawn(parent, mountedTo.Position, mountedTo.Map, Rot4.North);
            UpdateDrawPos(force: true);
        }

        public void ToInventory()
        {
            parent = (ThingWithComps)parent.SplitOff(1);
            mountedTo.inventory?.innerContainer?.TryAdd(parent, 1);
            overrideDrawPos = null;
        }

        private void SetPowerComp()
        {
            var compPower = parent.GetComp<CompPowerTrader>();
            if (compPower == null || !parent.Spawned)
            {
                return;
            }

            var compFlickable = parent.GetComp<CompFlickable>();
            if (compFlickable is { SwitchIsOn: true })
            {
                compPower.PowerOn = true;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref mountedTo, "mountedTo");
            Scribe_Values.Look(ref drawOffset, "drawOffset");
            // deliberately do not persist cached fields (overrideDrawPos, lastMountedPosition, etc.)
        }

        private void SetDrawOffset()
        {
            if (mountedTo == null) return;

            var curKindLifeStage = mountedTo.ageTracker.CurKindLifeStage;
            var unreadableTexture = curKindLifeStage.bodyGraphicData.Graphic.MatEast.mainTexture as Texture2D;
            if (unreadableTexture == null) return;

            var t = unreadableTexture.GetReadableTexture();
            var backHeight = GetBackHeight(t);
            var backHeightRelative = backHeight / (float)t.height;
            var textureHeight = curKindLifeStage.bodyGraphicData.drawSize.y;

            // If animal texture does not fit in a tile, take this into account
            var extraOffset = textureHeight > 1f ? (textureHeight - 1f) / 2f : 0f;
            // Small extra offset — you don't want to draw pawn exactly on back
            extraOffset += 0.5f;
            drawOffset = (textureHeight * backHeightRelative) - extraOffset;

            UpdateDrawPos(force: true);
        }

        private static int GetBackHeight(Texture2D t)
        {
            var middle = t.width / 2;
            var backHeight = 0;
            var inBody = false;
            var threshold = 0.8f;

            for (var i = 0; i < t.height; i++)
            {
                var c = t.GetPixel(middle, i);
                if (inBody && c.a < threshold)
                {
                    backHeight = i;
                    break;
                }

                if (c.a >= threshold)
                {
                    inBody = true;
                }
            }

            return backHeight;
        }
    }
}
