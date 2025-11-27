using UnityEngine;
using UnityEngine.UI;

namespace MiniIT.UTILS
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class AdjustCamera : MonoBehaviour
    {
        [SerializeField] private float desiredSize = 0;
        [SerializeField] private CanvasScaler canvas = null;

        private Camera cam = null;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnGUI()
        {
            if (cam.aspect == 0 || canvas.referenceResolution.y == 0)
            {
                return;
            }

            float referenceAspect = canvas.referenceResolution.x / canvas.referenceResolution.y;
            if (cam.aspect < referenceAspect)
            {
                cam.orthographicSize = desiredSize * referenceAspect / cam.aspect;
            }
            else
            {
                cam.orthographicSize = desiredSize;
            }

            canvas.matchWidthOrHeight = Camera.main.aspect < referenceAspect ? 0 : 1;
        }
    }
}
