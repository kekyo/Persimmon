namespace Persimmon.Internals

open System
open System.Diagnostics
open System.Reflection

open Persimmon

/// <summary>
/// For internal use only.
/// </summary>
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type RemotableTestExecutor() =
  inherit MarshalByRefObject()

  let loadAssembly assemblyPath =
    let preloadAssembly = Assembly.ReflectionOnlyLoadFrom assemblyPath
    let assemblyName = preloadAssembly.FullName
    Assembly.Load assemblyName

  /// <summary>
  /// For internal use only.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <returns>RunResult</returns>
  member this.RunTestsByParallel assemblyPath (reporter: Action<obj>) =

    Trace.WriteLine(sprintf "RemotableTestExecutor.RunTests: AppDomainId=%A" AppDomain.CurrentDomain.Id)

    let targetAssembly = loadAssembly assemblyPath
    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)
    let runner = new TestRunner()
    runner.AsyncRunAllTestsByParallel (fun tr -> reporter.Invoke(tr)) tests |> Async.RunSynchronously

  /// <summary>
  /// For internal use only.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <returns>RunResult</returns>
  member this.RunTestsBySequential assemblyPath (reporter: Action<obj>) =

    Trace.WriteLine(sprintf "RemotableTestExecutor.RunTestsBySequential: AppDomainId=%A" AppDomain.CurrentDomain.Id)

    let targetAssembly = loadAssembly assemblyPath
    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)
    let runner = new TestRunner()
    runner.AsyncRunAllTestsBySequential (fun rn -> reporter.Invoke(rn)) tests |> Async.RunSynchronously
