using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityWorld.Core
{
    public class LocalizationMgr :IDomainMgrBase
    {
        public static LocalizationMgr? Instance { get; private set; }

        public string Name => throw new NotImplementedException();

        public string Desc => throw new NotImplementedException();


        public  void Init()
        {
            Instance = this;

        }
        public  void End()
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
