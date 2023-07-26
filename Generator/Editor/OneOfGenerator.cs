using UnityEditor;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OneOf.Generator.Editor
{
    public static class OneOfGenerator
    {
        /// <summary>
        /// Change this to properly set the version
        /// </summary>
        private const string version = "1.0.0";

        private const int maxOneOfExtended = 31; 
        private const int maxOneOf = 8;

        const string LicenseFileName = "LICENSE.md";
        const string ReadmeFileName = "README.md";
        const string ThirdPartyFileName = "Third Party Notices.md";
        const string PackageManifestFileName = "package.json";

        const string metaSuffix = ".meta";

        private static void Generator([CallerFilePath] string executingPath = default)
        {
            static void RemakeFile(string path, string content)
            {
                Debug.Log($"Generating {path}");
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path+metaSuffix)) File.Delete(path+metaSuffix);
                File.WriteAllText(path, content);
            }

            Debug.Log("Generator setup");
            var root = Path.GetFullPath(Path.Combine(executingPath, @"..\..\.."));
            

            // License, Readme, Third party setup
            var licensePath = Path.Combine(root, LicenseFileName);
            var readmePath = Path.Combine(root, ReadmeFileName);
            var thirdPartyPath = Path.Combine(root, ThirdPartyFileName);

            var licenseText = File.ReadAllText(licensePath);
            var readmeText = File.ReadAllText(readmePath);
            var thirdPartyText = File.ReadAllText(thirdPartyPath);

            var packageRoot = Path.Combine(root, "OneOfUnity");
            var path = Path.Combine(packageRoot, "Runtime");

            // License, Readme, Third party copy to base package
            Debug.Log("Generating package files for base package");

            licensePath = Path.Combine(packageRoot, LicenseFileName);
            RemakeFile(licensePath, licenseText);

            readmePath = Path.Combine(packageRoot, ReadmeFileName);
            RemakeFile(readmePath, readmeText);
                
            thirdPartyPath = Path.Combine(packageRoot, ThirdPartyFileName);
            RemakeFile(thirdPartyPath, thirdPartyText);

            var packageManifestFile = Path.Combine(packageRoot, PackageManifestFileName);
            var manifest = GetPackageManifest(false, "Easy to use F#-like discriminated unions for C# with exhaustive compile time matching.");
            RemakeFile(packageManifestFile, manifest);

            // Type generation for base package
            Debug.Log("Generating base package");
            for (int i = 1; i < maxOneOf + 2; i++)
            {
                var file = Path.Combine(path, $"OneOfT{i - 1}.generated.cs");
                var output = GetContent(true, i);
                RemakeFile(file, output);

                file = Path.Combine(path, $"OneOfBaseT{i - 1}.generated.cs");
                output = GetContent(false, i);
                RemakeFile(file, output);
            }

            packageRoot = Path.Combine(root, "OneOfUnity.Extended");
            path = Path.Combine(packageRoot, "Runtime");

            // License, Third party copying for extended package
            Debug.Log("Generating package files for extended package");
            
            licensePath = Path.Combine(packageRoot, LicenseFileName);
            RemakeFile(licensePath, licenseText);

            // we don't copy README.md because the extended package has a special one

            thirdPartyPath = Path.Combine(packageRoot, ThirdPartyFileName);
            RemakeFile(thirdPartyPath, thirdPartyText);

            packageManifestFile = Path.Combine(packageRoot, PackageManifestFileName);
            manifest = GetPackageManifest(true, "Easy to use F#-like discriminated unions for C# with exhaustive compile time matching. Now extended to support a ridiculis ammount of generic arguments.", " Extended", ".extended");
            RemakeFile(packageManifestFile, manifest);

            // Type generation for extended package
            Debug.Log("Generating extended package");
            for (int i = maxOneOf + 2; i < maxOneOfExtended + 2; i++)
            {
                var file = Path.Combine(path, $"OneOfT{i - 1}.generated.cs");
                var output = GetContent(true, i);
                RemakeFile(file, output);

                file = Path.Combine(path, $"OneOfBaseT{i - 1}.generated.cs");
                output = GetContent(false, i);
                RemakeFile(file, output);
            }

            // Developer experience is horrible, scrapped for now
            //packageRoot = Path.Combine(root, "OneOfUnity.SourceGenerator");
            //path = Path.Combine(packageRoot, "Runtime");
            //// License copy to SourceGenerator
            //Debug.Log("Generating package files for SourceGenerator package");

            //licensePath = Path.Combine(packageRoot, LicenseFileName);
            //RemakeFile(licensePath, licenseText);

            //BuildGeneratorDll();

            AssetDatabase.Refresh();
        }

        private static string GetPackageManifest(bool isDependent, string description, string extraDisplay = "", string suffix = "")
        {
            string IsDependent(string s, string s2 = "") => isDependent ? s : s2; 

            return @$"{{
    ""name"": ""com.simplycods.oneofunity{suffix}"",
    ""version"": ""{version}"",
    ""author"": 
    {{
        ""name"": ""Simply-Cods""
    }}, 
    ""displayName"": ""OneOf Unity{extraDisplay}"",
    ""description"": ""{description}"",
{IsDependent(@$"    ""dependencies"": {{
        ""com.simplycods.oneofunity"": ""{version}""
    }},
")}    ""license"": ""MIT"",
    ""licensesUrl"": ""https://github.com/Simply-Cods/OneOfUnity/blob/main/LICENSE.md"",
    ""documentationUrl"": ""https://github.com/Simply-Cods/OneOfUnity/blob/main/"",
    ""keywords"" 
    [
        ""discriminated-unions""
    ]
}}";
        }

        private static string GetContent(bool isStruct, int i)
        {
            string RangeJoined(string delimiter, Func<int, string> selector) => Enumerable.Range(0, i).Joined(delimiter, selector);
            string IfStruct(string s, string s2 = "") => isStruct ? s : s2;

            var className = isStruct ? "OneOf" : "OneOfBase";
            var genericArgs = Enumerable.Range(0, i).Select(e => $"T{e}").ToList();
            var genericArg = genericArgs.Joined(", ");
            var sb = new StringBuilder();

            sb.Append(@$"using System;
using static OneOf.Functions;

namespace OneOf
{{
    public {IfStruct("readonly struct", "class")} {className}<{genericArg}> : IOneOf
    {{
        {RangeJoined(@"
        ", j => $"readonly T{j} _value{j};")}
        readonly int _index;

        {IfStruct( // constructor
                $@"OneOf(int index, {RangeJoined(", ", j => $"T{j} value{j} = default")})
        {{
            _index = index;
            {RangeJoined(@"
            ", j => $"_value{j} = value{j};")}
        }}",
                $@"protected OneOfBase(OneOf<{genericArg}> input)
        {{
            _index = input.Index;
            switch (_index)
            {{
                {RangeJoined($@"
                ", j => $"case {j}: _value{j} = input.AsT{j}; break;")}
                default: throw new InvalidOperationException();
            }}
        }}"
                )}

        public object Value =>
            _index switch
            {{
                {RangeJoined(@"
                ", j => $"{j} => _value{j},")}
                _ => throw new InvalidOperationException()
            }};

        public int Index => _index;

        {RangeJoined(@"
        ", j => $"public bool IsT{j} => _index == {j};")}

        {RangeJoined(@"
        ", j => $@"public T{j} AsT{j} =>
            _index == {j} ?
                _value{j} :
                throw new InvalidOperationException($""Cannot return as T{j} as result is T{{_index}}"");")}

        {IfStruct(RangeJoined(@"
        ", j => $"public static implicit operator {className}<{genericArg}>(T{j} t) => new {className}<{genericArg}>({j}, value{j}: t);"))}

        public void Switch({RangeJoined(", ", e => $"Action<T{e}> f{e}")})
        {{
            {RangeJoined(@"
            ", j => @$"if (_index == {j} && f{j} != null)
            {{
                f{j}(_value{j});
                return;
            }}")}
            throw new InvalidOperationException();
        }}

        public TResult Match<TResult>({RangeJoined(", ", e => $"Func<T{e}, TResult> f{e}")})
        {{
            {RangeJoined(@"
            ", j => $@"if (_index == {j} && f{j} != null)
            {{
                return f{j}(_value{j});
            }}")}
            throw new InvalidOperationException();
        }}

        {IfStruct(genericArgs.Joined(@"
        ", bindToType => $@"public static OneOf<{genericArgs.Joined(", ")}> From{bindToType}({bindToType} input) => input;"))}

        {IfStruct(genericArgs.Joined(@"
            ", bindToType => {
                    var resultArgsPrinted = genericArgs.Select(x => {
                        return x == bindToType ? "TResult" : x;
                    }).Joined(", ");
                    return $@"
        public OneOf<{resultArgsPrinted}> Map{bindToType}<TResult>(Func<{bindToType}, TResult> mapFunc)
        {{
            if (mapFunc == null)
            {{
                throw new ArgumentNullException(nameof(mapFunc));
            }}
            return _index switch
            {{
                {genericArgs.Joined(@"
                ", (x, k) =>
                            x == bindToType ?
                                $"{k} => mapFunc(As{x})," :
                                $"{k} => As{x},")}
                _ => throw new InvalidOperationException()
            }};
        }}";
                }))}
");

            if (i > 1)
            {
                sb.AppendLine(
                    RangeJoined(@"
        ", j => {
                        var genericArgWithSkip = Enumerable.Range(0, i).ExceptSingle(j).Joined(", ", e => $"T{e}");
                        var remainderType = i == 2 ? genericArgWithSkip : $"OneOf<{genericArgWithSkip}>";
                        return $@"
		public bool TryPickT{j}(out T{j} value, out {remainderType} remainder)
		{{
			value = IsT{j} ? AsT{j} : default;
            remainder = _index switch
            {{
                {RangeJoined(@"
                ", k =>
                            k == j ?
                                $"{k} => default," :
                                $"{k} => AsT{k},")}
                _ => throw new InvalidOperationException()
            }};
			return this.IsT{j};
		}}";
                    })
                );
            }

            sb.AppendLine($@"
        bool Equals({className}<{genericArg}> other) =>
            _index == other._index &&
            _index switch
            {{
                {RangeJoined(@"
                ", j => @$"{j} => Equals(_value{j}, other._value{j}),")}
                _ => false
            }};

        public override bool Equals(object obj)
        {{
            if (ReferenceEquals(null, obj))
            {{
                return false;
            }}

            {IfStruct(
                    $"return obj is OneOf<{genericArg}> o && Equals(o);",
                    $@"if (ReferenceEquals(this, obj)) {{
                    return true;
            }}

            return obj is OneOfBase<{genericArg}> o && Equals(o);"
                    )}
        }}

        public override string ToString() =>
            _index switch {{
                {RangeJoined(@"
                ", j => $"{j} => FormatValue(_value{j}),")}
                _ => throw new InvalidOperationException(""Unexpected index, which indicates a problem in the OneOf codegen."")
            }};

        public override int GetHashCode()
        {{
            unchecked
            {{
                int hashCode = _index switch
                {{
                    {RangeJoined(@"
                    ", j => $"{j} => _value{j}?.GetHashCode(),")}
                    _ => 0
                }} ?? 0;
                return (hashCode*397) ^ _index;
            }}
        }}
    }}
}}");

            return sb.ToString();
        }

        [MenuItem("Tools/Regenerate OneOf files")]
        private static void ManualGeneration()
        {
            Generator();
        }

        // Might be added later, developer experience is dreadful
        //private static void BuildGeneratorDll([CallerFilePath] string callerPath = default)
        //{
        //    var root = Path.GetFullPath(Path.Combine(callerPath, @"..\..\.."));
        //    var batchFile = Path.Combine(root, @"OneOfUnity.SourceGenerator\.build.bat");
        //    Debug.Log("Building OneOfUnity.SourceGenerator.dll");
        //    var args = $@"/k {batchFile} {version}";
        //    System.Diagnostics.Process.Start("CMD.exe", args);
        //}
    }
    internal static class Extensions
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        internal static string Joined<T>(this IEnumerable<T> source, string delimiter, Func<T, string>? selector = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            if (source == null) { return ""; }
            if (selector == null) { return string.Join(delimiter, source); }
            return string.Join(delimiter, source.Select(selector));
        }
        internal static string Joined<T>(this IEnumerable<T> source, string delimiter, Func<T, int, string> selector)
        {
            if (source == null) { return ""; }
            return string.Join(delimiter, source.Select(selector));
        }
        internal static IEnumerable<T> ExceptSingle<T>(this IEnumerable<T> source, T single) => source.Except(Enumerable.Repeat(single, 1));
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        internal static void AppendLineTo(this string? s, StringBuilder sb) => sb.AppendLine(s);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}