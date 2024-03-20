module Svg
open Giraffe.ViewEngine

let svg = tag "svg"
let rect = tag "rect"
let animate = tag "animate"

let _attributeName = attr "attributeName"
let _attributeType = attr "attributeType"

let _values = attr "values"
let _dur = attr "dur"
let _fill = attr "fill"
let _begin = attr "begin"
let _repeatCount = attr "repeatCount"