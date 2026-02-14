using System;
using System.Linq;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _ShaderLanguage_token = 3432100625708765602;
        private const long _ShaderLanguageVersion_token = 4939598639134320854;
        private const long _RenderAPI_token = -4289193798537740737;
        private const long _RootBindingName_token = -7052471165649598007;
        private const long _TargetName_token = -3837749658307710591;
        private const long _TargetType_token = -3837749658307508688;
        private const long _Entry_token = 7096547112123187085;

        private FBXImplementation ProcessImplementation(FBXNode node, long objectId, string name, string objectClass)
        {
            var implementation = new FBXImplementation(Document, name, objectId, objectClass);
            if (node!= null)
            {
                var properties = node.GetNodeByName(PropertiesName);
                if (properties.HasSubNodes)
                {
                    
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _ShaderLanguage_token:
                                implementation.ShaderLanguage = property.Properties.GetStringValue(4, false);
                                break;
                            case _ShaderLanguageVersion_token:
                                implementation.ShaderLanguageVersion = property.Properties.GetIntValue(4);
                                break;
                            case _RenderAPI_token:
                                implementation.RenderAPI = property.Properties.GetStringValue(4, false);
                                break;
                            case _RootBindingName_token:
                                implementation.RootBindingName = property.Properties.GetStringValue(4, false);
                                break;
                        }
                    }
                }
            }
            return implementation;
        }

        private FBXBindingTable ProcessBindingTable(FBXNode node, long objectId, string name, string objectClass)
        {
            var bindingTable = new FBXBindingTable(Document, name, objectId, objectClass);
            if (node!= null)
            {
                var properties = node.GetNodeByName(PropertiesName);
                if (properties.HasSubNodes)
                {
                    
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _TargetName_token:
                                bindingTable.TargetName = property.Properties.GetStringValue(4, false);
                                break;
                            case _TargetType_token:
                                bindingTable.TargetType = property.Properties.GetStringValue(4, false);
                                break;
                        }
                    }
                }
                var entries = node.GetNodesByName(_Entry_token);
                
                foreach (var entry in entries)
                {
                    var sourceName = entry.Properties.GetStringValue(0, false);
                    var lastIndexOfPipe = sourceName.LastIndexOf("|", StringComparison.Ordinal);
                    if (lastIndexOfPipe > -1)
                    {
                        sourceName = sourceName.Substring(lastIndexOfPipe + 1);
                    }
                    var targetName = entry.Properties.GetStringValue(2, false);
                    var sourceType = entry.Properties.GetStringValue(1, false);
                    var targetType = entry.Properties.GetStringValue(3, false);
                    var finalSourceName = sourceType == "FbxPropertyEntry" ? sourceName : targetName;
                    var finalTargetName = targetType == "FbxSemanticEntry" ? targetName : sourceName;
                    bindingTable.Entries.Add(finalSourceName, finalTargetName);
                }
            }
            return bindingTable;
        }
    }
}