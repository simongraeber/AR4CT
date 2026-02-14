namespace TriLibCore.Fbx
{
    public enum FBXWeightedMode
    {
        eWeightedNone = 0x00000000,
        eWeightedRight = 0x01000000,
        eWeightedNextLeft = 0x02000000,
        eWeightedAll = eWeightedRight | eWeightedNextLeft
    }
}