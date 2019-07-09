namespace DDDApi

open FSharp.Azure.Storage.Table
open System

type SessionV2Status = Unapproved | Approved | Rejected

type SessionV2 =
    { [<PartitionKey>] EventYear: string
      [<RowKey>] SessionizeId: string
      Title: string
      Abstract: string
      RecommendedAudience: string
      SubmittedDateUtc: DateTime
      Status: string
      SessionLength: string
      Track: string
      Topic: string }

type Pronoun = ``He Him`` | ``She Her`` | ``They Them`` | Other | ``Not Specified``
type PresenterLevel = Graduate | Intermdiate | Senior | Leader | ``Not Specified``
type PresenterExperience = ``No Experience`` | Rarely | Occasionally | ``Semi-regular`` | Regular | ``Not Specified``

type Presenter =
     { [<PartitionKey>] TalkId: string
       [<RowKey>] Id: string
       FirstName: string
       LastName: string
       FullName: string
       Email: string
       Url: string
       Bio: string
       Twitter: string
       Tagline: string
       Photo: string
       MobileNumber: string
       PreferredPronoun: string
       PreferredPronounOther: string
       Level: string
       Experience: string
       UnderRep: string
       LinkedIn: string
       EventYear: string }

