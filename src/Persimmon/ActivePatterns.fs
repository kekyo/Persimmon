﻿module Persimmon.ActivePatterns

let (|Context|TestCase|) (test: ITestObject) =
  match test with
  | :? Context as ctx -> Context ctx
  | tc -> TestCase (tc.GetType().GetMethod("BoxTypeParam").Invoke(tc, [||]) :?> TestCase<obj>)

let (|ContextResult|TestResult|EndMarker|) (res: ITestResultNode) =
  match res with
  | :? ContextResult as cr -> ContextResult cr
  | marker when marker = TestResult.endMarker -> EndMarker
  | tr -> TestResult (tr.GetType().GetMethod("BoxTypeParam").Invoke(tr, [||]) :?> TestResult<obj>)
