using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FreeScaleLogic : ManipulationLogic<Vector3>
    {
        private Vector3 startObjectScale;
        private Vector3 startAttachTransformScale;
        private Vector3 startHandDistanceMeters;
        private Transform target;

        /// <inheritdoc />
        public override void Setup(List<IXRSelectInteractor> interactors, IXRSelectInteractable interactable, MixedRealityTransform currentTarget)
        {
            base.Setup(interactors, interactable, currentTarget);

            target = interactable.transform;
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
                    return new Vector3(startObjectScale.x * ratioMultiplier.x, startObjectScale.y, startObjectScale.z);
                }
                else if (Math.Abs(max - ydiff) < 0.0001f)
                {
                    return new Vector3(startObjectScale.x , startObjectScale.y* ratioMultiplier.y, startObjectScale.z);
                }
                else{                    
                    return new Vector3(startObjectScale.x , startObjectScale.y, startObjectScale.z* ratioMultiplier.z);
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
