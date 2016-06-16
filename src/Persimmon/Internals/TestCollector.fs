﻿namespace Persimmon.Internals

open System
open System.Diagnostics
open System.Reflection
open Microsoft.FSharp.Collections
open Persimmon

module private TestCollectorImpl =

  let publicTypes (asm: Assembly) =
    asm.GetTypes()
    |> Seq.filter (fun typ -> typ.IsPublic && typ.IsClass && not typ.IsGenericTypeDefinition)

  let private publicNestedTypes (typ: Type) =
    typ.GetNestedTypes()
    |> Seq.filter (fun typ -> typ.IsNestedPublic && typ.IsClass && not typ.IsGenericTypeDefinition)

  let private enumerableGenericParameterType (typ: Type) =
    if not typ.IsGenericType then None
    else
      let gtd = typ.GetGenericTypeDefinition()
      if typedefof<System.Collections.Generic.IEnumerable<obj>> <> gtd then None
      else Some (typ.GetGenericArguments().[0])

  let rec private isTestTargetType (typ: Type) =
    if typeof<TestMetadata>.IsAssignableFrom typ then true
    else
      match enumerableGenericParameterType typ with
      | Some innerType -> isTestTargetType innerType
      | None -> false

  /// Traverse test instances recursive.
  /// <param name="partialSuggest">Nested sequence children is true: symbol naming is pseudo.</param>
  let rec private fixupAndCollectTests (testObject: obj, symbolName: string, index: int option) = seq {
    match testObject with

    /////////////////////////////////////////////////////
    // For test case:
    | :? TestCase as testCase ->
      // Set symbol name.
      match index with
      | Some i -> testCase.trySetIndex i
      | None -> testCase.trySetSymbolName symbolName
      yield testCase :> TestMetadata

    /////////////////////////////////////////////////////
    // For context:
    | :? Context as context ->
      // Set symbol name.
      match index with
      | Some i -> context.trySetIndex i
      | None -> context.trySetSymbolName symbolName
      yield context :> TestMetadata

    /////////////////////////////////////////////////////
    // For test objects (sequence, ex: array/list):
    // let tests = [                     --+
    //  test "success test(list)" {        |
    //    ...                              |
    //  }                                  | testObject
    //  test "failure test(list)" {        |
    //    ...                              |
    //  }                                  |
    // ]                                 --+
    | :? (TestMetadata seq) as tests ->
      // Nested children's symbol naming is pseudo.
      // "parentNamed[0]", "parentNamed[0][0]", ...
      let children =
        tests
        |> Seq.mapi (fun index -> fun child -> child, index)
        |> Seq.collect (fun entry -> fixupAndCollectTests(fst entry, symbolName, Some (snd entry)))
      yield new Context(symbolName, children) :> TestMetadata

    /////////////////////////////////////////////////////
    // Unknown type, ignored.
    | _ -> ()
  }

  /// Retreive test object via target property, and traverse.
  let private collectTestsFromProperty (p: PropertyInfo) =
    fixupAndCollectTests (p.GetValue(null, null), p.Name, None)
  
  /// Retreive test object via target method, and traverse.
  let private collectTestsFromMethod (m: MethodInfo) =
    fixupAndCollectTests (m.Invoke(null, [||]), m.Name, None)
  
  /// Retreive test object via target type, and traverse.
  let rec collectTests (typ: Type) = seq {
    // For properties (value binding):
    yield!
      typ.GetProperties(BindingFlags.Static ||| BindingFlags.Public)
      // Ignore setter only property / indexers
      |> Seq.filter
        (fun p ->
          p.CanRead &&
          (p.GetGetMethod() <> null) &&
          (p.GetIndexParameters() |> Array.isEmpty) &&
          (isTestTargetType p.PropertyType))
      |> Seq.collect collectTestsFromProperty
    // For methods (function binding):
    yield!
      typ.GetMethods(BindingFlags.Static ||| BindingFlags.Public)
      // Ignore getter methods / open generic methods / method has parameters
      |> Seq.filter
        (fun m ->
          not m.IsSpecialName &&
          not m.IsGenericMethodDefinition &&
          (m.GetParameters() |> Array.isEmpty) &&
          (isTestTargetType m.ReturnType))
      |> Seq.collect collectTestsFromMethod
    // For nested modules:
    for nestedType in publicNestedTypes typ do
      let testCases = collectTests nestedType |> Seq.toArray
      if Array.isEmpty testCases then ()
      else yield new Context(nestedType.Name, testCases) :> TestMetadata
  }

[<Sealed>]
type TestCollector() =

  /// Collect test cases from assembly
  let collect targetAssembly =
    targetAssembly
    |> TestCollectorImpl.publicTypes
    |> Seq.map (fun typ -> new Context(typ.FullName, TestCollectorImpl.collectTests typ))

  /// Remove contexts and flatten structured test objects.
  let rec flattenTestCase (testMetadata: TestMetadata) = seq {
    match testMetadata with
    | :? Context as context ->
      for child in context.Children do
        yield! flattenTestCase child
    | :? TestCase as testCase -> yield testCase
    | _ -> ()
  }

  /// Collect tests with basic procedure.
  member __.Collect targetAssembly =
    collect targetAssembly |> Seq.toArray

  /// Collect test cases.
  member __.CollectOnlyTestCases targetAssembly =
    collect targetAssembly
    |> Seq.collect flattenTestCase
    |> Seq.toArray

  /// CollectAndCallback collect test cases and callback. (Internal use only)
  member __.CollectAndCallback(targetAssembly, callback: Action<obj>) =
    collect targetAssembly
    |> Seq.collect flattenTestCase
    |> Seq.iter (fun testCase -> callback.Invoke testCase)
