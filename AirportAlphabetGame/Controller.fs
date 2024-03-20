module Controller

open System.Net.Http
open System.Text.RegularExpressions
open Giraffe.ViewEngine.HtmlElements
open Types
open System.Text
open Thoth.Json.Net


let parseUsername (username: string) =
    let pattern = @"https?://(?:www\.)?my\.flightradar24\.com/([^/?#]+)"
    let matches = Regex.Match(username, pattern)

    if matches.Success then matches.Groups[1].Value
    else username

let getJsonResult result =
    match result with
        | Error _ -> failwith "Invalid Json"
        | Ok finalResult -> finalResult


let makePostRequest<'x> (url: string) payload =
    async {
        use httpClient = new HttpClient()
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        let request = new HttpRequestMessage(HttpMethod.Post, url)
        request.Content <- content
        let! response = httpClient.SendAsync(request) |> Async.AwaitTask
        let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return responseContent
    }
    |> Async.RunSynchronously
    |> Decode.Auto.fromString<'x>
    |> getJsonResult


let decodeResponseData (data: AirportResponseData) =
    data
        |> Array.map (fun airport -> {Code=string airport[0];City=string airport[1]})

let groupAirportsByLetter (airports: Airport[]) =
    airports
        |> Array.groupBy (_.Code[0])
        |> Array.map snd

let getAirportAbbrTags airportGroup =
    airportGroup
    |> Array.distinct
    |> Array.sortBy (_.Code)
    |> Array.map View.airportAbbr


let processAirports (alphabet: char[]) (allAirports: Airport[])  =

    let intersperseCommas (nodes: XmlNode seq) =
        nodes
        |> Seq.mapi (fun i node -> if i < Seq.length nodes - 1 then [node; View.comma] else [node])
        |> Seq.concat
        |> Seq.toArray

    // Group airports by the first letter of their code
    let groupedAirports =
        allAirports
        |> Seq.groupBy (fun airport -> airport.Code.[0])
        |> Seq.map (fun (key, group) ->
            let sortedGroup = group |> Seq.sortBy (fun airport -> airport.Code)
            key, (sortedGroup |> Seq.map View.airportAbbr |> intersperseCommas))
        |> dict


    let dictionary =
        alphabet
        |> Array.map (fun letter ->
            let key = letter.ToString().ToUpper()
            match groupedAirports.TryGetValue(letter) with
            | (true, value) -> key, value
            | _ -> key, [||])

    dictionary
let RenderAirportList (user: usernameQuery) =
    let username = parseUsername user.fr24user
    let alphabet = [|'A'..'Z'|]
    let airports =
        $"username={username}&listType=airports&order=no&limit=0"
            |> makePostRequest<AirportResponseData> "https://my.flightradar24.com/public-scripts/profileToplist"
            |> decodeResponseData
            |> processAirports alphabet

    let numberOfAirportsNotFlown =
        airports
        |> Array.filter(fun (_, nodes) -> Array.isEmpty nodes)
        |> Array.length

    let percentageOfLettersWithNoAirports = (float numberOfAirportsNotFlown / float alphabet.Length) * 100.0
    let message = $"{username} has flown {System.Math.Round(100. - percentageOfLettersWithNoAirports, 1)}%% of the alphabet!"


    airports
        |> Array.map (fun (letter, nodes) -> View.tableRow letter nodes)
        |> View.table message username

let RenderPageWithUser (username: string) =
    let airportList = RenderAirportList {fr24user = username}
    View.index [|airportList|]