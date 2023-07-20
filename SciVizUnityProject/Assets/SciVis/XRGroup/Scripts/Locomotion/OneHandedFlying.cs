using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup.Scripts
{
    public class OneHandedFlying : MonoBehaviour
    {
        private InputAction flyingAction;

        public InputActionAsset actions;

        public XROrigin origin;

        public Transform rightHand;
        
        [SerializeField]
        private GameObject flyingIndicator;

        private GameObject indicatorInstance;
        
        private float speed = 1.0f;
        private void Awake()
        {
            flyingAction = actions.FindActionMap("BrainViz").FindAction("OneHandedFlying");
        }

        private void OnEnable()
        {
            actions.FindActionMap("BrainViz").Enable();
        }

        private void OnDisable()
        {
            actions.FindActionMap("BrainViz").Disable();
        }

        private void Start()
        {
            indicatorInstance = Instantiate(flyingIndicator, rightHand);
            indicatorInstance.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (flyingAction.IsPressed())
            {
                indicatorInstance.SetActive(true);
                float val = flyingAction.ReadValue<Vector2>()[1];
                var rightHandTransform = rightHand.transform;
                Vector3 posRight = rightHandTransform.position;
                var transformForward = rightHandTransform.forward;
                Vector3 moveDirection = transformForward*val;
                origin.GetComponent<Transform>().Translate(moveDirection*speed*Time.deltaTime, relativeTo:Space.World);
                indicatorInstance.transform.position = posRight + transformForward*0.15f;
                indicatorInstance.transform.rotation = Quaternion.LookRotation(moveDirection);
                float scale = 0.25f;
                indicatorInstance.transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                indicatorInstance.SetActive(false);
            }
        }
    }
}
