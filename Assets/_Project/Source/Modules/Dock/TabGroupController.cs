using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    public class TabGroupController : MonoBehaviour
    {
        [Header("Pages")]
        public GameObject[] contentPages;

        [Header("Tabs")]
        public Image[] tabButtonImages;

        [Header("Style")]
        public Color activeColor = Color.white;
        public Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

        private void Start()
        {
            OnTabClicked(0);
        }

        public void OnTabClicked(int index)
        {
            SetActivePage(index);
            SetActiveTab(index);
        }

        private void SetActivePage(int index)
        {
            if (contentPages == null) return;

            for (int i = 0; i < contentPages.Length; i++)
            {
                if (contentPages[i])
                {
                    contentPages[i].SetActive(i == index);
                }
            }
        }

        private void SetActiveTab(int index)
        {
            if (tabButtonImages == null) return;

            for (int i = 0; i < tabButtonImages.Length; i++)
            {
                if (tabButtonImages[i])
                {
                    tabButtonImages[i].color = i == index ? activeColor : inactiveColor;
                }
            }
        }
    }
}
