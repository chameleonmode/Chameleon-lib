using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

using Chameleon.lib.Playwright.Interfaces;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Chameleon.lib.Playwright.Services;
public class CompileScriptService {
	public async Task<IExternalScript> CompileScript(string script)
			=> await Task.Run(() => {
				var assembly = CompileCode(script);
				ArgumentNullException.ThrowIfNull(assembly);

				var type = assembly.GetTypes().FirstOrDefault();
				if (!typeof(IExternalScript).IsAssignableFrom(type)) {
					throw new Exception("The script does not meet the requirements to run. Please implement the IExternalScript interface.");
				}

				var instance = Activator.CreateInstance(type) as IExternalScript;
				ArgumentNullException.ThrowIfNull(instance);
				return instance;
			});

	private static Assembly CompileCode(string code) {
		var compilation = CompileTree(code);

		using var ms = new MemoryStream();
		var result = compilation.Emit(ms);

		if (result.Success) {
			_ = ms.Seek(0, SeekOrigin.Begin);
			return Assembly.Load(ms.ToArray());
		}

		var message = GenerateExceptionMessage(result);

		throw new Exception(message);
	}

	private static string GenerateExceptionMessage(EmitResult result) {
		var sb = new StringBuilder();
		_ = sb.AppendLine("It was a error when compiling the script:");

		var failures = result.Diagnostics.Where(diagnostic =>
				diagnostic.IsWarningAsError ||
				diagnostic.Severity == DiagnosticSeverity.Error);

		foreach (var diagnostic in failures) {
			_ = sb.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
		}

		return sb.ToString();
	}

	private static CSharpCompilation CompileTree(string code) {
		var syntaxTree = CSharpSyntaxTree.ParseText(code);
		var references = GetReferences();

		var assemblyName = Path.GetRandomFileName();

		return CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: new[] { syntaxTree },
				references: references,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static HashSet<MetadataReference> GetReferences() {
		var domainAssemblys = AppDomain.CurrentDomain.GetAssemblies()
		.Union(typeof(Microsoft.Playwright.Playwright).Assembly.GetReferencedAssemblies().Select(Assembly.Load))
		.Union([typeof(object).Assembly,
								typeof(Console).Assembly,
								typeof(Regex).Assembly,
								typeof(Microsoft.Playwright.Playwright).Assembly,
								typeof(IExternalScript).Assembly,
								typeof(System.Linq.Expressions.Expression).Assembly,
								typeof(TaskExtensions).Assembly]);

		var metadataReferenceList = new HashSet<MetadataReference>();
		foreach (var assembl in domainAssemblys) {
			unsafe {
				if (!assembl.TryGetRawMetadata(out var blob, out var length))
					continue;

				var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
				var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
				var metadataReference = assemblyMetadata.GetReference();
				_ = metadataReferenceList.Add(metadataReference);
			}
		}
		return metadataReferenceList;
	}
}

