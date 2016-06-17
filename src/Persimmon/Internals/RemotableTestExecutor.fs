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

  /// <summary>
  /// For internal use only.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <param name="isParallel">Parallel execution.</param>
  member this.RunTests assemblyPath (reporter: IRemoteReporter) isParallel =

    let preloadAssembly = Assembly.ReflectionOnlyLoadFrom assemblyPath
    let assemblyName = preloadAssembly.FullName
    let targetAssembly = Assembly.Load assemblyName

    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)

    let runner = new TestRunner()

    if isParallel then
      runner.AsyncRunAllTests callback tests |> Async.RunSynchronously
    else
      runner.RunSynchronouslyAllTests callback tests

      async {
        watch.Start()
        let! res = TestRunner.asyncRunAllTests reporter.ReportProgress tests
        watch.Stop()
        // report
        reporter.ReportProgress(TestResult.endMarker)
        reporter.ReportSummary(res.Results)
        return res.Errors
      }
      |> Async.RunSynchronously
    else
      watch.Start()
      let res = TestRunner.runAllTests reporter.ReportProgress tests
      watch.Stop()
      // report
      reporter.ReportProgress(TestResult.endMarker)
      reporter.ReportSummary(res.Results)
      res.Errors
