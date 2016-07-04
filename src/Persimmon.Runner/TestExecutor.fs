namespace Persimmon.Runner

open System
open System.IO
open System.Text
open System.Diagnostics
open System.Reflection

open Persimmon
open Persimmon.Output

/// <summary>
/// Discovery and execute persimmon based tests.
/// </summary>
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type TestExecutor() =

  let runTestsByParallel assemblyPath (reporter: IReporter) =
    let testExecutor = new Persimmon.Internals.TestExecutor()
    testExecutor.AsyncRunTestsByParallel assemblyPath (fun tr -> reporter.ReportProgress tr)
    
  let runTestsBySequential assemblyPath (reporter: IReporter) =
    let testExecutor = new Persimmon.Internals.TestExecutor()
    testExecutor.AsyncRunTestsBySequential assemblyPath (fun tr -> reporter.ReportProgress tr)

  let reportSummaries (reporter: IReporter) (results: Persimmon.Internals.RunResult<#ResultNode> seq) =
    reporter.ReportProgress(TestResult.endMarker)
    reporter.ReportSummary(
      results
      |> Seq.collect (fun result -> result.Results)
      |> Seq.map (fun result -> result :> ResultNode))

  /// <summary>
  /// Discovery and execute persimmon based tests.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param> 
  /// <param name="isParallel">Execute parallel tests.</param> 
  /// <returns>Number of errors</returns>
  member this.AsyncRunTests assemblyPath reporter isParallel = async {
    let! result = runTestsByParallel assemblyPath reporter
    do reportSummaries reporter [result]
    return result.Errors
  }
 
  /// <summary>
  /// Discovery and execute persimmon based tests.
  /// </summary>
  /// <param name="assemblyPaths">Target assembly paths.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <param name="isParallel">Execute parallel tests.</param> 
  /// <returns>Number of errors</returns>
  member this.AsyncRunAllTests assemblyPaths reporter isParallel = async {
    let! results =
      assemblyPaths
      |> Seq.map (fun assemblyPath -> runTestsByParallel assemblyPath reporter)
      |> Async.Parallel
    do reportSummaries reporter results
    return results |> Seq.sumBy (fun result -> result.Errors)
  }
