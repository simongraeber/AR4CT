namespace TriLibCore.Fbx
{
    public enum FBXAnimationLayerBlendMode
    {
        eBlendAdditive, //The layer "adds" its animation to layers that precede it in the stack and affect the same attributes.
        eBlendOverride, //The layer "overrides" the animation of any layer that shares the same attributes and precedes it in the stack.
        eBlendOverridePassthrough //This mode is like the eOverride but the Weight value influence how much animation from the preceding layers is allowed to pass-through. When using this mode with a Weight of 100.0, this layer is completely opaque and it masks any animation from the preceding layers for the same attribute. If the Weight is 50.0, half of this layer animation is mixed with half of the animation of the preceding layers for the same attribute.
    }
}