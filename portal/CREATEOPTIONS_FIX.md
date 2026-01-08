# CreateOptions Fix Applied

## Change Made

Changed `CreateOptions` property in `CreateGeoNFTRequest` class to be nullable:

**Before:**
```csharp
public ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> CreateOptions { get; set; } = null;
```

**After:**
```csharp
public ISTARNETCreateOptions<STARGeoNFT, STARNETDNA>? CreateOptions { get; set; } = null;
```

## Location

File: `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers/GeoNFTsController.cs`
Line: 732

## Why This Fix Works

- Adding the `?` makes the property explicitly nullable at the type level
- This tells ASP.NET Core model binding that the field is optional
- Other controllers in the codebase use the same pattern (e.g., `ZomesMetaDataController`, `TemplatesController`, `RuntimesController`, `OAPPsController`)

## Testing

After restarting the STAR API server, test with:
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name": "TestCreate", "description": "Test description", "sourceFolderPath": "GeoNFTs/Source/TestCreate", "holonSubType": 86}'
```

The request should now work without requiring the `createOptions` field.






























