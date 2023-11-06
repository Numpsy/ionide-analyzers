﻿module Ionide.Analyzers.Hints.CopyAndUpdateRecordChangesAllFieldsAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

type UpdateRecord = SynExprRecordField list * range

[<CliAnalyzer("CopyAndUpdateRecordChangesAllFieldsAnalyzer",
              "Detect if all fields of an update record are updated.",
              "https://ionide.io/ionide-analyzers/hints/001.html")>]
let copyAndUpdateRecordChangesAllFieldsAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            let untypedRecordUpdates =
                let xs = ResizeArray<UpdateRecord>()

                let collector =
                    { new SyntaxCollectorBase() with
                        override x.WalkExpr(e: SynExpr) =
                            match e with
                            | SynExpr.Record(copyInfo = Some _; recordFields = fields) -> xs.Add(fields, e.Range)
                            | _ -> ()
                    }

                walkAst collector context.ParseFileResults.ParseTree
                Seq.toList xs

            let messages = ResizeArray<Message> untypedRecordUpdates.Length

            let tastCollector =
                { new TypedTreeCollectorBase() with
                    override x.WalkNewRecord (mRecord: range) (recordType: FSharpType) =
                        let matchingUnTypedNode =
                            untypedRecordUpdates
                            |> List.tryFind (fun (_, mExpr) -> Range.equals mExpr mRecord)

                        match matchingUnTypedNode with
                        | None -> ()
                        | Some(fields, mExpr) ->

                        if not recordType.TypeDefinition.IsFSharpRecord then
                            ()
                        else if recordType.TypeDefinition.FSharpFields.Count = fields.Length then
                            messages.Add
                                {
                                    Type = "CopyAndUpdateRecordChangesAllFieldsAnalyzer analyzer"
                                    Message =
                                        "All record fields of record are being updated. Consider creating a new instance instead."
                                    Code = "IONIDE-001"
                                    Severity = Warning
                                    Range = mExpr
                                    Fixes = []
                                }
                }

            match context.TypedTree with
            | None -> ()
            | Some typedTree -> typedTree.Declarations |> List.iter (walkTast tastCollector)

            return Seq.toList messages
        }
