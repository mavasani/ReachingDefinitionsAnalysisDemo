using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ReachingDefinitionsAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;

namespace ExecutionProject
{
    class Program
    {
        const string ProjectRelativeFilePath = @"..\..\..\..\AnalysisProject\AnalysisProject.csproj";

        static void Main()
        {
            // #1. Load the test project and source file
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), ProjectRelativeFilePath);
            var compilation = GetCompilation(absolutePath);
            var tree = compilation.SyntaxTrees.Single(t => t.FilePath.EndsWith("Class1.cs"));
            var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);

            // #2. For every method in source file, iterate through operation blocks.
            foreach (var (methodBody, containingMethod) in GetBlockOperations(semanticModel))
            {
                // #3. Get ControlFlowGraph and execute Reaching definitions analysis.
                var cfg = ControlFlowGraph.Create(methodBody);
                var interproceduralConfig = InterproceduralAnalysisConfiguration.Create(analyzerOptions, s_descriptor,
                    cfg, compilation, InterproceduralAnalysisKind.None, CancellationToken.None);
                var reachingDefinitionsResult = ReachingDefinitionsAnalysis.TryGetOrComputeResult(cfg, containingMethod,
                    wellKnownTypeProvider, analyzerOptions, interproceduralConfig);

                if (reachingDefinitionsResult != null)
                {
                    Console.WriteLine($"ReachingDefinitionsAnalysis data for '{containingMethod.Name}':");
                    Console.WriteLine();
                    Console.WriteLine("Press a key to view data for the next basic block...");

                    // #4. Dump Dataflow analysis results for each basic block
                    foreach (var block in cfg.Blocks)
                    {
                        Console.ReadKey();
                        Console.WriteLine($"  BB{block.Ordinal} ({block.Kind}) ->");

                        // #5. Output Reaching definitions (Input set) for each basic block
                        if (reachingDefinitionsResult[block].Data.Count == 0)
                        {
                            Console.WriteLine($"    Reaching Definitions (In): <None>");
                        }
                        else
                        {
                            Console.WriteLine($"    Reaching Definitions (In):");

                            foreach (var (analysisEntity, reachingDefinitionsAbstractValue) in reachingDefinitionsResult[block].Data)
                            {
                                if (analysisEntity.Symbol != null && !analysisEntity.IsThisOrMeInstance)
                                {
                                    Console.WriteLine($"      For '{analysisEntity.Symbol}': ");
                                    Console.WriteLine($"        {reachingDefinitionsAbstractValue}");
                                }
                            }
                        }

                        Console.ReadKey();

                        // #5. Output Operations for each basic block
                        if (block.Operations.IsEmpty)
                        {
                            Console.WriteLine($"    Operations: <None>");
                        }
                        else
                        {
                            Console.WriteLine($"    Operations:");
                            var seenOperations = new HashSet<SyntaxNode>();
                            foreach (var operation in block.Operations)
                            {
                                var str = operation.Syntax.ToString().Split(Environment.NewLine);
                                var displayStr = str[0];
                                if (str.Length > 1)
                                {
                                    displayStr += " ... " + str[^1];
                                }

                                Console.WriteLine($"        {displayStr}");
                            }
                        }

                        Console.WriteLine();
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Loads a project into memory and returns a 'Compilation' object.
        /// </summary>
        private static Compilation GetCompilation(string projectFilePath)
        {
            MSBuildLocator.RegisterDefaults();
            using var workspace = MSBuildWorkspace.Create();
            var projectTask = workspace.OpenProjectAsync(projectFilePath);
            projectTask.Wait();
            var compilationTask = projectTask.Result.GetCompilationAsync();
            compilationTask.Wait();
            return compilationTask.Result;
        }

        /// <summary>
        /// Returns an enumerable of all method symbols and corresponding method body operation blocks in the source file.
        /// </summary>
        private static IEnumerable<(IMethodBodyOperation, IMethodSymbol)> GetBlockOperations(SemanticModel semanticModel)
        {
            foreach (var node in semanticModel.SyntaxTree.GetRoot().DescendantNodes(descendIntoChildren: node => semanticModel.GetOperation(node) == null))
            {
                if (semanticModel.GetOperation(node) is IMethodBodyOperation methodBodyOperation &&
                    methodBodyOperation.Syntax is MethodDeclarationSyntax methodDeclaration &&
                    semanticModel.GetDeclaredSymbol(methodDeclaration) is IMethodSymbol symbol)
                {
                    yield return (methodBodyOperation, symbol);
                }
            }
        }

#pragma warning disable RS2008 // Enable analyzer release tracking
        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor("RuleId", "Title", "Message", "Category", DiagnosticSeverity.Warning, true);
#pragma warning restore RS2008 // Enable analyzer release tracking
    }
}