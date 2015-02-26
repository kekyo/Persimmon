﻿namespace Persimmon.Tests

open System
open Persimmon
open UseTestNameByReflection
open PrettyPrinter

module PrettyPrinterTest =

  let ``unit type should printed "()"`` = test {
    do! assertEquals "()" (print (typeof<unit>, ()))
  }

  let ``bool should be printed lower case`` = test {
    do! assertEquals "true" (print (typeof<bool>, true))
    do! assertEquals "false" (print (typeof<bool>, false))
  }

  let ``printer should print string`` = test {
    do! assertEquals "\"\"" (print (typeof<string>, ""))
    do! assertEquals "\"a\"" (print (typeof<string>, "a"))
    do! assertEquals "null" (print (typeof<string>, null))
  }

  let ``printer should print array in a row`` = test {
    do! assertEquals "[|1; 2; 3|]" (print (typeof<int []>, [| 1; 2; 3 |]))
  }

  let ``printer should print list in a row`` = test {
    do! assertEquals "[1; 2; 3]" (print (typeof<int list>, [ 1; 2; 3 ]))
  }

  let ``printer should print seq in a row`` = test {
    do! assertEquals "seq [1; 2; 3]" (print (typeof<int seq>, seq { 1 .. 3 }))
    do! assertEquals "seq [1; 2; 3]" (print (typeof<obj>, box (seq { 1 .. 3 })))
  }

  type TestRecord = {
    Field1: int
    Field2: string
  }

  let ``printer should print record in a row`` = test {
    do! assertEquals """{ Field1 = 1; Field2 = "test" }""" (print (typeof<TestRecord>, { Field1 = 1; Field2 = "test" }))
  }

  let ``printer should print tuple`` = test {
    do! assertEquals "(1, 2)" (print (typeof<int * int>, (1, 2)))
    do! assertEquals "(1, (2, 3))" (print (typeof<int * (int * int)>, (1, (2, 3))))
    do! assertEquals "(1, seq [1; 2])" (print (typeof<int * int seq>, (1, seq { 1 .. 2 })))
  }

  type TestUnion =
    | Case1
    | Case2 of bool
    | Case3 of int * int

  let ``printer should print DU in a row`` = test {
    do! assertEquals "Case1" (print (typeof<TestUnion>, Case1))
    do! assertEquals "Case1" (print (typeof<obj>, box Case1))
    do! assertEquals "Case2(true)" (print (typeof<TestUnion>, Case2 true))
    do! assertEquals "Case3(0, 1)" (print (typeof<TestUnion>, Case3(0, 1)))
  }

  let ``printer should print option`` = test {
    do! assertEquals "None" (print (typeof<obj option>, None))
    do! assertEquals "Some(true)" (print (typeof<bool option>, Some true))
  }

  let ``printer should print Nullable`` = test {
    do! assertEquals "null" (print (typeof<Nullable<int>>, Nullable()))
    do! assertEquals "1" (print (typeof<Nullable<int>>, 1))
  }
