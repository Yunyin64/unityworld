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
        public static InputMgr? Instance { get; private set; }


        public string Desc => throw new NotImplementedException();

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
            throw new NotImplementedException();
        }

        public void Tick(float deltaTime)
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


        private bool iscoldown = false;

        private float _ColDown = 0f;

        public float TimeColDown = 0.033f;

    }
}
