module Ionide.Analyzers.Tests.Suggestion.StructDiscriminatedUnionAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text.Range
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.StructDiscriminatedUnionAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``du without any field values`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``negative: du is already struct`` () =
    async {
        let source =
            """module Lib

[<Struct>]
type Foo =
    | Bar
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``du with only primitive field values`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar of int
    | Barry of float
    | Bear of System.DateTime
    | B4 of string
    | B5 of System.TimeSpan
    | B6 of int16 * int64 * uint * uint16 * byte * sbyte * float32 * decimal * char * bool
    | B7 of System.Guid
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``fix data for simple type`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar of int
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>]\n", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``fix data for indented type `` () =
    async {
        let source =
            """namespace Lib

module N =
    type Foo =
        | Bar of int
        | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>]\n    ", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``fix data for recursive type`` () =
    async {
        let source =
            """module Lib

type X = int 

and Foo =
    | Bar of int
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>] ", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``negative: du field with function type`` () =
    async {
        let source =
            """module Lib

[<Struct>]
type Foo =
    | Bar of (int -> int)
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
