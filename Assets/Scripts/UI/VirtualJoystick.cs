using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoesOfSteal.UI
{
    /// <summary>
    /// Virtual joystick layar sentuh (kiri bawah). Menangani pointer touch/mouse
    /// dan mengekspos vektor input 2D yang dikonsumsi oleh PlayerController.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _deadZone = 0.1f;

        private float _handleRange;

        /// <summary>Vektor input ternormalisasi (-1..1 per sumbu). Nol saat dilepas.</summary>
        public Vector2 InputVector { get; private set; }

        private void Start()
        {
            _handleRange = Mathf.Max(1f, Mathf.Min(_background.rect.width, _background.rect.height) * 0.5f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            localPoint = Vector2.ClampMagnitude(localPoint, _handleRange);
            _handle.anchoredPosition = localPoint;

            Vector2 vector = localPoint / _handleRange;
            InputVector = vector.magnitude < _deadZone ? Vector2.zero : vector;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _handle.anchoredPosition = Vector2.zero;
            InputVector = Vector2.zero;
        }
    }
}
