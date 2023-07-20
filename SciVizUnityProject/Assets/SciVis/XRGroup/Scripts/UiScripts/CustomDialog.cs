using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;

public class CustomDialog : Dialog
{
    [SerializeField]
    [Tooltip("The button representing the format pain action. If specified by the user, " +
             "the button will be enabled and activated, with actions hooked up through code.")]
    private PressableButton formatPaintButton;
    private Action<DialogButtonEventArgs> formatPaintAction = null;
    // Start is called before the first frame update
    
    public IDialog SetFormatPaintButton(string label, Action<DialogButtonEventArgs> action)
    {
        if (label == null) { return this; }
        formatPaintAction = action ?? ((args) => {});
        return this;
    }

    protected override void Awake()
    {
        base.Awake();
        formatPaintButton.OnClicked.AddListener( () => {
            var args = new DialogButtonEventArgs() {
                ButtonType = DialogButtonType.Other,
                ButtonText = "Format paint",
                Dialog = this
            };
            formatPaintAction.Invoke(args);
            Dismiss();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
