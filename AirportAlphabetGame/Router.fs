module Router

open System
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

let inline (~%) (x: ^A) : ^B = (^B : (static member From: ^A -> ^B) x)

type RouteMethod = Get | Post
type Static<'x> =
    | HtmlDocument of (unit -> Giraffe.ViewEngine.HtmlElements.XmlNode)
    | HtmlElement of (unit -> Giraffe.ViewEngine.HtmlElements.XmlNode)
    | HtmlElements of (unit -> Giraffe.ViewEngine.HtmlElements.XmlNode list)
    | HtmlString of (unit -> string)
    | Text of (unit -> string)
    | Json of (unit -> 'x)

type Dynamic<'x, 'y> =
    | HtmlDocument of ('x -> Giraffe.ViewEngine.HtmlElements.XmlNode)
    | HtmlElement of ('x -> Giraffe.ViewEngine.HtmlElements.XmlNode)
    | HtmlElements of ('x -> Giraffe.ViewEngine.HtmlElements.XmlNode list)
    | HtmlString of ('x -> string)
    | Text of ('x -> string)
    | Json of ('x -> 'y)


let private GetMimeTypeFromStaticResponseFunction (func: Static<'x>) =
    match func with
        | Static.HtmlDocument _
        | Static.HtmlElements _
        | Static.HtmlElement _
        | Static.HtmlString _ -> "text/html"
        | Static.Json _ -> "application/json"
        | Static.Text _ -> "text/plain"

let private GetMimeTypeFromDynamicResponseFunction (func: Dynamic<'x, 'y>) =
    match func with
        | Dynamic.HtmlDocument _
        | Dynamic.HtmlElements _
        | Dynamic.HtmlElement _
        | Dynamic.HtmlString _ -> "text/html"
        | Dynamic.Json _ -> "application/json"
        | Dynamic.Text _ -> "text/plain"

let StaticView (func: unit -> Giraffe.ViewEngine.HtmlElements.XmlNode) = Static.HtmlDocument func


let BuildApp (args: string[]) =
    WebApplication
        .CreateBuilder(args)
        .Build()


let AddStaticRoute (route: string) (func: Static<'x>) (app: WebApplication) =
    let htmlResponseFunc (context: HttpContext) =
        let htmlContent =
            match func with
                | Static.HtmlDocument view -> view () |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument
                | Static.HtmlElement view -> view () |> Giraffe.ViewEngine.RenderView.AsString.htmlNode
                | Static.HtmlElements view -> view () |> Giraffe.ViewEngine.RenderView.AsString.htmlNodes
                | Static.HtmlString view -> view ()
                | Static.Text view -> view ()
                | Static.Json view -> view () |> Thoth.Json.Net.Encode.Auto.toString
        context.Response.ContentType <- GetMimeTypeFromStaticResponseFunction func
        context.Response.WriteAsync(htmlContent)

    app.MapGet(route, Func<HttpContext, Task>(htmlResponseFunc))
        |> ignore
    app

let AddDynamicRoute (route: string) (func: Dynamic<'x, 'y>) (app: WebApplication) =
    let dynamicResponseFunc (context: HttpContext) =
        let param = context.Request.RouteValues.Values.First() :?> 'x
        let content =
            match func with
                | Dynamic.HtmlDocument view -> view param |> Giraffe.ViewEngine.RenderView.AsString.htmlDocument
                | Dynamic.HtmlElement view -> view param |> Giraffe.ViewEngine.RenderView.AsString.htmlNode
                | Dynamic.HtmlElements view -> view param |> Giraffe.ViewEngine.RenderView.AsString.htmlNodes
                | Dynamic.HtmlString view -> view param
                | Dynamic.Text view -> view param
                | Dynamic.Json view -> view param |> Thoth.Json.Net.Encode.Auto.toString
        context.Response.ContentType <- GetMimeTypeFromDynamicResponseFunction func
        context.Response.WriteAsync(content)

    app.MapGet(route, dynamicResponseFunc) |> ignore
    app

let Run (app: WebApplication) =
    app.Run()
    0