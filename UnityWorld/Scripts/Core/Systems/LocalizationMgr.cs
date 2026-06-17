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
        public static LocalizationMgr Instance { get; private set; }

        public string Name => "LocalizationMgr";

        public string Desc => "";


        public  void Init()
        {
            Instance = this;

        }
        public  void End()
        {

        }

        public void Begin()
        {
             
        }

        public void Tick(float deltaTime)
        {
             
        }

        public void Update()
        {
             
        }

        public void Render(float dt)
        {
             
        }

    public IEnumerator Save()
        {
            yield break;
        }

    public IEnumerator Load()
        {
            yield break;
        }

    }
}
