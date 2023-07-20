using SciVis.XRGroup.Scripts;
using UnityEngine;

namespace SciVis.XRGroup
{
    public class BrainSelectionManager : MonoBehaviour
    {
        //private PlayAreaUtil selected;
        private BrainSelectionInteractable selected;
        private int cubeIDcounter;

        private void Start()
        {
            cubeIDcounter = 4;
        }

        public void Select(BrainSelectionInteractable viz)
        {
            if (selected != null)
            {
                selected.DeSelect();
            }

            selected = viz;
            if (viz != null)
            {
                viz.Select();
            }
        }

        public void IncrementCubeCounter()
        {
            cubeIDcounter++;
        }
        public int GetCubeCounter()
        {
            return cubeIDcounter;
        }
    }
}
