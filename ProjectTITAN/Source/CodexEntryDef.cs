using System;
using System.Collections.Generic;
using Verse;

namespace ProjectTITAN
{
    public enum CodexEntryStatus
    {
        Alive,
        Deceased,
        Classified
    }

    public class CodexEntryDef : Def
    {
        public int subjectNumber;
        public string displayNumber;
        public string labelKey;
        public string descKey;
        public string hintKey;
        public string pawnKindDef;
        public string companionPawnKindDef;
        public CodexEntryStatus status = CodexEntryStatus.Alive;
        public int order;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
                yield return err;
        }
    }
}