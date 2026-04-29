using UnityWorld.Core;

public struct SoulData
{
    public int Guid;
    public int NI;
    public int NE;
    public int TI;
    public int TE;
    public int FI;
    public int FE;
    public int SI;
    public int SE;
    public int MI;
    public int ME;
    
    private Rng rng { get; }
    public int NewId() => rng.NewId();

    public int Random(int min, int max) => rng.Range(min, max);
    
    public float Random(float min, float max) => rng.Range(min, max);
    public SoulData(int guid)
    {
        Guid = guid;
        rng = new Rng(guid);
        NI = rng.Range(0,100); NE = rng.Range(0,100);
        TI = rng.Range(0,100); TE = rng.Range(0,100);
        FI = rng.Range(0,100); FE = rng.Range(0,100);
        SI = rng.Range(0,100); SE = rng.Range(0,100);
        MI = rng.Range(0,100); ME = rng.Range(0,100);
    }
}
public interface ISoulBase
{
    public  SoulData Soul { get; }
    
    public  string LogSoulInfo()
    {
        return $"NI: {Soul.NI}, NE: {Soul.NE}, TI: {Soul.TI}, TE: {Soul.TE}, FI: {Soul.FI}, FE: {Soul.FE}, SI: {Soul.SI}, SE: {Soul.SE}, MI: {Soul.MI}, ME: {Soul.ME}";
    }
}