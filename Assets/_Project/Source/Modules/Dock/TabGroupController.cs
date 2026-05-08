using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    public class TabGroupController : MonoBehaviour
    {
        [Header("--- 1. 内容页面 (要把 Page 拖进来) ---")]
        public GameObject[] contentPages; // 数组：0号放Common页, 1号放Seats页

        [Header("--- 2. Tab 按钮背景 (要把 Button 的 Image 拖进来) ---")]
        public Image[] tabButtonImages;   // 数组：0号放Common按钮图, 1号放Seats按钮图

        [Header("--- 3. 样式设置 ---")]
        // 选中时变成白色 (看起来是亮的)
        public Color activeColor = Color.white; 
        
        // 没选中时变成灰色 (看起来是暗的/透明的)
        public Color inactiveColor = new Color(1f, 1f, 1f, 0.5f); 
        
        // 如果你是用“换图片”的方式（比如选中是实心图，没选是空心图），可以用 Sprite 替换 Color 逻辑
        // public Sprite activeSprite;
        // public Sprite inactiveSprite;

        void Start()
        {
            // 默认打开第 0 个页面 (Common)
            OnTabClicked(0);
        }

        // 🔘 这个方法给按钮绑定
        // index = 0 代表 Common
        // index = 1 代表 Seats
        public void OnTabClicked(int index)
        {
            // 1. 遍历所有页面：如果是 index 就显示，不是就隐藏
            for (int i = 0; i < contentPages.Length; i++)
            {
                if (contentPages[i] != null)
                {
                    contentPages[i].SetActive(i == index);
                }
            }

            // 2. 遍历所有按钮：如果是 index 就变亮，不是就变暗
            for (int i = 0; i < tabButtonImages.Length; i++)
            {
                if (tabButtonImages[i] != null)
                {
                    tabButtonImages[i].color = (i == index) ? activeColor : inactiveColor;
                    
                    // 如果你想改图片，就写:
                    // tabButtonImages[i].sprite = (i == index) ? activeSprite : inactiveSprite;
                }
            }
        }
    }
}