namespace DDDApi

module Array =
    let public findOrNone matcher items =
        match items |> Array.exists matcher with
        | true -> Some(items |> Array.find matcher)
        | _ -> None
