namespace TriLibCore.Fbx
{
    public enum FBXRotationAccumulationOrder
    {
        eRotationByLayer, //Rotation values are weighted per layer and the result rotation curves are calculated using concatenated quaternion values.
        eRotationByChannel //Rotation values are weighted per component and the result rotation curves are calculated by adding each independent Euler XYZ value.
    }
}