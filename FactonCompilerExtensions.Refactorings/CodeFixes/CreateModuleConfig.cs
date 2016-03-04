namespace FactonCompilerExtensions.Refactorings.CodeFixes
{
	using System;
	using System.Collections.Immutable;
	using System.Composition;
	using System.Threading.Tasks;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CodeFixes;

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateModuleConfig)), Shared]
	public class CreateModuleConfig : CodeFixProvider
	{
		// TODO: Replace with actual diagnostic id that should trigger this fix.
		public const string DiagnosticId = "CreateModuleConfig";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(DiagnosticId); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			throw new NotImplementedException();
		}
	}
}