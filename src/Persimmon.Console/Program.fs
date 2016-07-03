open System
open System.IO
open System.Text
open System.Diagnostics
open System.Reflection

open Persimmon
open Persimmon.Runner
open Persimmon.Output

let entryPoint (args: Args) =

  use progress = if args.NoProgress then IO.TextWriter.Null else Console.Out
  let watch = new Stopwatch()

  let requireFileName, outputs =
    let console = {
      Writer = Console.Out
      Formatter = Formatter.SummaryFormatter.normal watch
    }

    match args.Output, args.Format with
    | (Some file, JUnitStyleXml) ->
      let xml = {
        Writer = new StreamWriter(file.FullName, false, Encoding.UTF8) :> TextWriter
        Formatter = Formatter.XmlFormatter.junitStyle watch
      }
      (true, [console; xml])
    | (Some file, Normal) ->
      let file = {
        Writer = new StreamWriter(file.FullName, false, Encoding.UTF8) :> TextWriter
        Formatter = console.Formatter
      }
      (false, [console; file])
    | (None, Normal) -> (false, [console])
    | (None, JUnitStyleXml) -> (true, [])

  use error =
    match args.Error with
    | Some file -> new StreamWriter(file.FullName, false, Encoding.UTF8) :> TextWriter
    | None -> Console.Error

  use reporter =
    new Reporter(
      new Printer<_>(progress, Formatter.ProgressFormatter.dot),
      new Printer<_>(outputs),
      new Printer<_>(error, Formatter.ErrorFormatter.normal))

  if args.Help then
    error.WriteLine(Args.help)

  let founds, notFounds = args.Inputs |> List.partition (fun file -> file.Exists)
  if founds |> List.isEmpty then
    reporter.ReportError("input is empty.")
    -1
  elif requireFileName && Option.isNone args.Output then
    reporter.ReportError("xml format option require 'output' option.")
    -2
  elif notFounds |> List.isEmpty then

    let testExecutor = new Persimmon.Internals.TestExecutor()
    let results = testExecutor.AsyncRunAllTests (founds |> Seq.map (fun file -> file.FullName)) (fun tr -> reporter.ReportProgress tr) false |> Async.RunSynchronously
    results |> Seq.sumBy (fun result -> result.Errors)

  else
    reporter.ReportError("file not found: " + (String.Join(", ", notFounds)))
    -2

[<EntryPoint>]
let main argv = 
  let args = Args.parse Args.empty (argv |> Array.toList)
  entryPoint args
