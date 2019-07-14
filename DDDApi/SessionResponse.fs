namespace DDDApi

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

type ResponsePresenter =
    { FirstName: string
      LastName: string
      Url: string
      Bio: string
      Twitter: string
      Tagline: string
      Photo: string
      PreferredPronoun: string
      LinkedIn: string }

[<DataContract>]
type ResponseSessionV2 =
    { [<field: DataMember(Name="SessionId")>]Id: string
      [<field: DataMember(Name="SessionTitle")>]SessionTitle: string
      [<field: DataMember(Name="SessionAbstract")>]SessionAbstract: string
      [<field: DataMember(Name="RecommendedAudience")>]RecommendedAudience: string
      [<field: DataMember(Name="Year")>]Year: string
      [<field: DataMember(Name="TrackType")>]TrackType: string
      [<field: DataMember(Name="SessionLength")>]SessionLength: string
      [<field: DataMember(Name="Presenters")>]Presenters: ResponsePresenter array
      [<field: DataMember(Name="Tags")>]Tags: string array }

module ResponseSessionMapper =
  let sessionToResult (session: Session) = { Id = session.RowKey;
                                             SessionTitle = session.SessionTitle;
                                             SessionAbstract = session.SessionAbstract;
                                             PresenterName = session.PresenterName;
                                             PresenterBio = session.PresenterBio;
                                             RecommendedAudience = session.RecommendedAudience;
                                             PresenterTwitterAlias = session.PresenterTwitterAlias;
                                             PresenterWebsite = session.PresenterWebsite
                                             Year = session.PartitionKey.Replace("Session-", "");
                                             SessionLength = session.SessionLength;
                                             TrackType = session.TrackType }

  let sessionV2ToResult (session: SessionV2) (presenters: seq<Presenter>) =
      { Id = session.SessionizeId
        SessionTitle = session.Title
        SessionAbstract = session.Abstract
        RecommendedAudience = session.RecommendedAudience
        Year = session.EventYear
        SessionLength = session.SessionLength
        TrackType = session.Track
        Tags = match session.Topic with
               | null -> [||]
               | _ -> session.Topic.Split(',') |> Array.map(fun s -> s.Trim())
        Presenters = presenters
                     |> Seq.map(fun p ->
                        { FirstName = p.FirstName
                          LastName = p.LastName
                          Url = p.Url
                          Bio = p.Bio
                          Twitter = p.Twitter
                          Tagline = p.Tagline
                          Photo = p.Photo
                          PreferredPronoun = p.PreferredPronoun
                          LinkedIn = p.LinkedIn })
                     |> Seq.toArray}