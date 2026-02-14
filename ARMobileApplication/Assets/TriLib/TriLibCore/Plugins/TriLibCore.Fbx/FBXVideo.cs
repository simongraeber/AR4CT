using System.IO;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXVideo : FBXObject
    {
        public Color Color; 
        public long ClipIn; 
        public long ClipOut; 
        public bool Mute; 
        public bool ImageSequence; 
        public int ImageSequenceOffset; 
        public float FrameRate;
        public int LastFrame; 
        public int Width; 
        public int Height; 
        public int StartFrame;
        public int StopFrame;
        public string Path; 
        public string RelPath; 
        public float PlayOffset;
        public long Offset;
        public FBXInterlaceMode InterlaceMode;
        public bool FreeRunning;
        public bool Loop;
        public FBXAccessMode AccessMode;

        public string Type;
        public bool UseMipMap;
        public string Filename;
        public string RelativeFilename;
        public string ResolvedFilename;
        public byte[] Content;
        public Stream ContentStream;

        public FBXVideo(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Video;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.VideoDefinition != null)
            {
                Color = Document.VideoDefinition.Color;
                ClipIn = Document.VideoDefinition.ClipIn;
                ClipOut = Document.VideoDefinition.ClipOut;
                Mute = Document.VideoDefinition.Mute;
                ImageSequence = Document.VideoDefinition.ImageSequence;
                ImageSequenceOffset = Document.VideoDefinition.ImageSequenceOffset;
                FrameRate = Document.VideoDefinition.FrameRate;
                LastFrame = Document.VideoDefinition.LastFrame;
                Width = Document.VideoDefinition.Width;
                Height = Document.VideoDefinition.Height;
                StartFrame = Document.VideoDefinition.StartFrame;
                StopFrame = Document.VideoDefinition.StopFrame;
                Path = Document.VideoDefinition.Path;
                RelPath = Document.VideoDefinition.RelPath;
                PlayOffset = Document.VideoDefinition.PlayOffset;
                Offset = Document.VideoDefinition.Offset;
                InterlaceMode = Document.VideoDefinition.InterlaceMode;
                FreeRunning = Document.VideoDefinition.FreeRunning;
                Loop = Document.VideoDefinition.Loop;
                AccessMode = Document.VideoDefinition.AccessMode;
            }
        }
    }
}