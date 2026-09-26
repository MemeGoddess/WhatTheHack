using Verse;

namespace WhatTheHack
{
    public class Record : IExposable
    {
        public bool isException;
        public bool isSelected;
        public string label = "";

        public Record()
        {
        }

        public Record(bool isSelected, bool isException, string label)
        {
            this.isException = isException;
            this.isSelected = isSelected;
            this.label = label;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref isSelected, "isSelected", false);
            Scribe_Values.Look(ref isException, "isException", false);
            Scribe_Values.Look(ref label, "label", "");
        }

        public override string ToString()
        {
            return $"{isSelected},{isException},{label}";
        }
    }
}
