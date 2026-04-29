namespace UnityWorld.Game.Domain
{
        public enum APIType
        {
            None,
            Condition,
            Contest,
            Action
        }
    /// <summary>
    /// 标记一个静态方法为可执行的 API 函数，同时声明其参数签名。
    /// APIMgr 初始化时通过反射扫描所有带此 Attribute 的静态方法，
    /// 按 FuncName 注册执行委托和 API 签名定义。
    /// 参数签名格式：每个元素为 "ParamName:Type"，可选参数以 "?" 前缀标记，如 "?Duration:Float"。
    /// 支持的 Type：Int, Float, String, Bool。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class APIFuncAttribute : Attribute
    {
        /// <summary>函数名（全局唯一，如 "Attack"、"GiveTrait"）</summary>
        public string FuncName { get; }

        /// <summary>函数描述</summary>
        public string Desc { get; }

        public Scope Scope { get; }
        public APIType ApiType { get; }

        /// <summary>
        /// 参数签名定义列表。
        /// 每个元素格式为 "ParamName:Type" 或 "?ParamName:Type"（可选参数）。
        /// </summary>
        public string[] ParamDefs { get; }

        /// <summary>
        /// 构造 APIFunc 标记
        /// </summary>
        /// <param name="funcName">函数名（全局唯一）</param>
        /// <param name="desc">函数描述</param>
        /// <param name="paramDefs">参数签名：每个元素为 "ParamName:Type" 或 "?ParamName:Type"</param>
        public APIFuncAttribute(string funcName,APIType type = APIType.None, string desc = "",Scope scope = Scope.Global, params string[] paramDefs)
        {
            FuncName = funcName;
            Desc = desc;
            ApiType = type;
            Scope = scope;
            ParamDefs = paramDefs;
        }
    }
}