using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup.Scripts
{
    public class MenuToggle : MonoBehaviour, IInteractionModeDetector
    {

        private bool UIMode = false;
        
        public InputActionAsset actions;

        private UnityAction menuButtonOneAction;
        

        //For interaction mode selection
        [SerializeField] public GameObject leftHandController;
        [SerializeField] private GameObject rightHandController;
        private List<GameObject> controllers;
        [SerializeField]
        private InteractionMode modeOnDetection;

        private void Awake()
        {
            actions.FindActionMap("UI").FindAction("ToggleMenu").performed += OnToggleMenu;
        }

        // Start is called before the first frame update
        void Start()
        {
            controllers = new List<GameObject>();
            controllers.Add(leftHandController);
            controllers.Add(rightHandController);
        }

        private void OnEnable()
        {
            actions.FindActionMap("UI").Enable();
        }

        private void OnDisable()
        {
            actions.FindActionMap("UI").Disable();
        }

        // Update is called once per frame
        void Update()
        {
        }

        private void OnToggleMenu(InputAction.CallbackContext context)
        {
            UIMode = !UIMode;
            BrainSelectionInteractable[] visualizations = gameObject.GetComponentsInChildren<BrainSelectionInteractable>();
            foreach (BrainSelectionInteractable viz in visualizations)
            {
                viz.UIModeChanged(UIMode);
            }
        }

        public bool IsModeDetected()
        {
            return UIMode;
        }

        public List<GameObject> GetControllers()
        {
            return controllers;
        }

        public InteractionMode ModeOnDetection => modeOnDetection;

        public bool isUIMode()
        {
            return UIMode;
        }
    }
}
