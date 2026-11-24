namespace UnityWorld.Game.Domain
{
        public enum APIType
        {
            /// <summary>未分类</summary>
            None,
            /// <summary>数值计算</summary>
            Scale,
            /// <summary>条件判定</summary>
            Condition,
            /// <summary>战斗拼点</summary>
            Contest,
            /// <summary>作用域选择</summary>
            Scope,
            /// <summary>执行动作</summary>
            Action,
            /// <summary>灵气操作</summary>
            Aura
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

        /// <summary>函数适用范围</summary>
        public Scope Scope { get; }
        /// <summary>API 功能分类</summary>
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
        /// <param name="type">函数的功能分类</param>
        /// <param name="desc">函数描述</param>
        /// <param name="scope">函数适用的作用域</param>
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