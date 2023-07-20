using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using SciVis.XRGroup;
using SciVis.XRGroup.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SciVis.XRGroup.Scripts{
    public class BrainSelectionInteractable : MonoBehaviour
    {
        private int numHovering;
        public GameObject hoverIndicator;
        public GameObject selectedIndicator;
        private GameObject colorLegend;

        public GameObject handMenu;
        private BrainSelectionManager brainSelectionManager;

        private MenuToggle menuToggle;
        private bool selected;

        // Start is called before the first frame update
        void Awake()
        {
            brainSelectionManager = gameObject.GetComponentInParent<BrainSelectionManager>();
            menuToggle = gameObject.GetComponentInParent<MenuToggle>();
            colorLegend = transform.Find("ColorLegend").gameObject;
        }

        public void OnSelectEntered()
        {
            brainSelectionManager.Select(this);
        }

        public void OnHoverEntered()
        {
            numHovering++;
            if (menuToggle.isUIMode())
            {
                hoverIndicator.SetActive(true);
            }
        }

        public void OnHoverExited()
        {
            numHovering--;
            if (numHovering == 0)
            {
                hoverIndicator.SetActive(false);
            }
            
        }

        public void Select()
        {
            selected = true;
            if (GetComponentInParent<MenuToggle>().isUIMode())
            {
                selectedIndicator.SetActive(true);
                handMenu.SetActive(true);
                colorLegend.SetActive(true);
            }
            Debug.Log("Brain Selected");
        }
        
        public void DeSelect()
        {
            selected = false;
            
            selectedIndicator.SetActive(false);
            handMenu.SetActive(false);
            colorLegend.SetActive(false);
        }

        public bool GetSelected()
        {
            return selected;
        }
        
        public void UIModeChanged(bool uiMode)
        {
            bool shouldActivate = uiMode && selected;
            
            selectedIndicator.SetActive(shouldActivate);

            ObjectManipulator om = GetComponentInChildren<ObjectManipulator>();
            hoverIndicator.SetActive(uiMode && om.isHovered);
            handMenu.SetActive(shouldActivate);
            colorLegend.SetActive(shouldActivate);
        }
    }
}