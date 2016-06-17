﻿namespace Persimmon.Internals

open System
open System.IO
open System.Security.Policy

open Persimmon

/// <summary>
/// For internal use only.
/// </summary>
type RemotableReporter(reporter: TestResult -> unit) =
  inherit MarshalByRefObject()

  interface IRemoteReporter with
    /// <summary>
    /// For internal use only.
    /// </summary>
    member __.ReportProgress testResult = reporter testResult

/// <summary>
/// Discovery and execute persimmon based tests in separated AppDomain.
/// </summary>
[<Sealed; NoEquality; NoComparison; AutoSerializable(false)>]
type TestExecutor() =

  let asyncRunTests assemblyPath (reporter: TestResult -> unit) isParallel = async {

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

    try
      let executor = (appDomain.CreateInstanceFromAndUnwrap(persimmonBasePath, "Persimmon.Internals.RemotableTestExecutor")) :> RemotableTestExecutor
      let remotableReporter = new RemotableReporter(reporter) :> IRemoteReporter
      executor.RunTests assemblyPath remotableReporter isParallel

    finally
      AppDomain.Unload appDomain
  }

  /// <summary>
  /// Discovery and execute persimmon based tests in separated AppDomain.
  /// </summary>
  /// <param name="assemblyPath">Target assembly path.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <param name="isParallel">Parallel execution.</param>
  member this.AsyncRunTests assemblyPath reporter isParallel = async {
    do! Async.SwitchToThreadPool()
    do! asyncRunTests assemblyPath reporter isParallel
  }

  /// <summary>
  /// Discovery and execute persimmon based tests in separated AppDomain.
  /// </summary>
  /// <param name="assemblyPaths">Target assembly paths.</param>
  /// <param name="reporter">Progress reporter.</param>
  /// <param name="isParallel">Parallel execution.</param>
  member this.RunTests assemblyPaths reporter isParallel =
    if isParallel then
      assemblyPaths |> Seq.map (fun path -> asyncRunTests path reporter isParallel) |> Async.Parallel
    else async {
      for path in assemblyPaths do
        do! asyncRunTests path reporter false |> Async.RunSynchronously
    }
