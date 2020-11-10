namespace BraceAnalyzer
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BraceAnalyzerCodeFixProvider)), Shared]
    public class BraceAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add brace";
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(BraceAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => AddBracesAsync(root, context.Document, diagnosticSpan),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        Task<Document> AddBracesAsync(SyntaxNode root, Document document, TextSpan diagnosticSpan)
        {
            // Find the statement to wrap in braces.
            var statement = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<StatementSyntax>();
            // Compute brace indication.
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(statement, SyntaxFactory.Block(statement))));
        }

    }
}
