using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NextGenSoftware.Holochain.HoloNET.ORM.Generation;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    /// <summary>
    /// STAR's hApp template engine for Holochain 0.7.0 / 0.7.1: fills the templates in
    /// DNATemplates/RustDNATemplates/Holochain07 (OASISDNA HoloOASIS.RustDNAHolochain07TemplateFolder) for every zome,
    /// entry (holon) and field, producing a complete Cargo workspace, the DNA/hApp manifests, a build script and the
    /// HoloNET ORM C# models. Edit the templates to change what STAR generates; see their README.txt for the placeholders.
    /// </summary>
    public static class HolochainHappTemplateEngine
    {
        /// <summary>Generate the hApp for <paramref name="spec"/> from the templates in <paramref name="templateFolder"/> into <paramref name="outputFolder"/>. Returns the files written.</summary>
        public static List<string> Generate(HappSpec spec, string templateFolder, string outputFolder)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (!Directory.Exists(templateFolder)) throw new DirectoryNotFoundException($"Holochain 0.7 templates not found: {templateFolder}");
            if (string.IsNullOrWhiteSpace(spec.Name) || spec.Zomes.All(z => z.Entries.Count == 0))
                throw new ArgumentException("The hApp needs a name and at least one zome with at least one entry.");

            var files = new List<string>();
            string T(string relative) => File.ReadAllText(Path.Combine(templateFolder, relative.Replace('/', Path.DirectorySeparatorChar))).Replace("\r\n", "\n");

            string happ = HolochainHappGenerator.Snake(spec.Name);
            bool v070 = spec.HolochainVersion == HappHolochainVersion.V0_7_0;
            var happVars = new Dictionary<string, string>
            {
                ["happ"] = happ,
                ["HDK_VERSION"] = v070 ? "0.7.0" : "0.7.1",
                ["HDI_VERSION"] = v070 ? "0.8.0" : "0.8.1",
                ["NAMESPACE"] = string.IsNullOrWhiteSpace(spec.CSharpNamespace) ? HolochainHappGenerator.Pascal(spec.Name) : spec.CSharpNamespace
            };

            void Write(string relative, string content)
            {
                string path = Path.Combine(outputFolder, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, content);
                files.Add(path);
            }

            var workspaceDeps = new StringBuilder();
            var integrityZomes = new StringBuilder();
            var coordinatorZomes = new StringBuilder();

            foreach (HappZomeSpec zome in spec.Zomes.Where(z => z.Entries.Count > 0))
            {
                var zomeVars = new Dictionary<string, string>(happVars) { ["zome"] = HolochainHappGenerator.Snake(zome.Name) };
                string z = zomeVars["zome"];
                workspaceDeps.Append(Fill(T("workspace/integrity_workspace_dep.toml"), zomeVars));
                integrityZomes.Append(Fill(T("manifests/dna_integrity_zome.yaml"), zomeVars));
                coordinatorZomes.Append(Fill(T("manifests/dna_coordinator_zome.yaml"), zomeVars));

                var structs = new StringBuilder();
                var entryTypes = new StringBuilder();
                var linkTypes = new StringBuilder();
                var functions = new StringBuilder();

                foreach (HappEntrySpec entry in zome.Entries)
                {
                    var entryVars = new Dictionary<string, string>(zomeVars) { ["Holon"] = HolochainHappGenerator.Pascal(entry.Name), ["holon"] = HolochainHappGenerator.Snake(entry.Name) };
                    var rustFields = new StringBuilder();
                    var csFields = new StringBuilder();

                    foreach (HappFieldSpec field in entry.Fields)
                    {
                        (string typeTemplate, string csType) = FieldType(field.CSharpType);
                        string snake = HolochainHappGenerator.Snake(field.Name);
                        var fieldVars = new Dictionary<string, string>(entryVars)
                        {
                            ["field"] = RustKeywords.Contains(snake) ? "r#" + snake : snake,
                            ["Field"] = HolochainHappGenerator.Pascal(field.Name),
                            ["CSType"] = csType
                        };
                        rustFields.Append(Fill(T($"types/{typeTemplate}.rs"), fieldVars));
                        fieldVars["field"] = snake; // the C# attribute names the field as serialised (no r#)
                        csFields.Append(Fill(T("csharp/field.cs.template"), fieldVars));
                    }

                    structs.Append(Fill(T("zome/integrity_entry.rs"), new Dictionary<string, string>(entryVars) { ["FIELDS"] = rustFields.ToString() }));
                    entryTypes.Append(Fill(T("zome/integrity_entry_type.rs"), entryVars));
                    linkTypes.Append(Fill(T("zome/integrity_link_types.rs"), entryVars));
                    foreach (string crud in new[] { "types", "create", "read", "update", "delete", "list", "collection" })
                        functions.Append(Fill(T($"crud/{crud}.rs"), entryVars));

                    Write($"CSharp/{entryVars["Holon"]}.cs", Fill(T("csharp/HoloNETEntry.cs.template"), new Dictionary<string, string>(entryVars) { ["FIELDS"] = csFields.ToString() }));
                }

                Write($"dnas/{happ}/zomes/integrity/{z}_integrity/Cargo.toml", Fill(T("zome/integrity_Cargo.toml"), zomeVars));
                Write($"dnas/{happ}/zomes/integrity/{z}_integrity/src/lib.rs", Fill(T("zome/integrity_lib.rs"), new Dictionary<string, string>(zomeVars)
                {
                    ["ENTRY_STRUCTS"] = structs.ToString(), ["ENTRY_TYPES"] = entryTypes.ToString(), ["LINK_TYPES"] = linkTypes.ToString()
                }));
                Write($"dnas/{happ}/zomes/coordinator/{z}/Cargo.toml", Fill(T("zome/coordinator_Cargo.toml"), zomeVars));
                Write($"dnas/{happ}/zomes/coordinator/{z}/src/lib.rs", Fill(T("zome/coordinator_lib.rs"), new Dictionary<string, string>(zomeVars) { ["HOLON_FUNCTIONS"] = functions.ToString() }));
            }

            Write("Cargo.toml", Fill(T("workspace/Cargo.toml"), new Dictionary<string, string>(happVars) { ["INTEGRITY_WORKSPACE_DEPS"] = workspaceDeps.ToString() }));
            Write($"dnas/{happ}/workdir/dna.yaml", Fill(T("manifests/dna.yaml"), new Dictionary<string, string>(happVars)
            {
                ["INTEGRITY_ZOMES"] = integrityZomes.ToString(), ["COORDINATOR_ZOMES"] = coordinatorZomes.ToString()
            }));
            Write("workdir/happ.yaml", Fill(T("manifests/happ.yaml"), happVars));
            Write("build.sh", Fill(T("build.sh"), happVars));
            Write("README.md", Fill(T("README.md"), happVars));
            return files;
        }

        private static string Fill(string template, Dictionary<string, string> vars)
        {
            foreach (var kv in vars)
                template = template.Replace("{{" + kv.Key + "}}", kv.Value);
            return template;
        }

        // C# field type -> (types/<template>.rs, generated C# property type). Guid and DateTime are kept as strings.
        private static (string, string) FieldType(string csharpType)
        {
            switch ((csharpType ?? "string").Trim().ToLowerInvariant().Replace("system.", ""))
            {
                case "string": case "guid": case "datetime": return ("string", "string");
                case "int": case "int32": return ("int", "int");
                case "long": case "int64": return ("long", "long");
                case "bool": case "boolean": return ("bool", "bool");
                case "double": case "float": case "single": return ("double", "double");
                default: throw new ArgumentException($"Unsupported field type '{csharpType}' (use string, int, long, bool, double, float, Guid or DateTime).");
            }
        }

        private static readonly HashSet<string> RustKeywords = new HashSet<string> { "type", "struct", "fn", "let", "match", "move", "ref", "self", "super", "crate", "mod", "use", "impl", "trait", "enum", "where", "loop", "while", "for", "in", "if", "else", "return", "static", "const", "mut", "pub", "true", "false", "as", "break", "continue", "extern", "unsafe", "dyn", "async", "await", "box", "yield" };
    }
}
