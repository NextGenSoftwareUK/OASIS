namespace GoShared
{
    /// <summary>
    /// Stands in for GO Map's GoShared.GOCoordinate, which the bridge locates by full
    /// name in the loaded assemblies and fills reflectively (lower-case members).
    /// </summary>
    public class GOCoordinate
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
    }
}
