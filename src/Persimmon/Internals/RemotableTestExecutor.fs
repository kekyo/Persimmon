namespace Persimmon.Internals

open System
open System.Diagnostics
open System.Reflection

open Persimmon

/// <summary>
/// For internal use only.
/// </summary>
type IRemoteReporter =
  /// <summary>
  /// For internal use only.
  /// </summary>
  abstract ReportProgress : TestResult -> unit

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
  member this.RunTestsByParallel assemblyPath (reporter: IRemoteReporter) =

    Trace.WriteLine(sprintf "RemotableTestExecutor.RunTests: AppDomainId=%A" AppDomain.CurrentDomain.Id)

    let targetAssembly = loadAssembly assemblyPath

    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)

    let runner = new TestRunner()
    runner.AsyncRunAllTestsByParallel reporter.ReportProgress tests |> Async.RunSynchronously

  /// <summary>
  /// For internal use only.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <returns>RunResult</returns>
  member this.RunTestsBySequential assemblyPath (reporter: IRemoteReporter) =

    Trace.WriteLine(sprintf "RemotableTestExecutor.RunTestsBySequential: AppDomainId=%A" AppDomain.CurrentDomain.Id)

    let targetAssembly = loadAssembly assemblyPath

    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)

    let runner = new TestRunner()
    runner.RunAllTestsBySequential reporter.ReportProgress tests
