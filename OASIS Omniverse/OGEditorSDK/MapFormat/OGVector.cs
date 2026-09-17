using System;

namespace OGEditorSDK.MapFormat
{
    public struct OGVector2
    {
        public float X, Y;
        public OGVector2(float x, float y) { X = x; Y = y; }
        public override string ToString() => $"({X} {Y})";
    }

    public struct OGVector3
    {
        public float X, Y, Z;
        public OGVector3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static OGVector3 operator +(OGVector3 a, OGVector3 b) =>
            new OGVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static OGVector3 operator -(OGVector3 a, OGVector3 b) =>
            new OGVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static OGVector3 operator *(OGVector3 v, float s) =>
            new OGVector3(v.X * s, v.Y * s, v.Z * s);

        public float Dot(OGVector3 o) => X * o.X + Y * o.Y + Z * o.Z;

        public OGVector3 Cross(OGVector3 o) =>
            new OGVector3(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);

        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public OGVector3 Normalized()
        {
            float len = Length();
            return len < 1e-6f ? new OGVector3(0, 0, 1) : new OGVector3(X / len, Y / len, Z / len);
        }

        public override string ToString() => $"({X} {Y} {Z})";
    }

    public struct OGPlane
    {
        public OGVector3 Normal;
        public float Distance;
        public OGPlane(OGVector3 normal, float distance) { Normal = normal; Distance = distance; }

        public static OGPlane FromPoints(OGVector3 a, OGVector3 b, OGVector3 c)
        {
            var n = (b - a).Cross(c - a).Normalized();
            return new OGPlane(n, n.Dot(a));
        }
    }

    public struct OGColor
    {
        public byte R, G, B, A;
        public OGColor(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
    }

    public struct OGFog
    {
        public OGColor Color;
        public float Density;
        public OGFog(OGColor color, float density) { Color = color; Density = density; }
    }
}
