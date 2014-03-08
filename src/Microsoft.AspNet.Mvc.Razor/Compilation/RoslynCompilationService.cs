﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private readonly IDependencyExporter _exporter;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        IDependencyExporter exporter)
        {
            _environment = environment;
            _loader = loaderEngine;
            _exporter = exporter;
        }

        public Task<CompilationResult> Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };
            var targetFramework = _environment.TargetFramework;

            var references = GetApplicationReferences();

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    var result = compilation.Emit(ms, pdbStream: pdb);

                    if (!result.Success)
                    {
                        var formatter = new DiagnosticFormatter();

                        var messages = result.Diagnostics.Where(IsError).Select(d => GetCompilationMessage(formatter, d)).ToList();

                        return Task.FromResult(CompilationResult.Failed(content, messages));
                    }

                    var type = _loader.LoadBytes(ms.ToArray(), pdb.ToArray())
                                       .GetExportedTypes()
                                       .First();

                    return Task.FromResult(CompilationResult.Successful(String.Empty, type));
                }
            }
        }

        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();
            var applicationExport = _exporter.GetDependencyExport(_environment.ApplicationName, _environment.TargetFramework);

            // HACK: This is a hack, we need a way to get the application's dependencies.
            // Today this is relying on dynamic compilation
            ExtractReferences(applicationExport, references, expandCompilationReferences: true);

            var assemblies = new[] {
                "System.Linq",
                "System.Dynamic",
                "System.Dynamic.Runtime",
                typeof(HtmlString).GetTypeInfo().Assembly.GetName().Name
            };

            foreach (var assemblyName in assemblies)
            {
                var export = _exporter.GetDependencyExport(assemblyName, _environment.TargetFramework);

                if (export == null)
                {
                    continue;
                }

                ExtractReferences(export, references, expandCompilationReferences: false);
            }

            return references;
        }

        private void ExtractReferences(IDependencyExport export, List<MetadataReference> references, bool expandCompilationReferences)
        {
            foreach (var metadataReference in export.MetadataReferences)
            {
                var fileMetadataReference = metadataReference as IMetadataFileReference;

                if (fileMetadataReference != null)
                {
                    string path = fileMetadataReference.Path;
#if NET45
                    references.Add(new MetadataFileReference(path));
#else
                    // TODO: What about access to the file system? We need to be able to 
                    // read files from anywhere on disk, not just under the web root
                    using (var stream = File.OpenRead(path))
                    {
                        references.Add(new MetadataImageReference(stream));
                    }
#endif
                }
                else
                {
                    var roslynReference = metadataReference as IRoslynMetadataReference;

                    if (roslynReference != null)
                    {
                        references.Add(roslynReference.MetadataReference);

                        if (expandCompilationReferences)
                        {
                            var compilatonReference = roslynReference.MetadataReference as CompilationReference;
                            if (compilatonReference != null)
                            {
                                references.AddRange(compilatonReference.Compilation.References);
                            }
                        }
                    }
                }
            }
        }

        private CompilationMessage GetCompilationMessage(DiagnosticFormatter formatter, Diagnostic diagnostic)
        {
            return new CompilationMessage(formatter.Format(diagnostic));
        }

        private bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}