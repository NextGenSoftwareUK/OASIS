namespace ScGen.Lib.Shared.Extensions;

public static class HandlebarExtension
{
    public static void SolanaRegisterHelpers(this IHandlebars hb)
    {
        hb.RegisterHelper("snakeCase", (writer, _, args) =>
        {
            string input = args[0]?.ToString() ?? "";
            string snake = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
            writer.WriteSafeString(snake);
        });

        hb.RegisterHelper("pascalCase", (writer, _, args) =>
        {
            string input = args[0]?.ToString() ?? "";
            string[] split = input.Split('_', '-', ' ');
            List<string> pascalList = [];
            for (int i = 0; i < split.Length; i++)
            {
                string s = split[i];
                if (string.IsNullOrEmpty(s)) continue;
                string pascal = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1).ToLower() : "");
                pascalList.Add(pascal);
            }

            string pascalRes = string.Join("", pascalList);
            writer.WriteSafeString(pascalRes);
        });

        hb.RegisterHelper("rustType", (writer, _, args) =>
        {
            string type = args[0]?.ToString() ?? "";
            string rust;
            if (type == "string") rust = "String";
            else if (type == "int") rust = "i64";
            else if (type == "uint") rust = "u64";
            else if (type == "bool") rust = "bool";
            else if (type == "pubkey") rust = "Pubkey";
            else if (type == "bytes") rust = "Vec<u8>";
            else if (type == "float") rust = "f64";
            else rust = type;
            writer.WriteSafeString(rust);
        });

        hb.RegisterHelper("anchorAccountType", (writer, _, args) =>
        {
            string type = args[0]?.ToString() ?? "";
            string rust;
            if (type == "token") rust = "Account<'info, TokenAccount>";
            else if (type == "mint") rust = "Account<'info, Mint>";
            else if (type == "system") rust = "Program<'info, System>";
            else if (type == "associated_token") rust = "Program<'info, AssociatedToken>";
            else if (type == "signer") rust = "Signer<'info>";
            else rust = type;
            writer.WriteSafeString(rust);
        });

        RegisterCommonHelpers(hb);
    }

    public static void EthereumRegisterHelpers(this IHandlebars hb)
    {
        RegisterCommonHelpers(hb);
    }

    public static void RadixRegisterHelpers(this IHandlebars hb)
    {
        hb.RegisterHelper("snakeCase", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string input = args[0].ToString() ?? string.Empty;
            string snake = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
            writer.WriteSafeString(snake);
        });

        hb.RegisterHelper("lowercase", (writer, _, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0] != null)
            {
                writer.WriteSafeString(parameters[0].ToString()?.ToLowerInvariant() ?? string.Empty);
            }
            else
            {
                writer.WriteSafeString(string.Empty);
            }
        });

        hb.RegisterHelper("uppercase", (writer, _, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0] != null)
            {
                writer.WriteSafeString(parameters[0].ToString()?.ToUpperInvariant() ?? string.Empty);
            }
            else
            {
                writer.WriteSafeString(string.Empty);
            }
        });

        hb.RegisterHelper("pascalCase", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string input = args[0].ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string[] split = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 0)
            {
                writer.WriteSafeString(input);
                return;
            }

            List<string> pascalList = new List<string>(split.Length);

            foreach (string s in split)
            {
                if (string.IsNullOrEmpty(s)) continue;
                string pascal = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1).ToLower() : "");
                pascalList.Add(pascal);
            }

            string pascalRes = string.Join("", pascalList);
            writer.WriteSafeString(pascalRes);
        });

        hb.RegisterHelper("camelCase", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string input = args[0].ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string[] split = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 0)
            {
                writer.WriteSafeString(input.ToLowerInvariant());
                return;
            }

            List<string> camelList = new List<string>(split.Length);

            for (int i = 0; i < split.Length; i++)
            {
                string s = split[i];
                if (string.IsNullOrEmpty(s)) continue;

                if (i == 0)
                {
                    camelList.Add(s.ToLowerInvariant());
                }
                else
                {
                    string camel = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1).ToLower() : "");
                    camelList.Add(camel);
                }
            }

            string camelRes = string.Join("", camelList);
            writer.WriteSafeString(camelRes);
        });

        hb.RegisterHelper("scryptoType", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString("String");
                return;
            }

            string type = args[0].ToString()?.ToLowerInvariant() ?? "string";

            string scrypto = type switch
            {
                "string" => "String",
                "str" => "&str",
                "int" => "i64",
                "i32" => "i32",
                "i64" => "i64",
                "uint" => "u64",
                "u32" => "u32",
                "u64" => "u64",
                "u128" => "u128",
                "bool" => "bool",
                "boolean" => "bool",
                "decimal" => "Decimal",
                "precisedecimal" => "PreciseDecimal",
                "bytes" => "Vec<u8>",
                "float" => "Decimal",
                "f32" => "f32",
                "f64" => "f64",
                "address" => "ComponentAddress",
                "componentaddress" => "ComponentAddress",
                "globaladdress" => "GlobalAddress",
                "resource" => "ResourceAddress",
                "resourceaddress" => "ResourceAddress",
                "bucket" => "Bucket",
                "vault" => "Vault",
                "proof" => "Proof",
                "keyvaluestore" => "KeyValueStore",
                "owned" => "Owned",
                "global" => "Global",
                "fungibleresource" => "FungibleResource",
                "nonfungibleresource" => "NonFungibleResource",
                "nonfungiblelocalid" => "NonFungibleLocalId",
                "accessrule" => "AccessRule",
                "role" => "Role",
                "badge" => "Badge",
                _ => ToPascalCase(type)
            };

            writer.WriteSafeString(scrypto);
        });

        hb.RegisterHelper("defaultValue", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString("String::new()");
                return;
            }

            string type = args[0].ToString()?.ToLowerInvariant() ?? "string";

            string defaultVal = type switch
            {
                "string" => "String::new()",
                "str" => "\"\"",
                "i32" => "0i32",
                "i64" => "0i64",
                "u32" => "0u32",
                "u64" => "0u64",
                "u128" => "0u128",
                "bool" => "false",
                "decimal" => "Decimal::zero()",
                "precisedecimal" => "PreciseDecimal::zero()",
                "bytes" => "Vec::new()",
                "vault" => "Vault::new(XRD)",
                "keyvaluestore" => "KeyValueStore::new()",
                "globaladdress" => "GlobalAddress::default()",
                "nonfungiblelocalid" => "NonFungibleLocalId::integer(0)",
                _ => $"{ToPascalCase(type)}::default()"
            };

            writer.WriteSafeString(defaultVal);
        });

        hb.RegisterHelper("hasValue", (writer, _, args) =>
        {
            bool hasValue = args.Length > 0 && args[0] != null && !string.IsNullOrWhiteSpace(args[0].ToString());
            writer.WriteSafeString(hasValue ? "true" : "");
        });

        hb.RegisterHelper("isEmpty", (writer, _, args) =>
        {
            bool isEmpty = args.Length == 0 || args[0] == null || string.IsNullOrWhiteSpace(args[0].ToString());
            writer.WriteSafeString(isEmpty ? "true" : "");
        });

        hb.RegisterHelper("safeString", (writer, _, args) =>
        {
            string value = args.Length > 0 && args[0] != null ? args[0].ToString() ?? string.Empty : string.Empty;
            writer.WriteSafeString(value);
        });

        hb.RegisterHelper("concat", (writer, _, args) =>
        {
            if (args.Length == 0)
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string result = string.Join("", args.Select(arg => arg?.ToString() ?? string.Empty));
            writer.WriteSafeString(result);
        });

        hb.RegisterHelper("join", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString(string.Empty);
                return;
            }

            string separator = args[0]?.ToString() ?? "";
            string result = string.Join(separator, args.Skip(1).Select(arg => arg?.ToString() ?? string.Empty));
            writer.WriteSafeString(result);
        });

        hb.RegisterHelper("replace", (writer, _, args) =>
        {
            if (args.Length < 3)
            {
                writer.WriteSafeString(args.Length > 0 ? args[0]?.ToString() ?? "" : "");
                return;
            }

            string input = args[0]?.ToString() ?? "";
            string oldValue = args[1]?.ToString() ?? "";
            string newValue = args[2]?.ToString() ?? "";

            string result = input.Replace(oldValue, newValue);
            writer.WriteSafeString(result);
        });

        hb.RegisterHelper("trim", (writer, _, args) =>
        {
            string input = args.Length > 0 && args[0] != null ? args[0].ToString() ?? "" : "";
            writer.WriteSafeString(input.Trim());
        });

        hb.RegisterHelper("length", (writer, _, args) =>
        {
            if (args.Length == 0 || args[0] == null)
            {
                writer.WriteSafeString("0");
                return;
            }

            if (args[0] is IEnumerable<object> enumerable)
            {
                writer.WriteSafeString(enumerable.Count().ToString());
            }
            else
            {
                string str = args[0].ToString() ?? "";
                writer.WriteSafeString(str.Length.ToString());
            }
        });

        hb.RegisterHelper("add", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("0");
                return;
            }

            decimal sum = 0;
            foreach (object arg in args)
            {
                if (decimal.TryParse(arg?.ToString(), out decimal value))
                {
                    sum += value;
                }
            }

            writer.WriteSafeString(sum.ToString(CultureInfo.InvariantCulture));
        });

        hb.RegisterHelper("subtract", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("0");
                return;
            }

            if (decimal.TryParse(args[0]?.ToString(), out decimal first))
            {
                decimal result = first;
                for (int i = 1; i < args.Length; i++)
                {
                    if (decimal.TryParse(args[i]?.ToString(), out decimal value))
                    {
                        result -= value;
                    }
                }

                writer.WriteSafeString(result.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteSafeString("0");
            }
        });

        hb.RegisterHelper("gt", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("");
                return;
            }

            bool result = false;
            if (decimal.TryParse(args[0]?.ToString(), out decimal first) &&
                decimal.TryParse(args[1]?.ToString(), out decimal second))
            {
                result = first > second;
            }

            writer.WriteSafeString(result ? "true" : "");
        });

        hb.RegisterHelper("lt", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("");
                return;
            }

            bool result = false;
            if (decimal.TryParse(args[0]?.ToString(), out decimal first) &&
                decimal.TryParse(args[1]?.ToString(), out decimal second))
            {
                result = first < second;
            }

            writer.WriteSafeString(result ? "true" : "");
        });

        hb.RegisterHelper("gte", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("");
                return;
            }

            bool result = false;
            if (decimal.TryParse(args[0]?.ToString(), out decimal first) &&
                decimal.TryParse(args[1]?.ToString(), out decimal second))
            {
                result = first >= second;
            }

            writer.WriteSafeString(result ? "true" : "");
        });

        hb.RegisterHelper("lte", (writer, _, args) =>
        {
            if (args.Length < 2)
            {
                writer.WriteSafeString("");
                return;
            }

            bool result = false;
            if (decimal.TryParse(args[0]?.ToString(), out decimal first) &&
                decimal.TryParse(args[1]?.ToString(), out decimal second))
            {
                result = first <= second;
            }

            writer.WriteSafeString(result ? "true" : "");
        });

        RegisterCommonHelpers(hb);
    }

    private static void RegisterCommonHelpers(IHandlebars hb)
    {
        hb.RegisterHelper("eq", (writer, _, args) =>
        {
            bool ok = args.Length >= 2 &&
                      string.Equals(args[0]?.ToString(), args[1]?.ToString(), StringComparison.Ordinal);
            writer.WriteSafeString(ok ? "true" : "");
        });

        hb.RegisterHelper("ne", (writer, _, args) =>
        {
            bool ok = args.Length >= 2 &&
                      !string.Equals(args[0]?.ToString(), args[1]?.ToString(), StringComparison.Ordinal);
            writer.WriteSafeString(ok ? "true" : "");
        });

        hb.RegisterHelper("and", (writer, _, args) =>
        {
            bool ok = args.Length > 0;
            for (int i = 0; i < args.Length && ok; i++)
            {
                if (!IsTruthy(args[i]))
                {
                    ok = false;
                }
            }

            writer.WriteSafeString(ok ? "true" : "");
        });

        hb.RegisterHelper("or", (writer, _, args) =>
        {
            bool ok = false;
            for (int i = 0; i < args.Length && !ok; i++)
            {
                if (IsTruthy(args[i]))
                {
                    ok = true;
                }
            }

            writer.WriteSafeString(ok ? "true" : "");
        });

        hb.RegisterHelper("not", (writer, _, args) =>
        {
            bool ok = args.Length == 0 || !IsTruthy(args[0]);
            writer.WriteSafeString(ok ? "true" : "");
        });

        hb.RegisterHelper("if", (writer, options, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                options.Inverse(writer, context);
                return;
            }

            if (IsTruthy(parameters[0]))
            {
                options.Template(writer, context);
            }
            else
            {
                options.Inverse(writer, context);
            }
        });

        hb.RegisterHelper("unless", (writer, options, context, parameters) =>
        {
            if (parameters.Length == 0)
            {
                options.Template(writer, context);
                return;
            }

            if (!IsTruthy(parameters[0]))
            {
                options.Template(writer, context);
            }
            else
            {
                options.Inverse(writer, context);
            }
        });
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is decimal d) return d != 0;
        if (value is double db) return db != 0;
        if (value is float f) return f != 0;

        string s = value.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.Equals("0")) return false;
        if (s.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.Equals("undefined", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string[] split = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 0) return input;

        List<string> pascalList = new List<string>(split.Length);

        foreach (string s in split)
        {
            if (string.IsNullOrEmpty(s)) continue;
            string pascal = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1).ToLower() : "");
            pascalList.Add(pascal);
        }

        return string.Join("", pascalList);
    }
}