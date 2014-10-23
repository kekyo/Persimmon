// TODO: rename module
module Persimmon.ExtensionAssert

open Quotations.Patterns

// TODO: type safe eval
let rec eval = function
  | Sequential(expr, _) -> eval expr
  | Call(_, info, xs) ->
    match eval xs.[0], eval xs.[1] with
    | Success a, Success b -> Test.check a b
    | Failure (res1, rest1), Failure(res2, rest2) ->
       Failure (res1, rest1@(res2::rest2))
    | Failure nel, _ -> Failure nel
    | _, Failure nel -> Failure nel
  | Value (v, t) -> Success v
  | x -> failwithf "not match:\n%A" x

type ExtensionAssertBuilder (k) =
  member __.Zero() = Success ()
  member __.Quote() = ()
  member __.Run(q) = eval q

let eassert = ExtensionAssertBuilder(id)

let (==)<'T when 'T : equality> (a: 'T) (b: 'T) = ()
