using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace EVA.Workspaces;

public class WorkspaceManager : IDisposable
{
    private readonly AdhocWorkspace _workspace;

    public WorkspaceManager()
    {
        _workspace = new AdhocWorkspace();
    }

    public Project CreateProject(string projectName = "VirtualProject")
    {
        ProjectId projectId = ProjectId.CreateNewId(projectName);
        Solution solution = _workspace.CurrentSolution
            .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        return solution.GetProject(projectId)!;
    }

    public Document AddDocument(Project project, string fileName, string code)
    {
        DocumentId documentId = DocumentId.CreateNewId(project.Id);
        Solution solution = project.Solution.AddDocument(documentId, fileName, SourceText.From(code));
        return solution.GetDocument(documentId)!;
    }

    public async Task<SyntaxTree?> GetSyntaxTreeAsync(Document document)
    {
        return await document.GetSyntaxTreeAsync();
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }
}
