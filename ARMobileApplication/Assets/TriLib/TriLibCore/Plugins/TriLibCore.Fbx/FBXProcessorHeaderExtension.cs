using System;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _FBXHeaderExtension_token = 7835892614375777819;
        private const long _FBXVersion_token = -3838146983056387135;
        private const long _CreationTimeStamp_token = 1966388212080155506;
        private const long _Version_token = -5513532507323238093;
        private const long _Year_token = 6774539739449885122;
        private const long _Month_token = 7096547112130599323;
        private const long _Day_token = -3351804022671227529;
        private const long _Hour_token = 6774539739449388905;
        private const long _Minute_token = -1367968408471580071;
        private const long _Second_token = -1367968408303832903;
        private const long _Millisecond_token = -8295305822553764516;
        private const long _OriginalApplicationName_token = 7210582388936853685;
        private const long _LastSavedApplicationName_token = -9215154955804053749;
        private const long _SceneInfo_token = -4289193008374350603;
        private void ProcessHeaderExtension(FBXNode node)
        {
            var headerExtension = node.GetNodeByName(_FBXHeaderExtension_token);
            if (headerExtension!= null)
            {
                var fbxVersion = headerExtension.GetNodeByName(_FBXVersion_token);
                if (fbxVersion!= null)
                {
                    Document.Version = fbxVersion.Properties.GetIntValue(0);
                    if (Document.Version < 7000)
                    {
                        throw new Exception("Only files generated with FBX SDK 7.0 onwards can be loaded.");
                    }
                }
                var sceneInfo = headerExtension.GetNodeByName(_SceneInfo_token);
                if (sceneInfo!= null)
                {
                    var properties = sceneInfo.GetNodeByName(PropertiesName);
                    if (properties!= null && properties.HasSubNodes)
                    {
                        foreach (var property in properties)
                        {
                            var propertyName = property.Properties.GetStringHashValue(0);
                            switch (propertyName)
                            {
                                case _OriginalApplicationName_token:
                                case _LastSavedApplicationName_token:
                                    Document.OriginalApplicationName = property.Properties.GetStringValue(4, false);
                                    break;
                            }
                        }
                    }
                }
                var creationTimeStamp = headerExtension.GetNodeByName(_CreationTimeStamp_token);
                if (creationTimeStamp!= null)
                {
                    var timeStampString = "";
                    var version = creationTimeStamp.GetNodeByName(_Version_token);
                    if (version!= null)
                    {
                        timeStampString += version.Properties.GetIntValue(0);
                    }
                    var year = creationTimeStamp.GetNodeByName(_Year_token);
                    if (year!= null)
                    {
                        timeStampString += "-" + year.Properties.GetIntValue(0);
                    }
                    var month = creationTimeStamp.GetNodeByName(_Month_token);
                    if (month!= null)
                    {
                        timeStampString += "-" + month.Properties.GetIntValue(0);
                    }
                    var day = creationTimeStamp.GetNodeByName(_Day_token);
                    if (day!= null)
                    {
                        timeStampString += "-" + day.Properties.GetIntValue(0);
                    }
                    var hour = creationTimeStamp.GetNodeByName(_Hour_token);
                    if (hour!= null)
                    {
                        timeStampString += "-" + hour.Properties.GetIntValue(0);
                    }
                    var minute = creationTimeStamp.GetNodeByName(_Minute_token);
                    if (minute!= null)
                    {
                        timeStampString += "-" + minute.Properties.GetIntValue(0);
                    }
                    var second = creationTimeStamp.GetNodeByName(_Second_token);
                    if (second!= null)
                    {
                        timeStampString += "-" + second.Properties.GetIntValue(0);
                    }
                    var millisecond = creationTimeStamp.GetNodeByName(_Millisecond_token);
                    if (millisecond!= null)
                    {
                        timeStampString += "-" + millisecond.Properties.GetIntValue(0);
                    }
                    Reader.AssetLoaderContext.ModificationDate = timeStampString;
                }
            }
        }
    }
}