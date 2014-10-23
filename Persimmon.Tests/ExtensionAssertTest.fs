namespace Persimmon.Tests

open Persimmon
open Persimmon.Test
open Persimmon.ExtensionAssert
open NUnit.Framework
open Helper

[<TestFixture>]
module PowerAssertTest =

  let test1 = test "simple extension assert" {
    return! eassert { 2 == 3 }
  }

  [<Test>]
  let ``simple extension assert`` () =
    test1 |> shouldFail ("Expect: 2\nActual: 3", [])