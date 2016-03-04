using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactonCompilerExtensions.Refactorings.Analyzers
{
	using System;
	using System.Reflection;
	using System.Linq;
	using EnvDTE;
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

			var moduleFilePath = context.Node.SyntaxTree.FilePath;
			if (string.IsNullOrEmpty(moduleFilePath))
			{
				return;
			}

			Workspace workspace;
			if (!TryGetWorkspace(context, out workspace))
			{
				return;
			}

			IServiceProvider serviceProvider;
			if (!TryGetServiceProvider(workspace, out serviceProvider))
			{
				return;
			}

			var dte = serviceProvider.GetService(typeof(DTE)) as DTE;
			if (dte == null)
			{
				return;
			}

			var documentIds = workspace.CurrentSolution.GetDocumentIdsWithFilePath(context.Node.SyntaxTree.FilePath);
			if (!documentIds.Any())
			{
				return;
			}

			var project = workspace.CurrentSolution.GetProject(documentIds.First().ProjectId);
			var dteProject = dte.Solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name == project.Name);
			if (dteProject == null)
			{
				return;
			}

			//dteProject.ProjectItems.AddFolder("New Folder" + dteProject.ProjectItems.Count);

			// TODO check if config file already exists

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

		/// <summary>
		/// Hacky way of getting the workspace until Micsofot has made their internal class public
		/// </summary>
		private static bool TryGetWorkspace(SyntaxNodeAnalysisContext context, out Workspace workspace)
		{
			var type = context.Options.GetType();
			if (type.Name != "WorkspaceAnalyzerOptions")
			{
				workspace = null;
				return false;
			}

			var property = type.GetRuntimeProperty("Workspace");
			if (property == null)
			{
				workspace = null;
				return false;
			}

			workspace = (Workspace)property.GetValue(context.Options);
			return true;
		}

		/// <summary>
		/// Hacky way of getting the visual studio service provider until Microsoft has made their internal class public
		/// </summary>
		private static bool TryGetServiceProvider(Workspace workspace, out IServiceProvider serviceProvider)
		{
			var property = workspace.GetType().GetRuntimeFields().FirstOrDefault(f => f.Name == "ServiceProvider");
			if (property == null)
			{
				serviceProvider = null;
				return false;
			}

			serviceProvider = (IServiceProvider)property.GetValue(workspace);
			return true;
		}
	}
}