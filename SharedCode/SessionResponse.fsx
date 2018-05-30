#r "System.Runtime.Serialization"
open System.Runtime.Serialization

[<DataContract>]
type ResponseSession =
    { [<field: DataMember(Name="SessionId")>]Id: string
      [<field: DataMember(Name="SessionTitle")>]SessionTitle: string
      [<field: DataMember(Name="SessionAbstract")>]SessionAbstract: string
      [<field: DataMember(Name="PresenterName")>]PresenterName: string
      [<field: DataMember(Name="PresenterBio")>]PresenterBio: string
      [<field: DataMember(Name="PresenterTwitterAlias")>]PresenterTwitterAlias: string
      [<field: DataMember(Name="PresenterWebsite")>]PresenterWebsite: string
      [<field: DataMember(Name="RecommendedAudience")>]RecommendedAudience: string
      [<field: DataMember(Name="Year")>]Year: string
      [<field: DataMember(Name="TrackType")>]TrackType: string
      [<field: DataMember(Name="SessionLength")>]SessionLength: string }