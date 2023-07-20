using SciColorMaps.Portable;
using TMPro;
using UnityEngine;

public class LegendMaker : MonoBehaviour
{
    [SerializeField] private LineRenderer gradientColorLineRenderer;

    [SerializeField] private LineRenderer gradientColorLineRenderer2;

    [SerializeField] private LineRenderer gradientColorLineRenderer3;

    public TextMeshPro Legend;


    private void SetGradientColor(Gradient gradient, LineRenderer lineRenderer)
    {
        var vertices = new Vector3[gradient.colorKeys.Length];
        var colorKeys = gradient.colorKeys;
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(colorKeys[i].time, 0f, 0f);
        lineRenderer.positionCount = vertices.Length;
        lineRenderer.SetPositions(vertices);
        lineRenderer.colorGradient = gradient;
    }

    private GradientColorKey[] ComputeGradientColorKeys(ColorMap colorMap, (float, float) domain)
    {
        const int nBins = 8; // 8 is the maximum number of colors possible for Gradient
        var min = domain.Item1 == domain.Item2 ? 0 : domain.Item1;
        var max = domain.Item1 == domain.Item2 ? 1 : domain.Item2;

        var gradientColorKeys = new GradientColorKey[nBins];
        var binSize = (max - min) / (nBins - 1);

        for (var i = 0; i < nBins; i++)
        {
            var bin = min + i * binSize;
            gradientColorKeys[i] = new GradientColorKey(GetColor(bin, colorMap), bin);
        }

        return gradientColorKeys;
    }

    private void UpdateSelectionColorLegend(VisualizationHandler visualizationHandler)
    {
        var selectionGradient = new Gradient();
        // TODO: this uses a local color scale, but we should probably link to the actual one
        selectionGradient.colorKeys = ComputeGradientColorKeys(new ColorMap("reds"), (0f, 1f));
        SetGradientColor(selectionGradient, gradientColorLineRenderer2);
    }

    private void UpdateNeuronsColorLegend(VisualizationHandler visualizationHandler)
    {
        // colormap may not be set, yet. In that case, do nothing.
        if (visualizationHandler.NeuronsColorMap == null) return;

        var neuronsGradient = new Gradient();
        var neuronsDomain = visualizationHandler.NeuronsColorDomain;
        var neuronsColorMap = visualizationHandler.NeuronsColorMap;
        neuronsGradient.colorKeys = ComputeGradientColorKeys(neuronsColorMap, neuronsDomain);
        SetGradientColor(neuronsGradient, gradientColorLineRenderer);
    }

    private void UpdateSynapsesColorLegend(VisualizationHandler visualizationHandler)
    {
        // colormap may not be set, yet. In that case, do nothing.
        if (visualizationHandler.SynapsesColorMap == null) return;

        var synapsesGradient = new Gradient();
        var synapsesDomain = visualizationHandler.SynapsesColorDomain;
        var synapsesColorMap = visualizationHandler.SynapsesColorMap;
        synapsesGradient.colorKeys = ComputeGradientColorKeys(synapsesColorMap, synapsesDomain);
        SetGradientColor(synapsesGradient, gradientColorLineRenderer3);
    }

    public void MakeBrainLegend(VisualizationHandler visualizationHandler)
    {
        gradientColorLineRenderer.gameObject.SetActive(true);
        gradientColorLineRenderer2.gameObject.SetActive(true);
        gradientColorLineRenderer3.gameObject.SetActive(true);

        UpdateSelectionColorLegend(visualizationHandler);
        UpdateNeuronsColorLegend(visualizationHandler);
        UpdateSynapsesColorLegend(visualizationHandler);
    }

    public void MakeTerrainLegend(TerrainViewBuilder terrainViewBuilder)
    {
        gradientColorLineRenderer.gameObject.SetActive(true);
        gradientColorLineRenderer2.gameObject.SetActive(true);

        // Get gradient from terrainView
        var gradient = terrainViewBuilder.gradient;
        var gradientSel = terrainViewBuilder.gradientSelection;

        SetGradientColor(gradient, gradientColorLineRenderer);
        SetGradientColor(gradientSel, gradientColorLineRenderer2);
    }

    private Color GetColor(float colorValue, ColorMap colorMap)
    {
        var colorBytes = colorMap[colorValue];
        return new Color(colorBytes[0] / 255f, colorBytes[1] / 255f, colorBytes[2] / 255f, 1.0f);
    }

    public void MakeBillBoardLegend(BillBoardBuilder billBoardBuilder)
    {
        gradientColorLineRenderer.gameObject.SetActive(true);

        // Get gradient from BillBoard
        var gradients = billBoardBuilder.gradients;

        SetGradientColor(gradients[1], gradientColorLineRenderer);
        SetGradientColor(gradients[0], gradientColorLineRenderer2);
        SetGradientColor(gradients[2], gradientColorLineRenderer3);
    }
}