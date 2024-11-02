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
            link [_rel "stylesheet" ; _href "/styles/core.css?v=56"]
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
                            section [] [
                                input [_type "text"; _name "fr24user"; _autocomplete "off"; _placeholder "Enter your MyFlightRadar24 username..."; _required; _hxValidate "true"; _pattern ".{4,}"]
                                button [_type "submit";] [str "Let's find out!"]
                                img [_class "loading"; _alt "Loading..."; _width "200"; _height "30"; _src "/images/loading.svg"]
                            ]
                            section [] [
                                label [_class "checked"] [
                                    input [_checked; _type "radio"; _name "searchType"; _value "airports"]
                                    str "Airports"
                                ]
                                label [] [
                                    input [_type "radio"; _name "searchType"; _value "airlines"]
                                    str "Airlines"
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

let table (message) (title: string) (plaintextAirports: string) (rows: XmlNode[])  =
    article [] [
        h1 [] [str message]
        table [] [
            tr [] [
                th [_colspan "2"] [
                    str title
                    small [(attr "data-airportList") plaintextAirports; _onclick "navigator.clipboard.writeText(this.getAttribute('data-airportList')).then(() => { const originalText = this.innerText; this.innerText = '✅'; this.style.color = 'green'; setTimeout(() => { this.innerText = originalText; this.style.color = ''; }, 1000); })"] [str "📋"]
                ]
            ]
            yield! rows
        ]
        br []
    ]


let name (name:usernameQuery) : HttpHandler = h1 [] [str name.fr24user] |> htmlView
let error = b [] [str "Error"]