namespace TriLibCore.Fbx
{
    public enum FBXTangentVisibility
    {
        eTangentShowNone = 0x00000000,
        eTangentShowLeft = 0x00100000,
        eTangentShowRight = 0x00200000,
        eTangentShowBoth = eTangentShowLeft | eTangentShowRight
    }
}