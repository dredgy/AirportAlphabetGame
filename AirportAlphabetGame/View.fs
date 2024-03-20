module View

open System
open Htmx
open Giraffe.ViewEngine
open Giraffe.Core
open Types

let index content =
    let hide =
        match content |> Seq.length with
        | 0 -> "hide"
        | _ -> ""

    html [] [
        head [] [
            meta [_name "viewport"; _content "width=device-width"]
            title [] [str "Have you flown the alphabet?"]
            link [_rel "stylesheet" ; _href "/styles/core.css"]
            script [_src "/scripts/htmx.min.js"] []
            script [_src "/scripts/json-enc.js"] []
            script [_src "/scripts/index.js"] []
        ]
        body [] [
            div [_id "pageContainer"] [
                section [_id "input"] [
                    article [] [
                        h1 [] [str "Have you flown the alphabet?"]
                        form [
                              _id "username"
                              _hxTrigger "submit"
                              _hxExt "json-enc"
                              _hxPost "/search"
                              _hxTarget "#results"
                              _hxSwap "innerHTML show:top"
                              ] [
                            input [_type "text"; _name "fr24user"; _autocomplete "off"; _placeholder "Enter your MyFlightRadar24 username..."; _required; _hxValidate "true"; _pattern ".{4,}"]
                            button [_type "submit";] [str "Let's find out!"]
                            Svg.svg [_width "200"; _height "30"] [
                                Svg.rect [_width "200"; _height "30"; Svg._fill "lightgray"] []
                                Svg.rect [_width "50"; _height "30"; Svg._fill "white"] [
                                    Svg.animate [Svg._attributeName "x"; Svg._attributeType "XML"; Svg._values "0;150;0"; Svg._dur "2s"; Svg._begin "0s"; Svg._repeatCount "indefinite"] []
                                ]
                            ]
                        ]
                    ]
                ]
                section [_id "results"; _class hide; attr "hx-on::after-swap" "document.getElementById('results').style.display='flex'"] [
                   yield! content
                ]
            ]
        ]
    ]

let airportAbbr airport = abbr [_title airport.City] [str airport.Code]
let comma = str ", "

let tableRow (letter: string ) (codes: XmlNode[]) =
    let className =
        match codes.Length with
        | 0 -> "not-flown"
        | _ -> "flown"

    tr [_class className] [
        th [] [str letter]
        td [] [
            yield! codes
        ]
    ]

let table (message) (user: string) (rows: XmlNode[]) =
    article [] [
        h1 [] [str message]
        table [] [
            tr [] [
                th [_colspan "2"] [str $"{user}'s Airports Flown"]
            ]
            yield! rows
        ]
    ]


let name (name:usernameQuery) : HttpHandler = h1 [] [str name.fr24user] |> htmlView
let error = b [] [str "Error"]