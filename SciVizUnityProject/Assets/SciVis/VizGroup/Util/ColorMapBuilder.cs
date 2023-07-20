using SciColorMaps;
using SciColorMaps.Portable;

public static class ColorMapBuilder {
    public static float TARGET_CALCIUM_LEVEL = 0.7f;

    public static bool UseDivergentColorScale(BrainSubject subject) {
        bool isCalciumEncodingActive = subject.GetSpec().NeuronColorEncoding.value == NeuronAttributeType.Calcium;
        bool useDivergentActive = subject.GetSpec().DivergentColorScale;
        return isCalciumEncodingActive && useDivergentActive;
    }
    public static ColorMap CreateDivergingColorMap(float min, float neutral, float max) {
        // colorbrewer rgb values for PiYG (green -> off-white -> pink)
        var colors = new [] {
            new byte[] {39, 100, 25},  // extreme green
            new byte[] {77, 146, 33},
            new byte[] {127, 188, 65},
            new byte[] {184, 225, 134},
            new byte[] {230, 245, 208},
            new byte[] {247, 247, 247},  // off-white
            new byte[] {253, 224, 239},
            new byte[] {241, 182, 218},
            new byte[] {222, 119, 174},
            new byte[] {197, 27, 125},
            new byte[] {142, 1, 82},  // extreme pink
        };

        float normalizedNeutral = (neutral - min) / (max - min);

        // divide the steps between min and neutral and neutral and max into equal-sized bins per
        // "half" of the color scale
        var positions = new float[] {
            0f,
            normalizedNeutral / 5,
            (normalizedNeutral / 5) * 2,
            (normalizedNeutral / 5) * 3,
            (normalizedNeutral / 5) * 4,
            normalizedNeutral,
            normalizedNeutral + (1 - normalizedNeutral) / 5,
            normalizedNeutral + ((1 - normalizedNeutral) / 5) * 2,
            normalizedNeutral + ((1 - normalizedNeutral) / 5) * 3,
            normalizedNeutral + ((1 - normalizedNeutral) / 5) * 4,
            1f
        };

        // see https://github.com/ar1st0crat/SciColorMaps
        return ColorMap.CreateFromColors(colors, positions, min, max);
    }

    public static ColorMap CreateSequentialColorMap(float min, float max) {
        return new ColorMap("viridis", min, max);
    }

}