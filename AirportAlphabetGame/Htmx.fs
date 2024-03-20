module Htmx

open Giraffe.ViewEngine

let _hxGet = attr "data-hx-get"
let _hxPost = attr "data-hx-post"
let _hxTrigger = attr "data-hx-trigger"
let _hxTarget = attr "data-hx-target"
let _hxExt = attr "data-hx-ext"
let _hxSwap = attr "data-hx-swap"
let _hxReplaceUrl = attr "data-hx-replace-url"
let _hxValidate = attr "data-hx-validate"