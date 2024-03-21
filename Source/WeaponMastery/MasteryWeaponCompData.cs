using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WeaponMastery;

public class MasteryWeaponCompData : MasteryCompData
{
    private Pawn pawn;

    public MasteryWeaponCompData(Pawn pawn)
    {
        this.pawn = pawn;
        bonusStats = new Dictionary<StatDef, float>();
    }

    public override void ExposeData()
    {
        switch (Scribe.mode)
        {
            case LoadSaveMode.Saving:
            {
                var list = bonusStats.ToList();
                var list2 = new List<float>();
                var list3 = new List<StatDef>();
                foreach (var item in list)
                {
                    list2.Add(item.Value);
                    list3.Add(item.Key);
                }

                Scribe_Collections.Look(ref list2, "floats", LookMode.Value);
                Scribe_Collections.Look(ref list3, "defs", LookMode.Def);
                break;
            }
            case LoadSaveMode.LoadingVars:
            {
                var list4 = new List<float>();
                var list5 = new List<StatDef>();
                bonusStats = new Dictionary<StatDef, float>();
                Scribe_Collections.Look(ref list4, "floats", LookMode.Value);
                Scribe_Collections.Look(ref list5, "defs", LookMode.Def);
                for (var i = 0; i < list5.Count; i++)
                {
                    if (list5[i] != null)
                    {
                        bonusStats.Add(list5[i], list4[i]);
                    }
                }

                break;
            }
        }

        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref masteryLevel, "masterylevel");
        Scribe_Values.Look(ref experience, "experience");
    }
}