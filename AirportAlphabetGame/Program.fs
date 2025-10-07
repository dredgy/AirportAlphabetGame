open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Saturn
open Giraffe

open Types
open Thoth.Json.Giraffe
open System
open System.Net
open System.Net.Sockets
open System.IO


module Program =

    let router = router {
        not_found_handler (setStatusCode 404 >=> text "404")
        get "/" ( (View.index [||]) |> htmlView)
        getf "/%s" (fun username -> htmlView(Controller.RenderPageWithUser username))
        getf "/%s/%s" (fun (username, searchType) -> htmlView(Controller.RenderPageWithUserAndSearchType searchType username))
        post "/search" (bindJson<usernameQuery> (fun username ->
            fun next ctx ->
                ctx.Response.Headers.Add("HX-Push-Url", $"/{Controller.parseUsername username.fr24user}/{username.searchType}")
                htmlView (Controller.RenderAirportList username) next ctx
        ))
    }

    let ServiceConfig (services: IServiceCollection) =
        // Get the server IP address
        services.AddSingleton<Json.ISerializer>(ThothSerializer()) |> ignore
        let serverIpAddress =
            match Dns.GetHostEntry(Dns.GetHostName()).AddressList |> Array.tryFind(fun ip -> ip.AddressFamily = AddressFamily.InterNetwork) with
            | Some ip -> ip.ToString()
            | None -> "IP address not found"

        let boldCode = "\u001b[1m"
        let greenCode = "\u001b[32m"
        let resetCode = "\u001b[0m"

        // Print the server IP address
        printfn $"{boldCode}Now Running On: {greenCode}%s{serverIpAddress}{resetCode}"
        services.AddHttpContextAccessor()


    let app =
        application {
            use_mime_types [(".woff", "application/font-woff")]
            use_router router
            use_static (Path.Combine(AppContext.BaseDirectory, "wwwroot"))
            use_developer_exceptions
            service_config ServiceConfig
            url "http://0.0.0.0:5001"
        }

    run app
