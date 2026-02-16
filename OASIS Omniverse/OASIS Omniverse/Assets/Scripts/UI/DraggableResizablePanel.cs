using OASIS.Omniverse.UnityHost.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OASIS.Omniverse.UnityHost.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DraggableResizablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private float minWidth = 320f;
        [SerializeField] private float minHeight = 220f;
        [SerializeField] private float resizeHandleSize = 24f;

        private RectTransform _rect;
        private RectTransform _parentRect;
        private bool _isResizing;
        private bool _isDragging;
        private Vector2 _dragOffset;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _parentRect = transform.parent as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rect == null)
            {
                return;
            }

            var local = ScreenToLocal(eventData.position, eventData.pressEventCamera, _rect);
            _isResizing = IsInResizeZone(local);
            _isDragging = !_isResizing;
            if (_isDragging)
            {
                _dragOffset = local;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rect == null || _parentRect == null)
            {
                return;
            }

            if (_isResizing)
            {
                Resize(eventData.position, eventData.pressEventCamera);
                return;
            }

            if (_isDragging)
            {
                Drag(eventData.position, eventData.pressEventCamera);
            }
        }

        public OASISResult<bool> SetMinSize(float width, float height)
        {
            minWidth = Mathf.Max(100f, width);
            minHeight = Mathf.Max(100f, height);
            return OASISResult<bool>.Success(true);
        }

        private void Drag(Vector2 screenPosition, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPosition, eventCamera, out var parentPoint))
            {
                return;
            }

            var size = _rect.rect.size;
            var targetLocal = parentPoint - _dragOffset;
            var clamped = ClampToParent(targetLocal, size);
            _rect.anchoredPosition = clamped;
        }

        private void Resize(Vector2 screenPosition, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPosition, eventCamera, out var parentPoint))
            {
                return;
            }

            var panelPos = _rect.anchoredPosition;
            var width = Mathf.Max(minWidth, parentPoint.x - panelPos.x);
            var height = Mathf.Max(minHeight, panelPos.y - parentPoint.y);

            var maxWidth = _parentRect.rect.width - panelPos.x;
            var maxHeight = panelPos.y + _parentRect.rect.height;
            width = Mathf.Min(width, maxWidth);
            height = Mathf.Min(height, maxHeight);

            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private bool IsInResizeZone(Vector2 localPoint)
        {
            var width = _rect.rect.width;
            var height = _rect.rect.height;
            return localPoint.x >= width - resizeHandleSize && localPoint.y <= -height + resizeHandleSize;
        }

        private static Vector2 ScreenToLocal(Vector2 screenPosition, Camera eventCamera, RectTransform target)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPosition, eventCamera, out var local);
            return local;
        }

        private Vector2 ClampToParent(Vector2 anchoredPos, Vector2 size)
        {
            var minX = 0f;
            var maxX = Mathf.Max(0f, _parentRect.rect.width - size.x);
            var minY = Mathf.Min(0f, -_parentRect.rect.height + size.y);
            var maxY = 0f;
            return new Vector2(Mathf.Clamp(anchoredPos.x, minX, maxX), Mathf.Clamp(anchoredPos.y, minY, maxY));
        }
    }
}

