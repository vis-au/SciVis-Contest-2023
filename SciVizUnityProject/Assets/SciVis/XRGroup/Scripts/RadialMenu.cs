using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

public class RadialMenu : MonoBehaviour
{
    private RadialMenuItems menuItems;
    public GameObject divider;
    public GameObject IconPrefab;
    public Transform rightHand;
    private float anglePerItem;
    private Dictionary<int, GameObject> indexToIcon;
    private GameObject lastHovered;
    void Start()
    {
        indexToIcon = new Dictionary<int, GameObject>();
        menuItems = GetComponent<RadialMenuItems>();
        int numItems = menuItems.Items.Count;
        anglePerItem = 360f / numItems;
        float currentAngle = 0f;
        int index = 0;
        foreach (var item in menuItems.Items)
        {
            //Make divider
            GameObject div = Instantiate(divider, transform);
            div.transform.Rotate(Vector3.forward,currentAngle);
            div.transform.position = transform.position + transform.forward * 0.01f*transform.localScale.x + div.transform.up * 0.3f*transform.localScale.x;
            
            //Make Icon
            GameObject icon = Instantiate(IconPrefab, transform);
            float iconAngle = currentAngle + 0.5f * anglePerItem;
            icon.transform.localPosition = new Vector3(0, 0, 0.02f) + Vector3.up * 0.3f;
            icon.transform.RotateAround(transform.position,transform.forward,iconAngle);
            icon.transform.localRotation = Quaternion.identity;
            icon.GetComponentInChildren<Image>().sprite = item.Key;
            icon.GetComponent<RectTransform>().rect.Set(0,0,10,10);

            indexToIcon.Add(index, icon);
            index++;
            currentAngle += anglePerItem;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (rightHand == null)
        {
            return;
        }
        RaycastHit hit;
        if (Physics.Raycast(rightHand.position, rightHand.forward, out hit,200,layerMask:LayerMask.GetMask("UI")))
        {
            Vector3 hitPos = hit.point;
            Vector3 directionVector = hitPos - transform.position;
            Debug.DrawLine(rightHand.position,hitPos,Color.blue,2);
            Debug.DrawLine(transform.position, hitPos,Color.red,2);
            float angle = Vector3.SignedAngle(transform.up, directionVector, transform.forward);
            if (angle < 0)
            {
                angle += 360;
            }

            int hoveredIndex = (int) (angle / anglePerItem);
            GameObject hoveredIcon = indexToIcon[hoveredIndex];
            handleHover(hoveredIcon, directionVector);
        }
    }

    private void handleHover(GameObject hoveredIcon, Vector3 directionVector)
    {
        if (lastHovered != null)
        {
            lastHovered.transform.localPosition -= Vector3.forward * 0.02f;
            lastHovered.GetComponentInChildren<Image>().color = Color.black;
        }
        if (directionVector.magnitude < 0.06f)
        {
            lastHovered = null;
            return;
        }
            
        hoveredIcon.transform.localPosition += Vector3.forward*0.02f;
        hoveredIcon.GetComponentInChildren<Image>().color = Color.blue;
            

        lastHovered = hoveredIcon;
    }

    public void ExecuteSelected()
    {
        if (lastHovered == null)
        {
            return;
        }

        Sprite sprite = lastHovered.GetComponentInChildren<Image>().sprite;
        menuItems.Items[sprite].Invoke();
    }
}
