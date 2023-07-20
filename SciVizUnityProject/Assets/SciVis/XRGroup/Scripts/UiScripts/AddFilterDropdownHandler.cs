using System.Collections.Generic;
using UnityEngine;

namespace SciVis.XRGroup.Scripts
{
    public class AddFilterDropdownHandler : DropDownClickHandler
    {

        private FilterMenuBehaviour filterMenu;
        public override void OnValueChosen(NeuronAttributeType attribute)
        {
            if (filterMenu == null)
            {
                filterMenu = gameObject.GetComponentInParent<FilterMenuBehaviour>();
            }

            if (attribute == NeuronAttributeType.None)
            {
                return;
            }
            NeuronAttribute att = new NeuronAttribute{value = attribute};
            (float min, float max) = att.GetMinMax();
            NeuronFilter filter = new NeuronFilter(att,min,max); 
            filterMenu.AddFilter(filter);
        }
        public override void OnValueChosen(AggregationType attribute){
            // Do nothing
        }
        public override void OnValueChosen(int attribute){
            // Do nothing
        }
    }
}
