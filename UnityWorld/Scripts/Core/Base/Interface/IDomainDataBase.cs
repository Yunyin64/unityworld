using System.Collections;
/// <summary>
/// 运行时数据封装
/// </summary>
public interface IDomainDataBase
{
        void Log();
        IDomainDataBase Clone();
}
