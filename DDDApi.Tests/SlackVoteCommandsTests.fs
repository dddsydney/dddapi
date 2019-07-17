module SlackVoteCommandsTests

open Xunit
open SlackVoteCommands
open DDDApi

[<Fact>]
let ``extract year and pronoun`` () =
    let year, pronoun = extractFields "2019 male"

    Assert.Equal(year, "2019")
    Assert.Equal(pronoun.Value, "male")

[<Fact>]
let ``extract year only`` () =
    let year, pronoun = extractFields "2019"

    Assert.Equal(year, "2019")
    Assert.True(pronoun.IsNone)

[<Fact>]
let ``no pronoun filters all pronouns`` () = 
    let presenter = { 
                       TalkId =  ""
                       Id =  ""
                       FirstName =  ""
                       LastName =  ""
                       FullName =  ""
                       Email =  ""
                       Url =  ""
                       Bio =  ""
                       Twitter =  ""
                       Tagline =  ""
                       Photo =  ""
                       MobileNumber =  ""
                       PreferredPronoun =  ""
                       PreferredPronounOther =  ""
                       Level =  ""
                       Experience =  ""
                       UnderRep =  ""
                       LinkedIn =  ""
                       EventYear =  "" 
                    }

    let actual = filterPresenter presenter None

    Assert.True(actual)

[<Fact>]
let ``correct pronoun filters correct pronoun`` () = 
    let presenter = { 
                       TalkId =  ""
                       Id =  ""
                       FirstName =  ""
                       LastName =  ""
                       FullName =  ""
                       Email =  ""
                       Url =  ""
                       Bio =  ""
                       Twitter =  ""
                       Tagline =  ""
                       Photo =  ""
                       MobileNumber =  ""
                       PreferredPronoun =  "He/Him"
                       PreferredPronounOther =  ""
                       Level =  ""
                       Experience =  ""
                       UnderRep =  ""
                       LinkedIn =  ""
                       EventYear =  "" 
                    }

    let actual = filterPresenter presenter (Some "He/Him")

    Assert.True(actual)