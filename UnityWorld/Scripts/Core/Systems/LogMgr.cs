using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityWorld.Core
{
    /// <summary>
    /// log控制器，用于官方调试
    /// </summary>
    public  class  LogMgr:IDomainMgrBase
    {
        public string Name => "LogMgr";

        public string Desc => throw new NotImplementedException();


        public  void Init()
        {
            
        }
        public  void End()
        {

        }

        public static void Err(string ex,params object[] objs)
        {

        }

        public static void Dbg(string ex, params object[] objs)
        {

        }

        public static void Warn(string ex, params object[] objs)
        {

        }

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void Tick(float deltaTime)
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public void Render(float dt)
        {
            throw new NotImplementedException();
        }

        public IEnumerator Save()
        {
            throw new NotImplementedException();
        }

        public IEnumerator Load()
        {
            throw new NotImplementedException();
        }
    }
}
