module Controller

open System
open System.Net.Http
open System.Text.RegularExpressions
open Giraffe
open Giraffe.ViewEngine.HtmlElements
open Types
open System.Text
open System.Net
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
        use handler = new HttpClientHandler()
        handler.AutomaticDecompression <- DecompressionMethods.GZip ||| DecompressionMethods.Deflate
        use httpClient = new HttpClient(handler)

        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        let request = new HttpRequestMessage(HttpMethod.Post, url)
        request.Content <- content
        request.Headers.Add("User-Agent", "Mozilla/5.0") // mimic browser
        request.Headers.Add("Accept", "application/json")

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
    |> Array.sortBy _.Code
    |> Array.map View.airportAbbr

let rec extractTextValues (node: XmlNode) =
    match node with
    | Text code when code.Trim().Length > 1 -> [| code |] // Only include codes with exactly three letters
    | ParentNode (_, children) ->
        children
        |> List.toArray
        |> Array.collect extractTextValues // Collect all text values from children
    | _ -> [| |] // Ignore any other node types


let formatAirportsPlainText (airports: (string * XmlNode[])[]) (message: string) =
    message + "\n" + (airports
    |> Array.map (fun (letter, nodes) ->
        let airportCodes =
            nodes
            |> Array.collect extractTextValues
        match airportCodes with
        | [||] -> letter
        | _ -> String.concat ", " airportCodes
    )
    |> String.concat "\n")

let processAirports (alphabet: char[]) (allAirports: Airport[])  =

    let intersperseCommas (nodes: XmlNode seq) =
        nodes
        |> Seq.mapi (fun i node -> if i < Seq.length nodes - 1 then [node; View.comma] else [node])
        |> Seq.concat
        |> Seq.toArray

    // Group airports by the first letter of their code
    let groupedAirports =
        allAirports
        |> Seq.groupBy _.Code.[0]
        |> Seq.map (fun (key, group) ->
            let sortedGroup = group |> Seq.sortBy _.Code
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
        $"username={username}&listType={user.searchType}&order=no&limit=0"
            |> makePostRequest<AirportResponseData> "https://my.flightradar24.com/public-scripts/profileToplist"
            |> decodeResponseData
            |> processAirports alphabet


    let numberOfAirportsNotFlown =
        airports
        |> Array.filter (fun (_, nodes) -> Array.isEmpty nodes)
        |> Array.length

    let percentageOfLettersWithNoAirports = (float numberOfAirportsNotFlown / float alphabet.Length) * 100.0
    let percentageOfAlphabetString = $"{System.Math.Round(100. - percentageOfLettersWithNoAirports, 1)}%% of the alphabet!"
    let message = $"{username} has flown {percentageOfAlphabetString}"
    let messagePlainText = $"I've flown {percentageOfAlphabetString}"

    let title = $"{username}'s {user.searchType} flown"
    let plaintextAirports = formatAirportsPlainText airports messagePlainText
    airports
        |> Array.map (fun (letter, nodes) -> View.tableRow letter nodes)
        |> View.table message title plaintextAirports

let RenderPageWithUserAndSearchType (searchType: string) (username: string)  =
    let airportList = RenderAirportList {fr24user = username; searchType = searchType}
    View.index [|airportList|]

let RenderPageWithUser = RenderPageWithUserAndSearchType "airports"