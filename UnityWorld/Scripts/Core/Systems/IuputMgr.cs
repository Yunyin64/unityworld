using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace UnityWorld.Core
{
    public class InputMgr:IDomainMgrBase
    {
        public InputMgr()
        {
        }
        public static InputMgr Instance { get; private set; }


        public string Desc => "";

        public string Name => "InputMgr";

        public  void Init()
        {
            InputMgr.Instance = this;
        }
        public  void Update()
        {
            //_ColDown += Time.deltaTime;
            if(_ColDown > TimeColDown)
            {
                _ColDown -= TimeColDown;
                iscoldown = true;
            }
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


        private bool iscoldown = false;

        private float _ColDown = 0f;

        public float TimeColDown = 0.033f;

    }
}
