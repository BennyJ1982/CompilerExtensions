namespace Refactorings.CodeRefactorings
{
	using System.Composition;
	using System.Threading.Tasks;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CodeActions;
	using Microsoft.CodeAnalysis.CodeRefactorings;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MethodUpperCaseCodeRefactoringProvider)), Shared]
	internal class MethodUpperCaseCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override sealed async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
			{
				return;
			}

			var span = context.Span;
			if (!span.IsEmpty)
			{
				return;
			}

			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			var syntaxTree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken);
			if (syntaxTree == null)
			{
				return;
			}

			var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var token = root.FindToken(span.Start);

			var methodDeclaration = token.Parent as MethodDeclarationSyntax;
			if (methodDeclaration == null || !token.IsKind(SyntaxKind.IdentifierToken))
			{
				return;
			}

			var methodName = methodDeclaration.Identifier.ValueText;
			if (!char.IsLower(methodName[0]))
			{
				return;
			}

			context.RegisterRefactoring(
				CodeAction.Create("Rename method to have an uppercase first character.", c => ChangeToUppercase(document, root, methodDeclaration)));
		}

		private static Task<Document> ChangeToUppercase(Document document, SyntaxNode root, MethodDeclarationSyntax method)
		{
			var methodName = method.Identifier.ValueText;
			var newName = methodName.Substring(0, 1).ToUpper() + methodName.Substring(1);

			var newIdentifier = SyntaxFactory.Identifier(method.Identifier.LeadingTrivia, newName, method.Identifier.TrailingTrivia);
			var newMethod = method.WithIdentifier(newIdentifier);

			var newRoot = root.ReplaceNode(method, newMethod);
			return Task.FromResult(document.WithSyntaxRoot(newRoot));
		}
	}
}