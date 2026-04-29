using NLua;

public interface ILuaBindable
{
    public LuaTable env { get; set; }
}