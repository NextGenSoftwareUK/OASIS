using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.Firestore;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

// Standard native Firestore storage sizes for the provider's flat canonical scalar projection.
// Not protobuf request size, index size, a fee quote, or proof of service acceptance.
internal static class FirestoreDocumentSize
{
    internal const long MaximumDocumentBytes=1024L*1024;
    private const long MaximumFieldValueBytes=MaximumDocumentBytes-89;
    private static readonly UTF8Encoding StrictUtf8=new(false,true);
    internal static long Require(DocumentReference reference,IReadOnlyDictionary<string,object> fields)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var prefix=$"projects/{reference.Database.ProjectId}/databases/{reference.Database.DatabaseId}/documents/";
        if(!reference.Path.StartsWith(prefix,StringComparison.Ordinal))throw new ArgumentException("Firestore document path differs from its database.");
        return RequireRelative(reference.Path.Substring(prefix.Length),fields);
    }
    internal static long RequireRelative(string relativePath,IReadOnlyDictionary<string,object> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);ArgumentNullException.ThrowIfNull(fields);
        var names=relativePath.Split('/');if(names.Length%2!=0)throw new ArgumentException("A collection/document path is required.");
        long size=16+32;
        foreach(var name in names){if(name.Length==0)throw new ArgumentException("Firestore path contains an empty identity.");size+=StrictUtf8.GetByteCount(name)+1L;}
        foreach(var pair in fields)
        {
            if(string.IsNullOrEmpty(pair.Key))throw new ArgumentException("A native field name is required.");
            var nameBytes=StrictUtf8.GetByteCount(pair.Key);if(nameBytes>1500)throw new ArgumentException("Firestore field name exceeds1500UTF8bytes.");
            long valueBytes;
            if(pair.Value is string text){var bytes=StrictUtf8.GetByteCount(text);if(bytes>MaximumFieldValueBytes)throw new ArgumentException("Firestore string field exceeds its native value limit.");valueBytes=bytes+1L;}
            else valueBytes=pair.Value switch{null=>1,bool=>1,int or long or double or Timestamp=>8,_=>throw new ArgumentException("Canonical Firestore fields require the existing scalar projection.")};
            size+=nameBytes+1L+valueBytes;
        }
        if(size>MaximumDocumentBytes)throw new ArgumentException("Firestore document exceeds its native1MiB storage limit.");
        return size;
    }
}
