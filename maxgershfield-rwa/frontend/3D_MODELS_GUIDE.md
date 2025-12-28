# 3D House Models Guide

This guide explains how to use external 3D house models with the Property3DView component.

## Free 3D Model Sources

### Recommended Sources:

1. **Sketchfab** (https://sketchfab.com)
   - Search for "house" and filter by "Downloadable" and "Free"
   - Download in GLTF/GLB format
   - Examples:
     - Simple House: https://sketchfab.com/3d-models/simple-house-b480ce383ac4425a9bf3694842d3937e
     - Low-Poly House: https://sketchfab.com/3d-models/3d-model-low-poly-house-stylized-31440192648d45e9b460f4c321ccbf57

2. **Poly Pizza** (https://poly.pizza)
   - Free Creative Commons models
   - Direct GLTF downloads

3. **CGTrader** (https://www.cgtrader.com)
   - Free section with house models
   - Filter by "Free" and "GLTF" format

4. **TurboSquid** (https://www.turbosquid.com)
   - Free models section
   - Search for "house gltf free"

## How to Use External Models

1. **Download a GLTF/GLB model** from one of the sources above

2. **Place the model file** in your `public` folder:
   ```
   public/
     models/
       house.glb
   ```

3. **Update the Property3DView component** in `RwaData.tsx`:
   ```tsx
   <Property3DView
     totalShares={1000}
     purchasedShares={0}
     sharePrice={rwaData.price / 1000}
     propertyTitle={rwaData.title}
     propertyImage={rwaData.image}
     propertyPrice={rwaData.price}
     houseModelUrl="/models/house.glb"  // Add this prop
     onShareClick={(shareNumber) => {
       console.log("Purchase share:", shareNumber);
     }}
   />
   ```

## Model Requirements

- **Format**: GLTF or GLB (GLB is preferred for single-file models)
- **Size**: Keep models under 10MB for web performance
- **License**: Ensure the model license allows commercial use
- **Scale**: Models will be auto-scaled, but ideally should be around 1-5 units in size

## Brick Mapping

The fractional shares (bricks) are automatically positioned on the house surfaces:
- **Foundation**: 20% of shares
- **Walls**: 60% of shares (15% per wall)
- **Roof**: 20% of shares

The bricks will appear on the model's surfaces. If the model has complex geometry, you may need to adjust the positioning algorithm in the `Property3DView.tsx` component.

## Troubleshooting

### Model not loading?
- Check the file path is correct
- Ensure the file is in the `public` folder
- Check browser console for errors
- Verify the file format is GLTF/GLB

### Bricks not appearing correctly?
- The brick positioning is based on a standard house structure
- For custom models, you may need to adjust the share positioning logic
- Check the `shares` useMemo in Property3DView.tsx

### Performance issues?
- Use low-poly models (< 50k triangles)
- Compress textures
- Use GLB format (binary, smaller file size)
- Consider using Draco compression for further optimization
