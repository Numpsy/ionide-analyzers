module Ionide.Analyzers.Tests.Suggestion.HeadConsEmptyListPatternAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.HeadConsEmptyListPatternAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``x :: [] is detected`` () =
    async {
        let source =
            """
module M

do
    match [] with
    | x :: [] -> ()
    | _ -> ()
    """

        let ctx = getContext projectOptions source
        let! msgs = headConsEmptyListPatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs[0], Is.True)
    }
