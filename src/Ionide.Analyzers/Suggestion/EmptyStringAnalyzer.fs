module Ionide.Analyzers.Suggestion.EmptyStringAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

let (|EmptyStringConst|_|) (e: FSharpExpr) =
    if e.Type.ErasedType.BasicQualifiedName = "System.String" then
        match e with
        | FSharpExprPatterns.Const(o, _type) when not (isNull o) && (string o).Length = 0 -> Some()
        | _ -> None
    else
        None

let invalidStringFunctionUseAnalyzer (typedTree: FSharpImplementationFileContents) =
    let ranges = ResizeArray<range>()

    let walker =
        { new TypedTreeCollectorBase() with
            override _.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                match (mfv.Assembly.SimpleName, mfv.FullName, args) with
                | "FSharp.Core", "Microsoft.FSharp.Core.Operators.(=)", [ _; EmptyStringConst ]
                | "FSharp.Core", "Microsoft.FSharp.Core.Operators.(=)", [ EmptyStringConst; _ ] -> ranges.Add m
                | _ -> ()

        }

    walkTast walker typedTree

    ranges
    |> Seq.map (fun r ->
        {
            Type = "EmptyString analyzer"
            Message =
                "Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method."
            Code = "IONIDE-005"
            Severity = Warning
            Range = r
            Fixes = []
        }
    )
    |> Seq.toList

[<EditorAnalyzer("EmptyStringAnalyzer",
                 "Verifies testing for an empty string is done efficiently.",
                 "https://ionide.io/ionide-analyzers/suggestion/005.html")>]
let emptyStringEditorAnalyzer (ctx: EditorContext) =
    async {
        return
            ctx.TypedTree
            |> Option.map invalidStringFunctionUseAnalyzer
            |> Option.defaultValue []
    }

[<CliAnalyzer("EmptyStringAnalyzer",
              "Verifies testing for an empty string is done efficiently.",
              "https://ionide.io/ionide-analyzers/suggestion/005.html")>]
let emptyStringCliAnalyzer (ctx: CliContext) =
    async {
        return
            ctx.TypedTree
            |> Option.map invalidStringFunctionUseAnalyzer
            |> Option.defaultValue []
    }
