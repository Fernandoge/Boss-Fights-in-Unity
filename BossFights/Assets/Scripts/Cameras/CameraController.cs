using UnityEngine;

namespace Cameras
{
    public class CameraController : MonoBehaviour
    {
        public Transform player;
        public float smoothSpeed = 0.125f;
        // While true the camera stays where it is, e.g. while the player blinks around during a skill
        public bool IsFrozen { get; set; }
        
        private Vector3 offset;
        
        void Start()
        {
            if (player != null)
            {
                offset = transform.position - player.position;
            }
        }
        
        void LateUpdate()
        {
            if (player == null || IsFrozen)
                return;
            
            Vector3 desiredPosition = player.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}
