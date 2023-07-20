using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;

public class EncodingMenuBehaviour : MonoBehaviour
{
    private GameObject colorDropdown;
    private GameObject sizeDropdown;
    private PressableButton checkBox;
    private GameObject divergentColorUI;
    private PressableButton divergentColorScaleCheckBox;
    private BrainSubject brainSubject;
    // Start is called before the first frame update
    void Awake()
    {
        colorDropdown = transform.Find("Canvas/Vertical/ColorHorizontal/ColorAttribute").gameObject;
        sizeDropdown = transform.Find("Canvas/Vertical/SizeHorizontal/SizeAttribute").gameObject;
        checkBox = transform.Find("Canvas/Vertical/checkBoxHorizontal/CheckBox").GetComponent<PressableButton>();
        checkBox.OnClicked.AddListener(() => checkBoxClicked());
        divergentColorUI = transform.Find("Canvas/Vertical/checkBoxHorizontalDivergentColor").gameObject;
        divergentColorScaleCheckBox = divergentColorUI.GetComponentInChildren<PressableButton>(true);
        divergentColorScaleCheckBox.OnClicked.AddListener(()=>divergentClicked());
        brainSubject = GetComponentInParent<BrainSubject>();
        }

    private void divergentClicked()
    {
        brainSubject.SetDivergentColorScale(divergentColorScaleCheckBox.IsToggled);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void checkBoxClicked()
    {
        brainSubject.SetLocalColorScale(checkBox.IsToggled);
    }

    public void SetColorDropdownValue(NeuronAttributeType value)
    {
        if (colorDropdown == null)
        {
            colorDropdown = transform.Find("Canvas/Vertical/ColorHorizontal/ColorAttribute").gameObject;
        }
        colorDropdown.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
        bool shouldShowDivergentUI = value.ToString().Equals("Calcium");
        if (divergentColorUI == null)
        {
            divergentColorUI = transform.Find("Canvas/Vertical/checkBoxHorizontalDivergentColor").gameObject;
        }
        divergentColorUI.SetActive(shouldShowDivergentUI);
        brainSubject ??= GetComponentInParent<BrainSubject>();
        brainSubject.SetNeuronColorEncoding(new NeuronAttribute(){value = value});
    }
    
    public void SetSizeDropdownValue(NeuronAttributeType value)
    {
        if (sizeDropdown == null)
        {
            sizeDropdown = transform.Find("Canvas/Vertical/SizeHorizontal/SizeAttribute").gameObject;
        }
        sizeDropdown.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
        brainSubject ??= GetComponentInParent<BrainSubject>();
        brainSubject.SetNeuronSizeEncoding(new NeuronAttribute(){value = value});
    }

    public void SetLocalColorScale(bool local)
    {
        if (checkBox == null)
        {
            checkBox = transform.Find("Canvas/Vertical/checkBoxHorizontal/CheckBox").GetComponent<PressableButton>();
        }
        checkBox.ForceSetToggled(local);
    }

    public void SetDivergentColorScale(bool divergent)
    {
        if (divergentColorScaleCheckBox == null)
        {
            divergentColorUI = transform.Find("Canvas/Vertical/checkBoxHorizontalDivergentColor").gameObject;
            divergentColorScaleCheckBox = divergentColorUI.GetComponentInChildren<PressableButton>(true);
        }
        divergentColorScaleCheckBox.ForceSetToggled(divergent);
    }
}
