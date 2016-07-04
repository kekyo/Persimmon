namespace Persimmon.Internals

open System
open System.IO
open System.Security.Policy

open Persimmon
open System.Diagnostics

/// <summary>
/// For internal use only.
/// </summary>
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type RemoteHandlers<'T>(reporter: 'T -> unit) as this =
  inherit MarshalByRefObject()

  let persimmonAssembly = this.GetType().Assembly

  /// <summary>
  /// For internal use only.
  /// </summary>
  member __.AssemblyResolver (_: obj) (e: ResolveEventArgs) =
    if e.Name = persimmonAssembly.FullName then
      persimmonAssembly
    else
      null

  /// <summary>
  /// For internal use only.
  /// </summary>
  member this.ConstructAssemblyResolverHandler() =
    new ResolveEventHandler(this.AssemblyResolver)
        
  /// <summary>
  /// For internal use only.
  /// </summary>
  member __.Report(result: obj) =
    reporter(result :?> 'T)

  /// <summary>
  /// For internal use only.
  /// </summary>
  member this.ConstructReportAction() =
    new Action<obj>(this.Report)

/// <summary>
/// Discovery and execute persimmon based tests in separated AppDomain.
/// </summary>
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type TestExecutor() as this =

  let runTestsBySeparatedAppDomain assemblyPath methodName (handlers: RemoteHandlers<_>) =

    let appDomainId = Guid.NewGuid().ToString("N")
    let name = sprintf "Persimmon-%s" appDomainId
    let applicationBasePath = Path.GetDirectoryName(assemblyPath)
    let persimmonBasePath = (new Uri(this.GetType().Assembly.CodeBase)).LocalPath
    let shadowCopyTargets =
      System.String.Join(
        ";",
        [| applicationBasePath; persimmonBasePath |])

    let info = new AppDomainSetup()
    info.ApplicationName <- name
    info.ApplicationBase <- applicationBasePath
    info.ShadowCopyFiles <- "true"
    info.ShadowCopyDirectories <- shadowCopyTargets

    // If test assembly has configuration file, try to set.
    let configurationFilePath = assemblyPath + ".config";
    if File.Exists(configurationFilePath) then
      info.ConfigurationFile <- configurationFilePath

    // Derived current evidence.
    let evidence = new Evidence(AppDomain.CurrentDomain.Evidence);

    // Create AppDomain.
    let appDomain = AppDomain.CreateDomain(name, evidence, info)
    appDomain.add_AssemblyResolve(handlers.ConstructAssemblyResolverHandler())

    try
      let executor = (appDomain.CreateInstanceFromAndUnwrap(persimmonBasePath, "Persimmon.Internals.RemotableTestExecutor"))
      let t = executor.GetType()
      let mi = t.GetMethod(methodName)
      let result = mi.Invoke(executor, [|assemblyPath;handlers.ConstructReportAction()|])
      result

    finally
      AppDomain.Unload appDomain

  /// <summary>
  /// Discovery and execute persimmon based tests by parallelism in separated AppDomain.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param> 
  /// <returns>RunResult</returns>
  member this.AsyncRunTestsByParallel assemblyPath reporter = async {
    do! Async.SwitchToNewThread()
    let handlers = new RemoteHandlers<TestResult>(reporter)
    return runTestsBySeparatedAppDomain assemblyPath "RunTestsByParallel" handlers :?> RunResult<TestResult>
  }

  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <returns>RunResult</returns>
  member this.AsyncRunTestsBySequential assemblyPath reporter = async {
    do! Async.SwitchToNewThread()
    let handlers = new RemoteHandlers<ResultNode>(reporter)
    return runTestsBySeparatedAppDomain assemblyPath "RunTestsBySequential" handlers :?> RunResult<ResultNode>
  }
