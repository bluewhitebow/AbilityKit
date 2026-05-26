using System;
using System.Threading.Tasks;

namespace ET.Logic
{
    /// <summary>
    /// Demo 流程管理�?
    /// 处理 Scene 之间的切换逻辑
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoProcessComponent: Entity, IAwake, IUpdate
    {
        public Scene CurrentScene { get; set; }
        public DemoLoginComponent LoginComponent { get; set; }

        public void Awake()
        {
        }

        public void Update(DemoProcessComponent self)
        {
        }
    }
}
