using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
using Random = System.Random;

public class BrainLoader : MonoBehaviour
{
    // Use Unity Text assets to import text data (e.g. csv, tsv etc.)
    private TextAsset myDataSource;
    private CSVDataSource csvds;
    private ViewBuilder vb;
    void Start()
    {
        csvds = Object.FindObjectOfType<CSVDataSource>();
        // Create a view builder with the point topology
        vb = new ViewBuilder(MeshTopology.Points, "Brain")
            .initialiseDataView(csvds.DataCount)
            .setDataDimension(csvds["x"].Data, ViewBuilder.VIEW_DIMENSION.X)
            .setDataDimension(csvds["y"].Data, ViewBuilder.VIEW_DIMENSION.Y)
            .setDataDimension(csvds["z"].Data, ViewBuilder.VIEW_DIMENSION.Z);
            
                     
        // Use the "IATKUtil" class to get the corresponding Material mt 
        Material mt = IATKUtil.GetMaterialFromTopology(AbstractVisualisation.GeometryType.Spheres);
        mt.SetFloat("_Size", 0.03f);
        mt.SetFloat("_MinSize", 0.03f);

        // Create a view builder with the point topology
        View view = vb.updateView().apply(gameObject, mt);
    }

}
