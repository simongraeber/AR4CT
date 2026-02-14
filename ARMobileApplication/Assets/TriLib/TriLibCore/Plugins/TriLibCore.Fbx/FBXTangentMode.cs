namespace TriLibCore.Fbx
{
    public enum FBXTangentMode
    {
        eTangentAuto = 0x00000100,
        eTangentTCB = 0x00000200,
        eTangentUser = 0x00000400,
        eTangentGenericBreak = 0x00000800,
        eTangentBreak = eTangentGenericBreak | eTangentUser,
        eTangentAutoBreak = eTangentGenericBreak | eTangentAuto,
        eTangentGenericClamp = 0x00001000,
        eTangentGenericTimeIndependent = 0x00002000,
        eTangentGenericClampProgressive = 0x00004000 | eTangentGenericTimeIndependent
    }
}