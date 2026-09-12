namespace OGEditorSDK.MapFormat
{
    public enum DiagnosticSeverity { Info, Warning, Error }

    public class OGConversionDiagnostic
    {
        public DiagnosticSeverity Severity    { get; set; }
        public string             Message     { get; set; }
        public string             EntityClass { get; set; }  // null if not entity-specific
        public OGVector3?         Location    { get; set; }  // null if no location

        public OGConversionDiagnostic(DiagnosticSeverity severity, string message)
        {
            Severity = severity;
            Message  = message;
        }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    public class OGMapConversionResult
    {
        public string SourceFormat      { get; set; }
        public string DestinationFormat { get; set; }
        public float  Fidelity          { get; set; }
        public bool   Success           { get; set; }
        public string OutputPath        { get; set; }
        public System.Collections.Generic.List<OGConversionDiagnostic> Diagnostics { get; set; }
            = new System.Collections.Generic.List<OGConversionDiagnostic>();
    }
}
