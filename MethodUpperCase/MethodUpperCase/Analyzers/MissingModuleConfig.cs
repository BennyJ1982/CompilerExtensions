using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refactorings.Analyzers
{
	using System.Linq;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MissingModuleConfig : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "MissingModuleConfig";
		internal static readonly LocalizableString Title = "Missing module configuration";
		internal static readonly LocalizableString MessageFormat = "No module configuration file could be found for '{0}.{1}'.";
		internal const string Category = "FACTON";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

		private const string ModuleInterface = "IModule";
		private const string ModuleNamespace = "Facton.Infrastructure.Modularity";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
		}

		private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			INamedTypeSymbol factonModule;
			if (!TryGetFactonModule(context, out factonModule))
			{
				return;
			}

			// TODO check if config file exists

			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), factonModule.ContainingNamespace, factonModule.Name));
		}

		private static bool TryGetFactonModule(SyntaxNodeAnalysisContext context, out INamedTypeSymbol factonModule)
		{
			var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
			if (classDeclarationSyntax.BaseList == null)
			{
				factonModule = null;
				return false;
			}

			var baseTypeIdentifiers = classDeclarationSyntax.BaseList.Types.Select(t => t.Type).OfType<IdentifierNameSyntax>();
			if (!baseTypeIdentifiers.Any(b => b.Identifier.ValueText.Equals(ModuleInterface)))
			{
				factonModule = null;
				return false;
			}

			// syntactically matching base type found. Check if it also macthes semantically
			var module = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
			foreach (var moduleInterface in module.Interfaces)
			{
				if (moduleInterface.ContainingNamespace.ToString() == ModuleNamespace && moduleInterface.Name == ModuleInterface)
				{
					// found a FACTON module
					factonModule = module;
					return true;
				}
			}

			factonModule = null;
			return false;
		}
	}
}