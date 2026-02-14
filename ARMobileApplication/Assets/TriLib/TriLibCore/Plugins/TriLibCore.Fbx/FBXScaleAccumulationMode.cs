namespace TriLibCore.Fbx
{
    public enum FBXScaleAccumulationMode
    {
        eScaleMultiply, //Independent XYZ scale values per layer are calculated using the layer weight value as an exponent,
                        //and result scale curves are calculated by multiplying each independent XYZ scale value.
        eScaleAdditive //Result scale curves are calculated by adding each independent XYZ value.
    }
}