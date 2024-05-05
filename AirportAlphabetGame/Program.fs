open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open Types

module Program =

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
    let app =
        application {
            use_mime_types [(".woff", "application/font-woff")]
            use_static "wwwroot"
            use_router router
            use_developer_exceptions
            service_config ServiceConfig
        }

    run app
