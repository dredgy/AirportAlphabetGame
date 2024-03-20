open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open Types

module Program =

    let pipeline = pipeline {
        use_warbler
    }

    let router = router {
        not_found_handler (setStatusCode 404 >=> text "404")
        get "/" ( (View.index [||]) |> htmlView)
        getf "/%s" (fun username -> htmlView(Controller.RenderPageWithUser username))
        post "/search" (bindJson<usernameQuery> (fun username ->
            fun next ctx ->
                ctx.Response.Headers.Add("HX-Replace-Url", Controller.parseUsername username.fr24user)
                htmlView (Controller.RenderAirportList username) next ctx
        ))
    }

    let ServiceConfig (services: IServiceCollection) = services.AddHttpContextAccessor()
    let ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[2];
    let app =
        application {
            use_mime_types [(".woff", "application/font-woff")]
            use_static "public"
            use_router router
            service_config ServiceConfig
            url "http://*:5001"
        }

    run app
