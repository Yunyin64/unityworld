using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityWorld.Core
{
    public class AssatMgr:IDomainMgrBase
    {
        public static AssatMgr? Instance { get; private set; }

        public string Name => "AssatMgr";

        public string Desc => throw new NotImplementedException();


        public void Init()
        {
            Instance = this;
        }
        public void End()
        {
           
        }
		public static string GetFixedPath(string path, int index = -1)
		{
			if (string.IsNullOrEmpty(path))
			{
				return "";
			}
			path = path.Trim();
			path = path.Replace("\n", string.Empty);
			path = path.Replace("\r", string.Empty);
			path = path.Replace("\t", string.Empty);
			path = path.Replace("\\", "/");
			string[] array = path.Split(new char[]
			{
				';'
			});
			if (index == -1)
			{
				//path = array[World.RandomRange(0, array.Length, GMathUtl.RandomType.emNone)];
			}
			else
			{
				path = array[index];
			}
			path = path.Trim();
			return path;
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

        /*
public Sprite GetSprite(string path,int layout)
{
    path = GetFixedPath(path, -1);
    try
    {
        if (this.m_mapSprites.ContainsKey(path))
        {
            return this.m_mapSprites[path];
        }
        if (path.IndexOf('.') <= 0)
        {
            int num = path.LastIndexOf("/");
            string text = path;
            if (num >= 0)
            {
                text = path.Substring(0, num);
            }
            if (!this.m_lisLoadedPath.Contains(text))
            {
                Sprite[] array = Resources.LoadAll<Sprite>(text);
                for (int i = 0; i < array.Length; i++)
                {
                    this.m_mapSprites[text + "/" + array[i].name] = array[i];
                }
                this.m_lisLoadedPath.Add(text);
                if (this.m_mapSprites.ContainsKey(path))
                {
                    return this.m_mapSprites[path];
                }
            }
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                this.m_mapSprites[path] = sprite;
                return sprite;
            }
        }
    }
    catch (Exception ex)
    {
        LogMgr.Err(string.Concat(new object[]
        {
        "Sprite Error:",
        path,
        "\n",
        ex.Message,
        "\n",
        ex.InnerException
        }), new object[0]);
        path = null;
    }
    Texture2D texture2D = this.GetTexture2D(path);
    if (texture2D != null)
    {
        Sprite sprite2 = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.Tight);
        this.m_mapSprites[path] = sprite2;
        return sprite2;
    }
    return null;
}
public SpriteRenderer GetIconSprFromPool(string name)
{
    List<SpriteRenderer> list = null;
    if (this.m_IconPool.TryGetValue(name, out list) && list.Count > 0)
    {
        SpriteRenderer result = list[0];
        list.RemoveAt(0);
        return result;
    }
    GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("CMDIcon")) as GameObject;
    gameObject.name = name;
    SpriteRenderer componentInChildren = gameObject.GetComponentInChildren<SpriteRenderer>();
    componentInChildren.sprite = this.GetSprite(name,0);
    return componentInChildren;
}
public Texture2D GetTexture2D(string path)
{
    if (string.IsNullOrEmpty(path))
    {
        return null;
    }
    Texture2D result;
    if (path.IndexOf('.') > 0)
    {
        result = ModsMgr.Instance.LoadTexture2D("Resources/" + path);
    }
    else
    {
        result = Resources.Load<Texture2D>(path);
    }
    return result;
}

private Dictionary<string, List<SpriteRenderer>> m_IconPool = new Dictionary<string, List<SpriteRenderer>>();

private Dictionary<string, Sprite> m_mapSprites = new Dictionary<string, Sprite>();

*/
        private List<string> m_lisLoadedPath = new List<string>();
	}
}
