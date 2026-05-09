using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-100)] 
    public class LiquidMenuItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("Hierarchy")]
        public LiquidMenuItem parentItem;
        public List<LiquidMenuItem> childItems;
        public bool isRoot = false;

        [Header("Startup")]
        public bool autoExpandOnStart = false;

        [Header("Scale")]
        [Tooltip("Multiplier applied to the item when it is spawned.")]
        public float spawnScaleMultiplier = 1.0f; 

        [Tooltip("Multiplier applied on top of the spawn scale while the item is active.")]
        public float activeScaleMultiplier = 1.0f; 

        [Header("Animation")]
        public float popDuration = 0.6f;     
        public float retractDuration = 0.2f; 
        [Range(0, 2)] public float elasticity = 1.0f; 
        public float staggerDelay = 0.05f;   

        [Header("Visibility")]
        public bool autoHideSiblings = true;
        public List<GameObject> customObjectsToHide;
        public List<GameObject> customObjectsToShow;

        [Header("Events")]
        public UnityEvent onActionTriggered;

        private Vector3 _initLocalPos; 
        private Vector3 _initScale;    
        private bool _isOpen = false; 
        private Tween _autoExpandTween;

        public bool IsOpen => _isOpen;

        public Vector3 TargetSpawnScale => _initScale * spawnScaleMultiplier;

        void Awake()
        {
            _initLocalPos = transform.localPosition;
            _initScale = transform.localScale;

            if (!isRoot)
            {
                transform.localScale = Vector3.zero;
                gameObject.SetActive(false);
            }
        }

        void Start()
        {
            if (isRoot && autoExpandOnStart)
            {
                ScheduleAutoExpand(0.5f);
            }
        }

        private void OnDisable()
        {
            CancelAutoExpandTree();
            _isOpen = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            OnClick();
        }

        public void OnClick()
        {
            if (LiquidMenuManager.Instance)
            {
                LiquidMenuManager.Instance.HandleItemSelected(this);
            }

            if (childItems != null && childItems.Count > 0)
            {
                if (!_isOpen) OpenChildren();
            }
            else
            {
                ProjectLog.Info($"Triggered liquid menu action: {name}", this);
                if (!_isOpen)
                {
                    _isOpen = true; 
                    transform.DOScale(TargetSpawnScale * activeScaleMultiplier, 0.2f);
                    ProcessVisibility(true);
                }
                onActionTriggered?.Invoke(); 
            }
        }

        public void OpenChildren()
        {
            CancelAutoExpand();

            if (_isOpen || childItems == null || childItems.Count == 0) return;
            _isOpen = true;
            
            if (Mathf.Abs(activeScaleMultiplier - 1.0f) > 0.001f)
            {
                transform.DOScale(TargetSpawnScale * activeScaleMultiplier, 0.3f).SetEase(Ease.OutBack);
            }
            
            ProcessVisibility(true);

            for (int i = 0; i < childItems.Count; i++)
            {
                var child = childItems[i];
                if (child == null) continue;

                child.gameObject.SetActive(true);
                
                child.transform.position = this.transform.position; 
                child.transform.localScale = Vector3.zero;

                var childIconCtrl = child.GetComponent<LiquidIconController>();
                if (childIconCtrl) 
                {
                    Vector3 childRealSize = child._initScale * child.spawnScaleMultiplier;
                    childIconCtrl.ForceUpdateOriginalScale(childRealSize);
                }

                float delay = i * staggerDelay;
                child.ShowAnimate(delay, popDuration, elasticity);

                if (child.autoExpandOnStart)
                {
                    float autoExpandDelay = delay + popDuration * 0.8f;
                    child.ScheduleAutoExpand(autoExpandDelay);
                }
            }
        }

        public void CloseChildren()
        {
            CancelAutoExpandTree();

            if (!_isOpen) return;
            _isOpen = false;

            if (childItems != null)
            {
                foreach (var child in childItems)
                {
                    if (child == null) continue;

                    if (child.IsOpen) child.CloseChildren(); 
                    child.HideAnimate(this.transform.position); 
                }
            }

            transform.DOScale(TargetSpawnScale, retractDuration);

            ProcessVisibility(false);

            if (LiquidMenuManager.Instance)
            {
                LiquidMenuManager.Instance.SetCurrentFocus(parentItem != null ? parentItem : null);
            }
        }

        private void ProcessVisibility(bool isOpening)
        {
            if (autoHideSiblings)
            {
                if (parentItem != null && parentItem.childItems != null)
                {
                    foreach (var sibling in parentItem.childItems)
                    {
                        if (sibling != this) 
                        {
                            if (isOpening) 
                                sibling.HideAnimate(parentItem.transform.position); 
                            else 
                                sibling.ShowAnimate(0, 0.4f, 1f); 
                        }
                    }
                }
                else if (isRoot && LiquidMenuManager.Instance)
                {
                    if (isOpening) LiquidMenuManager.Instance.HideOtherRoots(this);
                    else LiquidMenuManager.Instance.ShowAllRoots();
                }
            }

            if (customObjectsToHide != null)
            {
                foreach (var obj in customObjectsToHide)
                {
                    if (obj == null) continue;
                    obj.SetActive(!isOpening); 
                }
            }

            if (customObjectsToShow != null)
            {
                foreach (var obj in customObjectsToShow)
                {
                    if (obj == null) continue;
                    obj.SetActive(isOpening);
                }
            }
        }

        public void ShowAnimate(float delay, float duration, float elastic)
        {
            CancelAutoExpand();

            gameObject.SetActive(true);
            var iconCtrl = GetComponent<LiquidIconController>();
            if (iconCtrl) iconCtrl.PlaySpawnAnimation(duration * 0.8f);

            transform.DOLocalMove(_initLocalPos, duration)
                     .SetEase(Ease.OutElastic, elastic)
                     .SetDelay(delay)
                     .OnComplete(() => {
                         transform.localPosition = _initLocalPos; 
                     });

            transform.DOScale(TargetSpawnScale, duration).SetEase(Ease.OutElastic, elastic).SetDelay(delay);
        }

        public void HideAnimate(Vector3? targetWorldPos = null)
        {
            CancelAutoExpandTree();
            _isOpen = false;

            Vector3 dest = targetWorldPos ?? transform.position;

            transform.DOKill(); 

            transform.DOMove(dest, retractDuration).SetEase(Ease.InQuad);

            transform.DOScale(Vector3.zero, retractDuration).SetEase(Ease.InQuad)
                     .OnComplete(() => gameObject.SetActive(false));
        }

        public void CancelExternalRuntimeControl(bool closeChildTree)
        {
            CancelAutoExpand();
            transform.DOKill();
            _isOpen = false;
            transform.localPosition = _initLocalPos;

            if (!closeChildTree || childItems == null) return;

            foreach (LiquidMenuItem child in childItems)
            {
                if (child == null) continue;

                child.CancelExternalRuntimeControl(true);
                child._isOpen = false;
                child.transform.localScale = Vector3.zero;
                child.gameObject.SetActive(false);
            }
        }

        private void ScheduleAutoExpand(float delay)
        {
            CancelAutoExpand();
            _autoExpandTween = DOVirtual.DelayedCall(Mathf.Max(0f, delay), () =>
            {
                _autoExpandTween = null;
                OpenChildren();
            });
        }

        private void CancelAutoExpandTree()
        {
            CancelAutoExpand();

            if (childItems == null) return;

            foreach (LiquidMenuItem child in childItems)
            {
                if (child != null)
                {
                    child.CancelAutoExpandTree();
                }
            }
        }

        private void CancelAutoExpand()
        {
            _autoExpandTween?.Kill();
            _autoExpandTween = null;
        }
    }
}
