using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;

namespace SciVis.XRGroup.Scripts.TwoKnobSlider
{
    public class TwoKnobSliderHandler : MonoBehaviour
    {
        [SerializeField]
        private Slider sliderLeft;
        [SerializeField]
        private Slider sliderRight;

        private TMPro.TextMeshProUGUI leftText;
        private TMPro.TextMeshProUGUI rightText;

        private float min;

        private float max;

        private List<UnityAction<float,float>> slidingFinishedListeners;

        private IBrainSubject brainSubject;
        
        // Start is called before the first frame update
        void Awake()
        {
            slidingFinishedListeners = new List<UnityAction<float,float>>();
            sliderLeft.MaxValue = 1;
            sliderLeft.MinValue = 0;
            sliderRight.MaxValue = 1;
            sliderRight.MinValue = 0;

            if (max == 0)
            {
                min = 0;
                max = 100; 
            }

            UnityAction<SliderEventData> action = (SliderEventData data) => sliderValuesChanged();
            leftText = sliderLeft.transform.Find("Handle").GetComponentInChildren<TMPro.TextMeshProUGUI>();
            rightText = sliderRight.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            sliderLeft.OnValueUpdated.AddListener(action);
            sliderRight.OnValueUpdated.AddListener(action);
            sliderValuesChanged();
            
            sliderLeft.OnClicked.AddListener(()=>UpdateListeners());
            sliderRight.OnClicked.AddListener(()=>UpdateListeners());

            brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
        }

        private void UpdateListeners()
        {
            foreach (UnityAction<float,float> listener in slidingFinishedListeners)
            {
                (float left, float right) = getValues();
                listener.Invoke(left, right);
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void setValues(float valLeft, float valRight)
        {
            //Normalize values
            float maxMinDiff = max - min;
            if (maxMinDiff == 0)
            {
                return;
            }
            valLeft = (valLeft - min) / maxMinDiff;
            valRight = (valRight - min) / maxMinDiff;
                
            if (valLeft > sliderLeft.MaxValue)
            {
                valLeft = sliderLeft.MaxValue;
            }
            else if (valLeft < sliderLeft.MinValue)
            {
                valLeft = sliderLeft.MinValue;
            }
            
            if (valRight > sliderLeft.MaxValue)
            {
                valRight = sliderLeft.MaxValue;
            }
            else if (valRight < sliderLeft.MinValue)
            {
                valRight= sliderLeft.MinValue;
            }
            sliderLeft.Value = sliderLeft.MaxValue - valLeft;
            sliderRight.Value = valRight;

        }

        public (float vLeft, float vRight) getValues()
        {
            float leftNormalized = sliderLeft.MaxValue - sliderLeft.Value;
            float rightNormalized = sliderRight.Value;

            float maxMinDiff = max - min;
            float leftAbsolute = min + leftNormalized * maxMinDiff;
            float rightAbsolute = min + rightNormalized * maxMinDiff;
            return (leftAbsolute,rightAbsolute);
        }
        
        public void setMin(float min)
        {
            this.min = min;
        }
        
        public void setMax(float max)
        {
            this.max = max;
        }

        public (float, float) getMinAndMax()
        {
            return (this.min, this.max);
        }

        private void sliderValuesChanged()
        {
            (float v1, float v2) vals = getValues();

            leftText.text = String.Format("{0:0.00}", vals.v1);
            rightText.text = String.Format("{0:0.00}", vals.v2);
        }

        public void AddSlidingFinishedListener(UnityAction<float,float> action)
        {
            slidingFinishedListeners.Add(action);
        }
    }
}
