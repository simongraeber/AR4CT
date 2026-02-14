using TriLibCore.Dae.Schema;

namespace TriLibCore.Dae
{
    public class DaeAnimatedMatrices : DaeMatrices
    {
        public void Reset(DaeModel model)
        {
            ClearMatrices();
            foreach (var kvp in model.Matrices)
            {
                var elementName = model.Matrices.ElementNames[kvp.Key];
                AddMatrix(elementName, kvp.Key, kvp.Value);
            }
        }

        public void UpdateValues(string property, string subProperty, float[] value)
        {
            if (TryGetValue(property, out var matrix))
            {
                var elementName = ElementNames[property];
                switch (elementName)
                {
                    case ItemsChoiceType7.lookat:
                        break;
                    case ItemsChoiceType7.matrix:
                        if (subProperty == null)
                        {
                            for (var i = 0; i < 16; i++)
                            {
                                matrix[i] = value[i];
                            }
                        }
                        break;
                    case ItemsChoiceType7.rotate:
                        if (subProperty == "ANGLE")
                        {
                            matrix[3] = value[0];
                        }
                        break;
                    case ItemsChoiceType7.skew:
                        break;
                    case ItemsChoiceType7.scale:
                    case ItemsChoiceType7.translate:
                        switch (subProperty)
                        {
                            case "X":
                                matrix[0] = value[0];
                                break;
                            case "Y":
                                matrix[1] = value[1];
                                break;
                            case "Z":
                                matrix[2] = value[2];
                                break;
                            case null:
                                matrix[0] = value[0];
                                matrix[1] = value[1];
                                matrix[2] = value[2];
                                break;
                        }
                        break;
                }
                this[property] = matrix;
            }
        }
    }
}