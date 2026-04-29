using System.Reflection;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 参数类型枚举
    /// </summary>
    public enum Param_TYPE
    {
        Int,
        Float,
        String,
        Bool,
    }

    /// <summary>
    /// 参数定义：描述一个参数的类型和名称
    /// </summary>
    public struct Param
    {
        /// <summary>参数类型</summary>
        public Param_TYPE Type;

        /// <summary>参数名称（如 "Element"、"AttackValue"）</summary>
        public string Name;

        /// <summary>是否为可选参数（可选参数在 Params 列表末尾，不传时使用默认值）</summary>
        public bool IsOptional;
    }

    /// <summary>
    /// API 函数签名定义：描述一个可调用函数的名称和参数列表。
    /// 纯粹的签名注册，不带业务语义。
    /// </summary>
    public class API
    {
        /// <summary>函数名（如 "Attack"、"Heal"、"AddPoison"）</summary>
        public string FuncName;

        /// <summary>函数描述</summary>
        public string Desc;

        public MethodInfo Method;

        /// <summary>参数定义列表（有序，与 ActionDefine.Params 一一对应）</summary>
        public List<Param> ParamsList = new List<Param>();

        /// <summary>
        /// 构造 API 定义
        /// </summary>
        /// <param name="funcName">函数名</param>
        /// <param name="desc">函数描述</param>
        /// <param name="paramDefs">参数定义列表：(类型, 名称) 或 (类型, 名称, 是否可选)</param>
        public API(string funcName, string desc, params (Param_TYPE, string)[] paramDefs)
        {
            FuncName = funcName;
            Desc = desc;
            foreach (var (type, name) in paramDefs)
            {
                ParamsList.Add(new Param { Type = type, Name = name, IsOptional = false });
            }
        }

        /// <summary>
        /// 构造 API 定义（直接传入已解析的参数列表，供反射扫描使用）
        /// </summary>
        public API(string funcName, string desc, List<Param> paramsList)
        {
            FuncName = funcName;
            Desc = desc;
            ParamsList = paramsList;
        }

        /// <summary>
        /// 构造 API 定义（支持可选参数标记）
        /// </summary>
        public API(string funcName, string desc, (Param_TYPE Type, string Name)[] requiredParams, (Param_TYPE Type, string Name)[] optionalParams)
        {
            FuncName = funcName;
            Desc = desc;
            foreach (var (type, name) in requiredParams)
            {
                ParamsList.Add(new Param { Type = type, Name = name, IsOptional = false });
            }
            foreach (var (type, name) in optionalParams)
            {
                ParamsList.Add(new Param { Type = type, Name = name, IsOptional = true });
            }
        }

        /// <summary>
        /// 获取全部参数数量（含可选）
        /// </summary>
        public int ParamCount => ParamsList.Count;

        /// <summary>
        /// 获取必填参数数量
        /// </summary>
        public int RequiredParamCount => ParamsList.Count(p => !p.IsOptional);

        /// <summary>
        /// 按索引获取参数名称
        /// </summary>
        public string GetParamName(int index)
        {
            if (index < 0 || index >= ParamsList.Count) return "";
            return ParamsList[index].Name;
        }

        /// <summary>
        /// 按索引获取参数类型
        /// </summary>
        public Param_TYPE GetParamType(int index)
        {
            if (index < 0 || index >= ParamsList.Count) return Param_TYPE.String;
            return ParamsList[index].Type;
        }

        public override string ToString()
            => $"API({FuncName}, Params=[{string.Join(", ", ParamsList.Select(p => $"{p.Name}:{p.Type}"))}])";
    }
}