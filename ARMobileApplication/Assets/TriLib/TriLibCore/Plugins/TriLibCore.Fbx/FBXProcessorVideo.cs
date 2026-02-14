using System;
using System.Collections.Generic;
using System.IO;
using TriLibCore.Utils;


namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _Path_token = 6774539739449613738;
        private const long _RelPath_token = -5513532510879844167;
        private const long _ClipIn_token = -1367968408755256102;
        private const long _ClipOut_token = -5513532523993829831;
        private const long _ImageSequence_token = -5853625260039281065;
        private const long _ImageSequenceOffset_token = -7085945247453095478;
        private const long _FrameRate_token = -4289203686847013496;
        private const long _LastFrame_token = -4289199021067434478;
        private const long _Width_token = 7096547112139646177;
        private const long _Height_token = -1367968408618582676;
        private const long _StartFrame_token = -3837760350564031504;
        private const long _StopFrame_token = -4289192531755035610;
        private const long _PlayOffset_token = -3837846487376353620;
        private const long _Offset_token = -1367968408417333032;
        private const long _InterlaceMode_token = -5812684953502036015;
        private const long _FreeRunning_token = -8300811447135437682;
        private const long _Loop_token = 6774539739449507881;
        private const long _AccessMode_token = -3838250719411039508;
        private const long _Content_token = -5513532523903184684;
        private const long _comma_token = 34902897112120551;


        private FBXVideo ProcessVideo(FBXNode node, long objectId, string name, string objectClass)
        {
            var video = new FBXVideo(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _Path_token:
                                video.Path = property.Properties.GetStringValue(4, false);
                                break;
                            case _RelPath_token:
                                video.RelPath = property.Properties.GetStringValue(4, false);
                                break;
                            case _Color_token:
                                video.Color = property.Properties.GetColorValue(4);
                                break;
                            case _ClipIn_token:
                                video.ClipIn = property.Properties.GetLongValue(4);
                                break;
                            case _ClipOut_token:
                                video.ClipOut = property.Properties.GetLongValue(4);
                                break;
                            case _Mute_token:
                                video.Mute = property.Properties.GetBoolValue(4);
                                break;
                            case _ImageSequence_token:
                                video.ImageSequence = property.Properties.GetBoolValue(4);
                                break;
                            case _ImageSequenceOffset_token:
                                video.ImageSequenceOffset = property.Properties.GetIntValue(4);
                                break;
                            case _FrameRate_token:
                                video.FrameRate = property.Properties.GetIntValue(4);
                                break;
                            case _LastFrame_token:
                                video.LastFrame = property.Properties.GetIntValue(4);
                                break;
                            case _Width_token:
                                video.Width = property.Properties.GetIntValue(4);
                                break;
                            case _Height_token:
                                video.Height = property.Properties.GetIntValue(4);
                                break;
                            case _StartFrame_token:
                                video.StartFrame = property.Properties.GetIntValue(4);
                                break;
                            case _StopFrame_token:
                                video.StopFrame = property.Properties.GetIntValue(4);
                                break;
                            case _PlayOffset_token:
                                video.PlayOffset = property.Properties.GetFloatValue(4);
                                break;
                            case _Offset_token:
                                video.Offset = property.Properties.GetLongValue(4);
                                break;
                            case _InterlaceMode_token:
                                video.InterlaceMode = (FBXInterlaceMode)property.Properties.GetIntValue(4);
                                break;
                            case _FreeRunning_token:
                                video.FreeRunning = property.Properties.GetBoolValue(4);
                                break;
                            case _Loop_token:
                                video.Loop = property.Properties.GetBoolValue(4);
                                break;
                            case _AccessMode_token:
                                video.AccessMode = (FBXAccessMode)property.Properties.GetIntValue(4);
                                break;
                        }
                    }
                }
            }
            var filename = node?.GetNodeByName(_Filename_token);
            if (filename != null)
            {
                video.Filename = filename.Properties.GetStringValue(0, false);
            }
            var relativeFilename = node?.GetNodeByName(_RelativeFilename_token);
            if (relativeFilename != null)
            {
                video.RelativeFilename = relativeFilename.Properties.GetStringValue(0, false);
            }
            var type = node?.GetNodeByName(_Type_token);
            if (type != null)
            {
                video.Type = type.Properties.GetStringValue(0, false);
            }
            var content = node?.GetNodeByName(_Content_token);
            if (content != null)
            {
                if (content.Properties.IsASCII)
                {
                    List<byte> bytes = null;
                    for (var i = 0; i < content.Properties.ArrayLength; i++)
                    {
                        if (i >= content.Properties.Values.Count)
                        {
                            break;
                        }
                        var base64Data = content.Properties.GetStringValue(i, false);
                        var data = Convert.FromBase64String(base64Data);
                        bytes = new List<byte>(data.Length);
                        bytes.AddRange(data);
                        break;
                    }
                    if (bytes != null)
                    {
                        var contentStream = new MemoryStream();
                        bytes.ToMemoryStream(ref contentStream);
                        video.ContentStream = contentStream;
                    }
                }
                else
                {
                    if (!FileUtils.TrySaveFileAtPersistentDataPath(
                            Reader.AssetLoaderContext,
                            video.Name,
                            video.Filename ?? video.RelativeFilename,
                            new PropertyAccessorIEnumerator<byte>(content.Properties.GetByteValues()),
                            out video.ResolvedFilename))
                    {
                        var contentStream = new MemoryStream();
                        content.Properties.GetByteValues().ToMemoryStream(ref contentStream);
                        video.ContentStream = contentStream;
                    }
                }
            }
            return video;
        }

        private static void DecodeBase64Texture(IEnumerator<byte> input, string filename)
        {
            var base64decoder = new Base64Decoder();
            using (var fileWriter = new BinaryWriter(File.Create(filename)))
            {
                while (input.MoveNext())
                {
                    if (base64decoder.DecodeByte(input.Current, out var decoded))
                    {
                        fileWriter.Write(decoded);
                    }
                }
            }
        }
    }
}
