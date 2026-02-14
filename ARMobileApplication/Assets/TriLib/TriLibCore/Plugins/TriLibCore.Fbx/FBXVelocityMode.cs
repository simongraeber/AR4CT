namespace TriLibCore.Fbx
{
    public enum FBXVelocityMode
    {
        eVelocityNone = 0x00000000,
        eVelocityRight = 0x10000000,
        eVelocityNextLeft = 0x20000000,
        eVelocityAll = eVelocityRight | eVelocityNextLeft
    }
}