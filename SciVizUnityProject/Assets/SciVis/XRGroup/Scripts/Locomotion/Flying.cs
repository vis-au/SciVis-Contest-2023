using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup.Scripts
{
    public class Flying : MonoBehaviour
    {
        private InputAction flyingAction;

        public InputActionAsset actions;

        public XROrigin origin;

        public Transform leftHand;

        public Transform rightHand;

        [SerializeField]
        private GameObject flyingIndicator;

        private GameObject indicatorInstance;
        
        private float speed = 1.0f;


        private void Awake()
        {
            flyingAction = actions.FindActionMap("BrainViz").FindAction("Flying");
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
                float val = flyingAction.ReadValue<float>();
                Vector3 posLeft = leftHand.transform.position;
                Vector3 posRight = rightHand.transform.position;
                Vector3 moveDirection = (posRight - posLeft)*val;
                origin.GetComponent<Transform>().Translate(moveDirection*speed*Time.deltaTime, relativeTo:Space.World);
                indicatorInstance.transform.position = (posRight + posLeft) / 2;
                indicatorInstance.transform.rotation = Quaternion.LookRotation(moveDirection);
                float scale = (posRight - posLeft).magnitude;
                indicatorInstance.transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                indicatorInstance.SetActive(false);
            }
        }
    }
}
