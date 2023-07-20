using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup.Scripts
{
    public class Rotation : MonoBehaviour
    {
        private InputAction rotationAction;

        public InputActionAsset actions;

        public XROrigin origin;
        
        private float speed = 80.0f;


        private void Awake()
        {
            rotationAction = actions.FindActionMap("BrainViz").FindAction("Rotation");
        }

        private void OnEnable()
        {
            actions.FindActionMap("BrainViz").Enable();
        }

        private void OnDisable()
        {
            actions.FindActionMap("BrainViz").Disable();
        }

        // Update is called once per frame
        private void Update()
        {
            if (rotationAction.IsPressed())
            {
                Vector2 touchPos = rotationAction.ReadValue<Vector2>();
                origin.GetComponent<Transform>().RotateAround(Camera.main.transform.position,Vector3.up, touchPos[0]*speed * Time.deltaTime);

            }
        }
    }
}