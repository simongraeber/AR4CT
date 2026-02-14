namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _GlobalSettings_token = 9074070111337867307;
        private const long _UnitScaleFactor_token = 2593542748670334064;
        private const long _UpAxis_token = -1367968408237420255;
        private const long _UpAxisSign_token = -3837711758236767618;
        private const long _OriginalUpAxis_token = 6078093276137017682;
        private const long _OriginalUpAxisSign_token = -5207438113299715537;
        private const long _FrontAxis_token = -4289203674379964667;
        private const long _FrontAxisSign_token = -8077899038780393886;
        private const long _CoordAxis_token = -4289206315491179055;
        private const long _CoordAxisSign_token = 7929723365106337582;
        private const long _TimeMode_token = -4898811115266539243;
        private const long _CustomFrameRate_token = 4101781551444926679;
        private const long _TimeSpanStart_token = 2718799556408863494;
        private const long _TimeSpanStop_token = 1277815732381547742;
        private const long _OtherFlags_token = -3837865928059360484;
        private const long _TCDefinition_token = 1245586349863366023;

        private void ProcessGlobalSettings(FBXNode node)
        {
            Document.GlobalSettings = new FBXGlobalSettings
            {
                UnitScaleFactor = 1f,
                UpAxis = 1,
                UpAxisSign = 1,
                FrontAxis = 2,
                FrontAxisSign = 1,
                CoordAxis = 0,
                CoordAxisSign = 1
            };
            var globalSettings = node.GetNodeByName(_GlobalSettings_token);
            if (globalSettings == null)
            {
                var objectsNode = node.GetNodeByName(_Objects_token);
                if (objectsNode != null)
                {
                    globalSettings = objectsNode.GetNodeByName(_GlobalSettings_token);
                }
            }
            if (globalSettings != null)
            {
                var properties = globalSettings.GetNodeByName(PropertiesName);
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _UnitScaleFactor_token:
                                Document.GlobalSettings.UnitScaleFactor = property.Properties.GetFloatValue(4);
                                break;
                            case _UpAxis_token:
                                Document.GlobalSettings.UpAxis = property.Properties.GetIntValue(4);
                                break;
                            case _UpAxisSign_token:
                                Document.GlobalSettings.UpAxisSign = property.Properties.GetIntValue(4);
                                break;
                            case _OriginalUpAxis_token:
                                Document.GlobalSettings.OriginalUpAxis =property.Properties.GetIntValue(4);
                                break;
                            case _OriginalUpAxisSign_token:
                                Document.GlobalSettings.OriginalUpAxisSign = property.Properties.GetIntValue(4);
                                break;
                            case _FrontAxis_token:
                                Document.GlobalSettings.FrontAxis = property.Properties.GetIntValue(4);
                                break;
                            case _FrontAxisSign_token:
                                Document.GlobalSettings.FrontAxisSign = property.Properties.GetIntValue(4);
                                break;
                            case _CoordAxis_token:
                                Document.GlobalSettings.CoordAxis = property.Properties.GetIntValue(4);
                                break;
                            case _CoordAxisSign_token:
                                Document.GlobalSettings.CoordAxisSign = property.Properties.GetIntValue(4);
                                break;
                            case _TimeMode_token:
                                Document.GlobalSettings.TimeMode = (FBXMode)property.Properties.GetIntValue(4);
                                break;
                            case _CustomFrameRate_token:
                                Document.GlobalSettings.CustomFrameRate = property.Properties.GetFloatValue(4);
                                break;
                            case _TimeSpanStart_token:
                                Document.GlobalSettings.TimeSpanStart = property.Properties.GetLongValue(4);
                                break;
                            case _TimeSpanStop_token:
                                Document.GlobalSettings.TimeSpanStop = property.Properties.GetLongValue(4);
                                break;
                        }
                    }
                }
            }

            var otherFlags = node.GetNodeByName(_OtherFlags_token);
            if (otherFlags != null)
            {
                var tcDefinition = otherFlags.GetNodeByName(_TCDefinition_token);
                if (tcDefinition != null)
                {
                    Document.NewTC = tcDefinition.Properties.GetIntValue(0) != 127;
                }
            }

            Document.SetupCoordSystem();
        }
    }
}