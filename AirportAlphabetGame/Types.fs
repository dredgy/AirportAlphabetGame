module Types

type usernameQuery = {
    fr24user: string
    searchType: string
}

type AirportResponseData = obj[][]

type Airport = {
    Code: string
    City: string
}
