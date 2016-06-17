namespace Persimmon.Runner

open System
open System.IO
open System.Text
open System.Diagnostics
open System.Reflection

open Persimmon
open Persimmon.Internals
open Persimmon.Runner
open Persimmon.Output

[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type TestExecutor() =
  inherit MarshalByRefObject()

  member this.RunTests assemblyPath (reporter: IReporter) isParallel =

    let preloadAssembly = Assembly.ReflectionOnlyLoadFrom assemblyPath
    let assemblyName = preloadAssembly.FullName
    let targetAssembly = Assembly.Load assemblyName

    let collector = new TestCollector()
    let tests = collector.Collect(targetAssembly)

    let watch = new Stopwatch()

    if isParallel then
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
