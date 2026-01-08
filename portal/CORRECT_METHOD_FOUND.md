# Correct Method for Creating GeoNFTs

## Summary

**The correct endpoint is `POST /api/GeoNFTs/create`** which uses `CreateAsync` method.

## Method Signature

From `GeoNFTsController.CreateGeoNFTWithOptions` (line 322):
```csharp
var result = await _starAPI.GeoNFTs.CreateAsync(AvatarId, request.Name, request.Description, request.HolonSubType, request.SourceFolderPath, request.CreateOptions);
```

## Request Structure

From `CreateGeoNFTRequest` class (line 726-733):
```csharp
public class CreateGeoNFTRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public HolonType HolonSubType { get; set; } = HolonType.GeoNFT;
    public string SourceFolderPath { get; set; } = "";
    public ISTARNETCreateOptions<STARGeoNFT, STARNETDNA> CreateOptions { get; set; } = null;
}
```

## Issues

1. **CreateOptions is marked as Required** - Validation error says "The CreateOptions field is required" even though it's nullable in code
2. **CreateOptions is an interface type** - ASP.NET Core may have trouble deserializing it
3. **CreateOptions structure is complex** - Based on CLI code, it should have:
   - `STARNETDNA` (optional)
   - `STARNETHolon` (optional) 
   - `MetaTagMappings` (optional)
   - `CheckIfSourcePathExists` (optional)
   - `CustomCreateParams` (optional)

## CURL Command Attempts

All attempts to call `/api/GeoNFTs/create` result in:
```json
{
  "errors": {
    "CreateOptions": ["The CreateOptions field is required."]
  }
}
```

Even when passing:
- `"createOptions": null`
- `"CreateOptions": null`
- Omitting the field entirely

## Conclusion

**The `/api/GeoNFTs/create` endpoint appears to have a validation/binding issue** where CreateOptions is incorrectly marked as required when it should be optional (since the backend method accepts `null`).

**Recommendation**: This is a backend bug that needs to be fixed - either:
1. Remove the [Required] attribute from CreateOptions (if present)
2. Make CreateOptions truly optional in the model binding
3. Or use a different endpoint/method

**For now, the backend needs to be fixed before this endpoint can be used.**






























