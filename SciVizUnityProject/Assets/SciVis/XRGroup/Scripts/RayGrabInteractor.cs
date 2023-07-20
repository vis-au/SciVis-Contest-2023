using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SciVis.XRGroup.Scripts
{
    public class RayGrabInteractor : MRTKRayInteractor
    {
        // Start is called before the first frame update
        private Pose ourInitialLocalAttach = Pose.identity;
        private float ourRefDistance;
        new void Start()
        {
            float ourRefDistance = 10f;
        }

        // Update is called once per frame
        void Update()
        {
            // Use Pose Sources to calculate the interactor's pose and the attach transform's position
            // We have to make sure the ray interactor is oriented appropriately before calling
            // lower level raycasts
            if (AimPoseSource != null && AimPoseSource.TryGetPose(out Pose aimPose))
            {
                transform.SetPositionAndRotation(aimPose.position, aimPose.rotation);

                if (hasSelection)
                {
                    
                    float distanceRatio = GetDistanceToBody(aimPose) / ourRefDistance;
                    attachTransform.localPosition = new Vector3(ourInitialLocalAttach.position.x, ourInitialLocalAttach.position.y, ourInitialLocalAttach.position.z * distanceRatio);
                }
                else
                {
                    ourRefDistance = 10f;
                }
            }

            // Use the Device Pose Sources to calculate the attach transform's pose
            if (DevicePoseSource != null && DevicePoseSource.TryGetPose(out Pose devicePose))
            {
                attachTransform.rotation = devicePose.rotation;
            }
        }
        float GetDistanceToBody(Pose pose)
        {
            if (pose.position.y > Camera.main.transform.position.y)
            {
                return Vector3.Distance(pose.position, Camera.main.transform.position);
            }
            else
            {
                Vector2 headPosXZ = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
                Vector2 pointerPosXZ = new Vector2(pose.position.x, pose.position.z);

                return Vector2.Distance(pointerPosXZ, headPosXZ);
            }
        }

        public void SetRefDistance(float val)
        {
            this.ourRefDistance = val;
        }

        public float GetRefDistance()
        {
            return ourRefDistance;
        }
    }
}
