using UnityEngine;
using UnityEngine.UI;

namespace Match3.Utils
{
    /// <summary>
    /// Adjusts camera orthographic size to fit content based on canvas reference resolution.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class AdjustCamera : MonoBehaviour
    {
        [SerializeField] private float targetSize = 0;
        [SerializeField] private CanvasScaler canvas = null;

        private Camera cam = null;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            AdjustCameraSize();
        }

#if UNITY_EDITOR
    private void OnGUI()
        {
            AdjustCameraSize();
        }
#endif

        private void AdjustCameraSize()
        {
            if (cam.aspect == 0 || canvas == null || canvas.referenceResolution.y == 0)
                return;

            float referenceAspect = canvas.referenceResolution.x / canvas.referenceResolution.y;
            if (cam.aspect < referenceAspect)
                // Screen is narrower than reference - expand vertical view
                cam.orthographicSize = targetSize * referenceAspect / cam.aspect;
            else
                cam.orthographicSize = targetSize;

            // Match width or height based on aspect ratio
            canvas.matchWidthOrHeight = Camera.main.aspect < referenceAspect ? 0 : 1;
        }
    }
}
