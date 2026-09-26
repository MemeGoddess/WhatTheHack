// Dict2DRecordHandler.cs
using System;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace WhatTheHack
{
    // Note: Record must implement IExposable as well (or be a class/struct we can scribe directly).
    public class Dict2DRecordHandler : IExposable
    {
        // public for UI access
        public Dictionary<string, Dictionary<string, Record>> InnerList = new();

        public float CustomDrawerHeight;

        public void Clear()
        {
            InnerList?.Clear();
        }

        public int Count => InnerList?.Count ?? 0;

        public IEnumerable<string> Keys => InnerList?.Keys ?? Enumerable.Empty<string>();

        /// <summary>
        /// Returns the inner dictionary for a faction name, creating it if missing.
        /// Also ensures entries for allPawns can be built later via FilterSelection.
        /// </summary>
        public Dictionary<string, Record> GetOrCreateInner(string factionName)
        {
            if (InnerList == null) InnerList = new Dictionary<string, Dictionary<string, Record>>();
            if (!InnerList.ContainsKey(factionName)) InnerList[factionName] = new Dictionary<string, Record>();
            return InnerList[factionName];
        }


        public Dict2DRecordHandler()
        {
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // nothing special
            }

            // Scribe does not directly support a nested dictionary; we convert to a list wrapper for Scribe
            var outerList = new List<Dict2DRecordHandler_Entry>();
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.Inactive)
            {
                foreach (var kv in InnerList)
                {
                    var entry = new Dict2DRecordHandler_Entry { Key = kv.Key, Inner = new List<Dict2DRecordHandler_InnerEntry>() };
                    foreach (var kvInner in kv.Value)
                    {
                        entry.Inner.Add(new Dict2DRecordHandler_InnerEntry { Key = kvInner.Key, Value = kvInner.Value });
                    }

                    outerList.Add(entry);
                }
            }

            Scribe_Collections.Look(ref outerList, "FactionRestrictions_OuterList", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // rebuild dictionary from outerList
                InnerList = new Dictionary<string, Dictionary<string, Record>>();
                if (outerList != null)
                {
                    foreach (var entry in outerList)
                    {
                        var innerDict = new Dictionary<string, Record>();
                        if (entry.Inner != null)
                        {
                            foreach (var innerEntry in entry.Inner)
                            {
                                innerDict[innerEntry.Key] = innerEntry.Value ?? new Record(); // ensure non-null
                            }
                        }

                        InnerList[entry.Key] = innerDict;
                    }
                }
            }
        }
    }

    // helpers used to scribe the nested structure
    public class Dict2DRecordHandler_Entry : IExposable
    {
        public string Key;
        public List<Dict2DRecordHandler_InnerEntry> Inner;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Key, "key");
            Scribe_Collections.Look(ref Inner, "inner", LookMode.Deep);
        }
    }

    public class Dict2DRecordHandler_InnerEntry : IExposable
    {
        public string Key;
        public Record Value;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Key, "key");
            Scribe_Deep.Look(ref Value, "value");
        }
    }
}
