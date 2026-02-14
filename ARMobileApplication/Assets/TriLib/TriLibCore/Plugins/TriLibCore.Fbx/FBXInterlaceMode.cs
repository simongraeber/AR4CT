namespace TriLibCore.Fbx
{
    public enum FBXInterlaceMode
    {
        None,       // Progressive frame (full frame)
        Fields,     // Alternate even/odd fields
        HalfEven,   // Half of a frame, even fields only
        HalfOdd,    // Half of a frame, odd fields only
        FullEven,   // Extract and use the even field of a full frame
        FullOdd,    // Extract and use the odd field of a full frame
        FullEvenOdd, // Extract Fields and make full frame with each one beginning with Even (60fps)
        FullOddEven
    }
}