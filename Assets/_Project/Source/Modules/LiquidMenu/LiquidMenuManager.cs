using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public class LiquidMenuManager : MonoBehaviour
    {
        public static LiquidMenuManager Instance { get; private set; }

        [Header("Root Items")]
        public List<LiquidMenuItem> rootItems = new List<LiquidMenuItem>();

        [Header("Pointer Routing")]
        [SerializeField] private bool ensurePointerRouting = true;

        private LiquidMenuItem _currentFocus;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple LiquidMenuManager instances detected; the latest instance will be used.", this);
            }

            Instance = this;

            if (ensurePointerRouting)
            {
                ScenePointerRouting.Ensure();
            }
        }

        public void HandleItemSelected(LiquidMenuItem item)
        {
            if (item == null) return;

            bool switchingRoot = item.isRoot
                && _currentFocus != null
                && _currentFocus != item
                && !_currentFocus.transform.IsChildOf(item.transform);

            if (switchingRoot)
            {
                _currentFocus.CloseChildren();
            }

            _currentFocus = item;
        }

        public void SetCurrentFocus(LiquidMenuItem item)
        {
            _currentFocus = item;
        }

        public void DismissCurrentFocus()
        {
            if (_currentFocus != null)
            {
                _currentFocus.CloseChildren();
            }
        }

        public void HideOtherRoots(LiquidMenuItem activeRoot)
        {
            foreach (LiquidMenuItem root in rootItems)
            {
                if (root != null && root != activeRoot)
                {
                    root.HideAnimate();
                }
            }
        }

        public void ShowAllRoots()
        {
            foreach (LiquidMenuItem root in rootItems)
            {
                if (root != null && !root.gameObject.activeSelf)
                {
                    root.ShowAnimate(0, 0.5f, 1f);
                }
            }
        }
    }
}
