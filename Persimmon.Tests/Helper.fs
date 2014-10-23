namespace Persimmon.Tests

open NUnit.Framework
open FsUnit
open Persimmon

module Helper =

  let shouldSucceed expected = function
    | Success actual -> actual |> should equal expected
    | Failure xs -> Assert.Fail(sprintf "%A" xs)

  let shouldFail (expectedMessage: NonEmptyList<string>) = function
    | Success x -> Assert.Fail(sprintf "Expect: Failure\nActual: %A" x)
    | Failure actual -> actual |> should equal expectedMessage
