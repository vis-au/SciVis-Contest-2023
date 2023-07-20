using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TerrainTableScaling : ManipulationLogic<Vector3>
    {
        private Vector3 startObjectScale;
        private Vector3 startAttachTransformScale;
        private Vector3 startHandDistanceMeters;
        private Transform target;
        private Transform cylinderX1;
        private Transform cylinderX2;
        private Transform cylinderZ1;
        private Transform cylinderZ2;
        
        private Vector3 cylinderX1InitScale;
        private Vector3 cylinderX2InitScale;
        private Vector3 cylinderZ1InitScale;
        private Vector3 cylinderZ2InitScale;
        
        private Vector3 cylinderX1InitPos;
        private Vector3 cylinderX2InitPos;
        private Vector3 cylinderZ1InitPos;
        private Vector3 cylinderZ2InitPos;

        private Transform timePlane;
        private Vector3 timePlaneInitScale;
        private Vector3 timePlaneInitPos;

        private Transform viz;
        private Vector3 vizInitScale;
        private Vector3 vizInitPos;

        private Transform hoverIndicator;
        private Vector3 hoverIndicatorInitScale;
        private Vector3 hoverIndicatorInitPos;

        private Transform selectIndicator;
        private Vector3 selectIndicatorInitScale;
        private Vector3 selectIndicatorInitPos;

        private Transform secondHandle;
        private Vector3 secondHandleInitPos;
        /// <inheritdoc />
        public override void Setup(List<IXRSelectInteractor> interactors, IXRSelectInteractable interactable, MixedRealityTransform currentTarget)
        {
            base.Setup(interactors, interactable, currentTarget);

            target = interactable.transform;
            cylinderX1 = target.Find("CylinderScaleX1");
            cylinderX2 = target.Find("CylinderScaleX2");
            cylinderZ1 = target.Find("CylinderScaleZ1");
            cylinderZ2 = target.Find("CylinderScaleZ2");
            timePlane = target.parent.Find("Slider/Handle/TimePlane");
            viz = target.Find("Terrainview");
            hoverIndicator = target.parent.Find("Backplate");
            selectIndicator = target.parent.Find("Backplate2");
            secondHandle = target.parent.Find("Slider/Handle/ThumbModel2");
            
            cylinderX1InitScale = cylinderX1.localScale.Multiply(Vector3.one);
            cylinderX2InitScale = cylinderX2.localScale.Multiply(Vector3.one);
            cylinderZ1InitScale = cylinderZ1.localScale.Multiply(Vector3.one);
            cylinderZ2InitScale = cylinderZ2.localScale.Multiply(Vector3.one);
            timePlaneInitScale = timePlane.localScale.Multiply(Vector3.one);
            vizInitScale = viz.localScale.Multiply(Vector3.one);
            hoverIndicatorInitScale = hoverIndicator.localScale.Multiply(Vector3.one);
            selectIndicatorInitScale = selectIndicator.localScale.Multiply(Vector3.one);

            cylinderX1InitPos = cylinderX1.localPosition.Multiply(Vector3.one);
            cylinderX2InitPos = cylinderX2.localPosition.Multiply(Vector3.one);
            cylinderZ1InitPos = cylinderZ1.localPosition.Multiply(Vector3.one);
            cylinderZ2InitPos = cylinderZ2.localPosition.Multiply(Vector3.one);
            timePlaneInitPos = timePlane.localPosition.Multiply(Vector3.one);
            vizInitPos = viz.localPosition.Multiply(Vector3.one);
            hoverIndicatorInitPos = hoverIndicator.GetComponent<SolverHandler>().AdditionalOffset.Multiply(Vector3.one);
            selectIndicatorInitPos = selectIndicator.GetComponent<SolverHandler>().AdditionalOffset.Multiply(Vector3.one);

            secondHandleInitPos = secondHandle.localPosition.Multiply(Vector3.one);
            
            startAttachTransformScale = interactors[0].GetAttachTransform(interactable).localScale;
            startHandDistanceMeters = GetScaleBetweenInteractors(interactors, interactable);
            startObjectScale = currentTarget.Scale;
        }

        /// <inheritdoc />
        public override Vector3 Update(List<IXRSelectInteractor> interactors, IXRSelectInteractable interactable, MixedRealityTransform currentTarget, bool centeredAnchor)
        {
            base.Update(interactors, interactable, currentTarget, centeredAnchor);

            if (interactors.Count == 1)
            {
                // With a single interactor, apply the localScale of the attachTransform

                // Use the relative scale to handle cases in which the target selection happens with a non-default attachTransform scale
                var currentScale = interactors[0].GetAttachTransform(interactable).localScale;
                var relativeScale = new Vector3(
                    currentScale.x / startAttachTransformScale.x,
                    currentScale.y / startAttachTransformScale.y,
                    currentScale.z / startAttachTransformScale.z);

                var scaledByAttachTransform = startObjectScale;
                scaledByAttachTransform.Scale(relativeScale);
                return scaledByAttachTransform;
            }
            else
            {
                var ratioMultiplier = GetScaleBetweenInteractors(interactors, interactable);
                
                var xdiff = Math.Abs(ratioMultiplier.x - startHandDistanceMeters.x);
                var ydiff = Math.Abs(ratioMultiplier.y - startHandDistanceMeters.y);
                var zdiff = Math.Abs(ratioMultiplier.z - startHandDistanceMeters.z);
                
                ratioMultiplier.x /= startHandDistanceMeters.x;
                ratioMultiplier.y /= startHandDistanceMeters.y;
                ratioMultiplier.z /= startHandDistanceMeters.z;
                var max = Math.Max(Math.Max(xdiff, ydiff), zdiff);
                if (Math.Abs(max - xdiff) < 0.0001f)
                {
                    cylinderX1.localScale = new Vector3(cylinderX1InitScale.x,
                        cylinderX1InitScale.y * ratioMultiplier.x, cylinderX1InitScale.z);
                    cylinderX2.localScale = new Vector3(cylinderX2InitScale.x,
                        cylinderX2InitScale.y * ratioMultiplier.x, cylinderX2InitScale.z);
                    cylinderX1.localPosition = cylinderX1InitPos;
                    cylinderX2.localPosition = cylinderX2InitPos;


                    cylinderZ1.localScale = cylinderZ1InitScale;
                    cylinderZ2.localScale = cylinderZ2InitScale;
                    cylinderZ1.localPosition = new Vector3(cylinderZ1InitPos.x*ratioMultiplier.x, cylinderZ1InitPos.y, cylinderZ1InitPos.z);
                    cylinderZ2.localPosition = new Vector3(cylinderZ2InitPos.x*ratioMultiplier.x, cylinderZ2InitPos.y, cylinderZ2InitPos.z);

                    timePlane.localPosition = timePlaneInitPos;
                    timePlane.localScale = timePlaneInitScale;
                    
                    viz.localScale = new Vector3((vizInitScale.x + 0.1f)* ratioMultiplier.x - 0.1f,
                        vizInitScale.y , vizInitScale.z);
                    viz.localPosition = new Vector3(-0.5f*viz.localScale.x, vizInitPos.y, vizInitPos.z);

                    hoverIndicator.localScale = new Vector3(hoverIndicatorInitScale.x*ratioMultiplier.x, hoverIndicatorInitScale.y,
                        hoverIndicatorInitScale.z);
                    selectIndicator.localScale = hoverIndicator.localScale;
                    
                    Vector3 newOffset = new Vector3(hoverIndicatorInitPos.x*ratioMultiplier.x,
                        hoverIndicatorInitPos.y, hoverIndicatorInitPos.z);
                    hoverIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;
                    selectIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;

                    secondHandle.localPosition = secondHandleInitPos;
                    
                    return startObjectScale;
                }
                else if (Math.Abs(max - ydiff) < 0.0001f)
                {
                    viz.localScale = new Vector3(vizInitScale.x ,
                        vizInitScale.y* ratioMultiplier.y , vizInitScale.z);
                    viz.localPosition = new Vector3(vizInitPos.x, vizInitPos.y, vizInitPos.z);
                    
                    cylinderX1.localScale = cylinderX1InitScale;
                    cylinderX2.localScale = cylinderX2InitScale;
                    cylinderX1.localPosition = new Vector3(cylinderX1InitPos.x, cylinderX1InitPos.y, cylinderX1InitPos.z);
                    cylinderX2.localPosition = new Vector3(cylinderX2InitPos.x, cylinderX2InitPos.y, cylinderX2InitPos.z);
                    cylinderZ1.localScale = cylinderZ1InitScale;
                    cylinderZ2.localScale = cylinderZ2InitScale;
                    cylinderZ1.localPosition = new Vector3(cylinderZ1InitPos.x, cylinderZ1InitPos.y, cylinderZ1InitPos.z);
                    cylinderZ2.localPosition = new Vector3(cylinderZ2InitPos.x, cylinderZ2InitPos.y, cylinderZ2InitPos.z);

                    
                    timePlane.localScale = new Vector3(timePlaneInitScale.x, timePlaneInitScale.y*ratioMultiplier.y, timePlaneInitScale.z);
                    timePlane.localPosition = new Vector3(timePlaneInitPos.x, 0.5f*timePlane.localScale.y, timePlaneInitPos.z);
                    
                    secondHandle.localPosition = new Vector3(secondHandleInitPos.x, secondHandleInitPos.y*ratioMultiplier.y, secondHandleInitPos.z);
                    
                    hoverIndicator.localScale = new Vector3(hoverIndicatorInitScale.x, hoverIndicatorInitScale.y*ratioMultiplier.y,
                        hoverIndicatorInitScale.z);
                    selectIndicator.localScale = hoverIndicator.localScale;
                    
                    Vector3 newOffset = new Vector3(hoverIndicatorInitPos.x,
                        hoverIndicatorInitPos.y*ratioMultiplier.y, hoverIndicatorInitPos.z);
                    hoverIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;
                    selectIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;
                    return startObjectScale;
                }
                else{      
                    cylinderZ1.localScale = new Vector3(cylinderZ1InitScale.x,
                        cylinderZ1InitScale.y * ratioMultiplier.z, cylinderZ1InitScale.z);
                    cylinderZ2.localScale = new Vector3(cylinderZ2InitScale.x,
                        cylinderZ2InitScale.y * ratioMultiplier.z, cylinderZ2InitScale.z);
                    cylinderZ1.localPosition = cylinderZ1InitPos;
                    cylinderZ2.localPosition = cylinderZ2InitPos;
                    
                    cylinderX1.localScale = cylinderX1InitScale;
                    cylinderX2.localScale = cylinderX2InitScale;
                    cylinderX1.localPosition = new Vector3(cylinderX1InitPos.x, cylinderX1InitPos.y, cylinderX1InitPos.z*ratioMultiplier.z);
                    cylinderX2.localPosition = new Vector3(cylinderX2InitPos.x, cylinderX2InitPos.y, cylinderX2InitPos.z*ratioMultiplier.z);
                    
                    timePlane.localScale = new Vector3(timePlaneInitScale.x,
                        timePlaneInitScale.y, timePlaneInitScale.z * ratioMultiplier.z);
                    timePlane.localPosition = new Vector3(timePlaneInitPos.x, timePlaneInitPos.y, timePlaneInitPos.z * ratioMultiplier.z);
                    
                    viz.localScale = new Vector3(vizInitScale.x ,
                        vizInitScale.y , (vizInitScale.z + 0.1f)* ratioMultiplier.z - 0.1f);
                    viz.localPosition = new Vector3(vizInitPos.x, vizInitPos.y, -0.5f*viz.localScale.z);
                    
                    hoverIndicator.localScale = new Vector3(hoverIndicatorInitScale.x, hoverIndicatorInitScale.y,
                        hoverIndicatorInitScale.z*ratioMultiplier.z);
                    selectIndicator.localScale = hoverIndicator.localScale;
                    Vector3 newOffset = new Vector3(hoverIndicatorInitPos.x,
                        hoverIndicatorInitPos.y, hoverIndicatorInitPos.z*ratioMultiplier.z);
                    hoverIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;
                    selectIndicator.GetComponent<SolverHandler>().AdditionalOffset = newOffset;
                    
                    secondHandle.localPosition = secondHandleInitPos;
                    
                    return startObjectScale;
                }
                //return new Vector3(startObjectScale.x * ratioMultiplier.x, startObjectScale.y*ratioMultiplier.y, startObjectScale.z* ratioMultiplier.z);
            }
        }

        private Vector3 GetScaleBetweenInteractors(List<IXRSelectInteractor> interactors, IXRSelectInteractable interactable)
        {
            // If only one interactor, we never change scale.
            if (interactors.Count == 1)
            {
                return Vector3.one;
            }

            var result = float.MaxValue;
            var pos1 = new Vector3();
            var pos2 = new Vector3();
            for (int i = 0; i < interactors.Count; i++)
            {
                for (int j = i + 1; j < interactors.Count; j++)
                {
                    // Defer square root until end for performance.
                    var tempPos1 = target.InverseTransformPoint(interactors[i].GetAttachTransform(interactable).position);
                    var tempPos2 = target.InverseTransformPoint(interactors[j].GetAttachTransform(interactable).position);
                    var distance = Vector3.SqrMagnitude(tempPos1 - tempPos2);
                    if (distance < result)
                    {
                        result = distance;
                        pos1 = tempPos1;
                        pos2 = tempPos2;
                    }
                }
            }

            return new Vector3(Math.Abs(pos2.x - pos1.x), Math.Abs(pos2.y - pos1.y), Math.Abs(pos2.z - pos1.z));
        }
    }
