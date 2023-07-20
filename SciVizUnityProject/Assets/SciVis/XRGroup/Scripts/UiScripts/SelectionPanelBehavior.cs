using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectionPanelBehavior : MonoBehaviour
{
  

    void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatedSelection(HashSet<int> brushedIndicies)
    {
        
    }

    public void MakeLegend()
    {
        BillBoardBuilder billBoardBuilder = GetComponentInChildren<BillBoardBuilder>();
        LegendMaker legendMaker = GetComponentInChildren<LegendMaker>();
        legendMaker.MakeBillBoardLegend(billBoardBuilder);
    }
}
