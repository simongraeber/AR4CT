using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace IxMilia.ThreeMf
{
    public enum TargetMode
    {
        External = 1,
        Internal = 0
    }

    public enum CompressionOption
    {
        Fast = 2,
        Maximum = 1,
        Normal = 0,
        NotCompressed = -1,
        SuperFast = 3
    }

    public class PackageRelationship
    {
        private string _type;

        public PackageRelationship(Package package, string target, string id, string type)
        {
            Package = package;
            TargetUri = new Uri(target, UriKind.RelativeOrAbsolute);
            Id = id;
            _type = type;
        }

        public string Id { get; }
        public Package Package { get; }
        public Uri SourceUri { get; }
        public TargetMode TargetMode { get; }
        public Uri TargetUri { get; }
    }


    public class PackagePart
    {
        private Package _package;

        public Uri Uri { get; }
        public string ContentType { get; }

        public PackagePart(Uri uri, Package package)
        {
            _package = package;
            Uri = uri;
        }

        public Stream GetStream()
        {
            return _package.ReadFile(Uri.OriginalString, out _);
        }

        public void CreateRelationship(Uri uri, TargetMode targetMode, string textureRelationshipType)
        {
            throw new NotImplementedException();
        }
    }

    public class Package : IDisposable
    {
        private ZipFile _zipFile;
        private XDocument _rels;

        public static Package Open(Stream stream, FileMode fileMode)
        {
            var package = new Package();
            switch (fileMode)
            {
                case FileMode.Open:
                    package._zipFile = new ZipFile(stream);
                    package.ReadRelationships();
                    break;
                default:
                    package = null;
                    break;
            }
            return package;
        }

        public Stream ReadFile(string path, out long fileSize)
        {
            path = new ZipNameTransform().TransformFile(path);
            var zipEntry = _zipFile.GetEntry(path);
            if (zipEntry == null)
            {
                fileSize = 0;
                return null;
            }
            fileSize = zipEntry.Size;
            return _zipFile.GetInputStream(zipEntry);
        }

        private void ReadRelationships()
        {
            using (var stream = ReadFile("\\_rels\\.rels", out var fileSize))
            {
                if (stream != null)
                {
                    var buffer = new byte[fileSize];
                    StreamUtils.ReadFully(stream, buffer);
                    var xmlData = Encoding.UTF8.GetString(buffer);
                    _rels = XDocument.Parse(xmlData);
                }
            }
        }

        public static Package Open(Stream stream)
        {
            return Open(stream, FileMode.Open);
        }

        public void Dispose()
        {

        }

        public void CreateRelationship(Uri uri, TargetMode targetMode, string modelRelationshipType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PackageRelationship> GetRelationshipsByType(string modelRelationshipType)
        {
            var ns = _rels.Root.Attribute(XName.Get("xmlns")).Value;
            return _rels.Root.
                Descendants(XName.Get("Relationship", ns)).
                Where(rel => rel.Attribute(XName.Get("Type")).Value == modelRelationshipType).
                Select(
                    rel => new
                        PackageRelationship(this,
                            rel.Attribute(XName.Get("Target")).Value,
                            rel.Attribute(XName.Get("Id")).Value,
                            rel.Attribute(XName.Get("Type")).Value
                        )
                );
        }

        public PackagePart GetPart(Uri uri)
        {
            return new PackagePart(uri, this);
        }

        public PackagePart CreatePart(Uri uri, string contentType, CompressionOption compressionOption)
        {
            throw new NotImplementedException();
        }
    }
}