using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using SciVis.XRGroup;
using SciVis.XRGroup.Scripts;
using SciVis.XRGroup.Scripts.Tools;
using UnityEngine;

namespace Assets.SciVis.XRGroup
{
    public class CubeSelectMenuBehavior : MonoBehaviour
    {
        // Specification of cube
        private TimeCapsuleUtil tcu;
        private Specification spec;
        private NeuronAttribute sizeEncoding;
        private NeuronAttribute colorEncoding;
        private List<NeuronFilter> filters;
        private int timestamp;
        private SimulationType simID;
        private int playAreaCounter;
        private PressableButton[] buttons;

        // Menu components
        private GameObject cubeInfo;


        private GameObject playAreaPrefab;

        // Start is called before the first frame update
        void Start()
        {
            playAreaCounter = 0;
            playAreaPrefab = Resources.Load<GameObject>("Prefabs/World Objects/PlayAreaFlex");
            tcu = gameObject.GetComponentInParent<TimeCapsuleUtil>();
            spec = tcu.GetSpecification();
            sizeEncoding = spec.NeuronSizeEncoding;
            colorEncoding = spec.NeuronColorEncoding;
            filters = spec.Filters;
            timestamp = spec.NeuronTimeStep;
            simID = spec.SimulationId;
            cubeInfo = transform.Find("Canvas/CubeInfo").gameObject;

            SetCubeInfo("ID: "+tcu.GetCubeID()+" Timestamp: "+timestamp+" SimNr: "+simID);

            buttons = gameObject.GetComponentsInChildren<PressableButton>();
            SetButtonActions();
            transform.Find("Canvas/Encodings/ColorText").gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).SetText("Color: "+colorEncoding);
            transform.Find("Canvas/Encodings/SizeText").gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).SetText("Size: "+sizeEncoding);
            transform.Find("Canvas/Filters/FilterText").gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).SetText(filters.Count+" filters added");
        }
        public void SetCubeInfo(string value)
        {
            cubeInfo.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).SetText(value);
        }
        // Update is called once per frame
        void Update()
        {

        }
        private void SetButtonActions()
        {
            // New play area button
            buttons[0].OnClicked.AddListener(()=>
            {
                SpawnPlayArea();
            });
            // Delete cube
            buttons[1].OnClicked.AddListener(()=>
            {
                DeleteCube();
            });
        }

        private void SpawnPlayArea()
        {
            Transform playAreas = GameObject.FindWithTag("PlayArea").transform;
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y,transform.position.z);
            tcu.gameObject.SetActive(false);

            PlayAreaUtil[] playAreaUtils = playAreas.GetComponentsInChildren<PlayAreaUtil>(true);
            bool foundUnusedPlayArea = false;
            for (int i = 0; i < playAreaUtils.Length; i++)
            {
                PlayAreaUtil pa = playAreaUtils[i];
                if (pa.inUse)
                {
                    continue;
                }
                
                pa.inUse = true;
                pa.LoadAll(spec);
                Transform brainTransform = pa.GetComponentInChildren<VisualizationHandler>(true).transform;
                brainTransform.position = spawnPos;         
                brainTransform.rotation = Quaternion.Euler(-90,180,0);
                foundUnusedPlayArea = true;
                //pa.GetComponentInChildren<BrushingAndLinking2>(true).SetBrushedIndices(spec.BrushedIndicies, true);
                break;
            }

            if (!foundUnusedPlayArea)
            {
                GameObject playArea = Instantiate(playAreaPrefab, spawnPos, Quaternion.identity,playAreas);
                PlayAreaUtil util = playArea.GetComponent<PlayAreaUtil>();
                util.LoadAll(spec);
                util.inUse = true;
                //util.GetComponentInChildren<BrushingAndLinking2>(true).SetBrushedIndices(spec.BrushedIndicies, true);
            }

            playAreaCounter++;

            DeleteCube();
        }
        
        private void DeleteCube()
        {
            tcu.gameObject.Destroy();
        }
        
    }
}