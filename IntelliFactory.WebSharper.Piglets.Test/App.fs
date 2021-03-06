// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2013 IntelliFactory
//
// For open source use, WebSharper is licensed under GNU Affero General Public
// License v3.0 (AGPLv3) with open-source exceptions for most OSS license types
// (see http://websharper.com/licensing). This enables you to develop open
// source WebSharper applications royalty-free, without requiring a license.
// However, for closed source use, you must acquire a developer license.
//
// Please contact IntelliFactory for licensing and support options at
// {licensing|sales @ intellifactory.com}.
//
// $end{copyright}

module App

open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.Piglets

module Model =

    type Name = { firstName: string; lastName: string }

    type Gender = Male | Female

    type User =
        { name: Name; age: int; gender: Gender; comments: string; participates: bool; friends: Name[] }

        [<JavaScript>]
        static member Pretty u =
            u.name.firstName + " " + u.name.lastName
            + ", aged " + string u.age
            + if u.gender = Male then ", male" else ", female"
            + if u.comments = "" then "\nNo comment" else ("\n" + u.comments)
            + if u.participates then "\nParticipates" else "\nDoesn't participate"

module ViewModel =

    open Model
    module V = Piglet.Validation

    [<JavaScript>]
    let NotANumber x =
        V.Is (fun s -> int s = 0) "I am not a number! I am a free man!" x

    [<JavaScript>]
    let Name init =
        Piglet.Return (fun f l -> { firstName = f; lastName = l })
        <*> (Piglet.Yield init.firstName
            |> V.Is V.NotEmpty "First name should not be empty."
            |> NotANumber)
        <*> (Piglet.Yield init.lastName
            |> V.Is V.NotEmpty "Last name should not be empty."
            |> NotANumber)
        |> Piglet.MapViewArgs (fun f l -> (f, l))

    [<JavaScript>]
    let User init =
        Piglet.Return (fun n a g c p f -> { name = n; age = a; gender = g; comments = c; participates = p; friends = f })
        <*> Name init.name
        <*> (Piglet.Yield init.age
            |> V.Is (fun a -> a >= 18) "You must be over 18.")
        <*> Piglet.Yield init.gender
        <*> Piglet.Yield init.comments
        <*> Piglet.Yield init.participates
        <*> Piglet.ManyInit init.friends { firstName = ""; lastName = "" } Name
        |> Piglet.TransmitReader
        |> Piglet.WithSubmit
        |> Piglet.Run (fun u ->
            JavaScript.Alert (Model.User.Pretty u))

module View =

    open Model
    open IntelliFactory.WebSharper.Html
    module C = IntelliFactory.WebSharper.Piglets.Controls

    [<JavaScript>]
    let RedBgOnError (r: Reader<'a>) =
        C.CssResult r "background-color" (fun x ->
            if x.isSuccess then "white" else "#ffa0a0")

    [<JavaScript>]
    let User init =
        ViewModel.User init
        |> Piglet.Render (fun (firstName, lastName) age gender comments participates friends liveUser submit ->
            let nameInput s =
                C.Input s |> RedBgOnError (s.Through liveUser)
            Div [
                Div [nameInput firstName |> C.WithLabel "First name:"]
                Div [nameInput lastName |> C.WithLabel "Last name:"]
                Div [C.Radio gender [Male, "Male"; Female, "Female"]]
                Div [C.IntInput age |> RedBgOnError (age.Through liveUser) |> C.WithLabel "Age:"]
                Div [C.CheckBox participates |> C.WithLabel "Participate in the survey"]
                Div [C.TextArea comments |> C.WithLabel "Comments:"]
                Div [] |> C.RenderMany friends (fun ops (first, last) ->
                    Div [
                        nameInput first
                        nameInput last
                        C.Button ops.Delete -< [B [Text "Delete this friend"]]
                        C.ButtonValidate ops.MoveUp -< [Text "Move up"]
                        C.ButtonValidate ops.MoveDown -< [Text "Move down"]
                    ])
                    |> C.WithLabel "Friends:"
                Div [C.Button friends.Add -< [B [Text "Add a friend"]]]
                Table [
                    TBody [
                        TR [
                            TH [Attr.ColSpan "7"] -< [Text "Summary"]
                        ]
                        TR [
                            TH [Text "First name"]
                            TH [Text "Last name"]
                            TH [Text "Gender"]
                            TH [Text "Age"]
                            TH [Text "Participates"]
                            TH [Text "Comments"]
                            TH [Text "Friends"]
                        ]
                        TR [
                            // These will only show up if the whole user is valid
                            TD [] |> C.ShowString liveUser (fun u -> u.name.firstName)
                            TD [] |> C.ShowString liveUser (fun u -> u.name.lastName)
                            TD [] |> C.ShowString liveUser (fun u -> if u.gender = Male then "Male" else "Female")
                            TD [] |> C.ShowString liveUser (fun u -> string u.age)
                            // This one will show up even if other parts are invalid
                            // because it uses the `participates` stream instead of `liveUser`
                            TD [] |> C.ShowString participates (function
                                    | true -> "Yes"
                                    | false -> "No")
                                |> C.Css participates "font-weight" (function
                                    | true -> "bold"
                                    | false -> "normal")
                            TD [] |> C.Show liveUser (function
                                | {comments = ""} -> [I [Text "(no comment)"]]
                                | {comments = c} -> [Span [Text c]])
                            TD [] |> C.ShowString friends
                                (Seq.map (fun n -> n.firstName + " " + n.lastName)
                                >> String.concat ", ")
                        ]
                    ]
                ]
                Div [] |> C.ShowErrors liveUser (fun msgs ->
                    [
                        Div [Attr.Style "border:solid 1px #c00;color:#c00;margin:10px;padding:5px"] -< [
                            for m in msgs do
                                yield Span [Text m]
                                yield Br []
                        ]
                    ])
                Div [C.Submit submit]
                Div [
                    Br []; Br []
                    C.IntInput age |> C.WithLabel "Age again:"
                    Span [Text "(just to test several inputs connected to the same stream)"]
                ]
            ])

[<JavaScript>]
let UI() =
    View.User {
        name = { firstName = "John"; lastName = "Rambo" }
        age = 40
        gender = Model.Male
        comments = "Blah blah blah"
        participates = true
        friends =
            [|
                { firstName = "Ernesto"; lastName = "Rodriguez" }
                { firstName = "Loic"; lastName = "Denuziere" }
            |]
    }