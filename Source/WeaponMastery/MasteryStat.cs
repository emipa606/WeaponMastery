using RimWorld;
using Verse;

namespace WeaponMastery;

public class MasteryStat : IExposable
{
    private float offset;
    private StatDef stat;

    private string statDefName;

    public MasteryStat()
    {
    }

    public MasteryStat(StatDef stat, float offset)
    {
        this.stat = stat;
        statDefName = stat.defName;
        this.offset = offset;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref statDefName, "stat");
        Scribe_Values.Look(ref offset, "offset");
    }

    public StatDef GetStat()
    {
        return stat;
    }

    public float GetOffset()
    {
        return offset;
    }

    public void SetOffset(float value)
    {
        offset = value;
    }

    public string GetStatDefName()
    {
        return statDefName;
    }

    public void Resolve()
    {
        stat = DefDatabase<StatDef>.GetNamed(statDefName, false);
    }
}
