namespace BraceAnalyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Rename;
    using Microsoft.CodeAnalysis.Text;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            var editor = new SyntaxEditor(root, context.Document.Project.Solution.Workspace);
            
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => AddBracesAsync(context.Document, diagnostic, editor),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        Task<Document> AddBracesAsync(Document document, Diagnostic diagnostic, SyntaxEditor editor)
        {
            // Compute brace indication.
            var root = editor.OriginalRoot;
            var statement = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<StatementSyntax>();
            editor.ReplaceNode(statement, (currentStatement, _)
                => currentStatement.ReplaceNode(currentStatement, SyntaxFactory.Block(currentStatement as StatementSyntax))
            );
            var newRoot = editor.GetChangedRoot();
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

    }
}
