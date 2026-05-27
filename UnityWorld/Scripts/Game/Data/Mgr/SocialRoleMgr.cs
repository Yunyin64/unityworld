namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 社会角色数据管理器
    /// </summary>
    public class SocialRoleMgr : DefineMgrBase<SocialRoleDefine>
    {
        public static SocialRoleMgr Instance { get; private set; }

        public SocialRoleMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
