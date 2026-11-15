using RimWorld;
using Verse;

namespace WhatTheHack.Tests;

public static class Utils
{
    public static Zone_Stockpile CreateStockpile(
        Map map,
        IntVec3[] cells,
        StorageSettingsPreset preset = StorageSettingsPreset.DefaultStockpile)
    {
        var stockpile = new Zone_Stockpile(preset, map.zoneManager);

        map.zoneManager.RegisterZone(stockpile);

        foreach (var cell in cells)
        {
            if (!cell.InBounds(map))
                continue;

            if (map.zoneManager.ZoneAt(cell) != null)
                continue;

            if (!Designator_ZoneAdd.IsZoneableCell(cell, map))
                continue;

            stockpile.AddCell(cell);
        }

        if (stockpile.CellCount != 0) 
            return stockpile;
        
        stockpile.Delete(playSound: false);
        return null!;
    }
}