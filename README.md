# SwitchDB.NetCore - the C# library for the Switch Database REST API and WebSocket API

[![NuGet Badge](https://buildstats.info/nuget/SwitchAPI.Connector)](https://www.nuget.org/packages/SwitchAPI.Connector/)

Switch API is the primary endpoint of data sevices and Switch DB's platform. You can do adding, editing, deleting or listing data works to your database with query operations by using this low-level API based on HTTP.

- API version: 1.2.1
- SDK version: 1.0.0
- Build date: 2017-06-25

<a name="frameworks-supported"></a>
## Platform support

| Platform                        | Version | Compatibility Percentage | Description                                                            |
|---------------------------------|---------|--------------------------|------------------------------------------------------------------------|
| ASP.NET Core                    | 1.0     | 100%                     |                                                                        |
| Mono                            | 2.0     | 82%                      | WebSockets and async methods not working.                              |
| Mono                            | 3.5     | 82%                      | WebSockets and async methods not working.                              |
| Mono                            | 4.0     | 96%                      | WebSockets not working.                                                |
| Mono                            | 4.5     | 100%                     |                                                                        |
| .NET Core + Platform Extensions | -       | 100%                     |                                                                        |
| .NET Core                       | 1.0     | 100%                     |                                                                        |
| .NET Core                       | 1.1     | 100%                     |                                                                        |
| .NET Core                       | 1.0     | 100%                     |                                                                        |
| .NET Standard                   | 1.0     | 80.67%                   | It is not recommended to use it on projects.                           |
| .NET Standard                   | 1.1     | 80.67%                   | It is not recommended to use it on projects.                           |
| .NET Standard                   | 1.2     | 80.67%                   | It is not recommended to use it on projects.                           |
| .NET Standard                   | 1.3     | 84.67%                   | WebSockets not working.                                                |
| .NET Standard                   | 1.4     | 84.67%                   | WebSockets not working.                                                |
| .NET Standard                   | 1.5     | 84.67%                   | WebSockets not working.                                                |
| .NET Standard                   | 1.6     | 100%                     |                                                                        |
| .NET Standard                   | 2.0     | 100%                     |                                                                        |
| .NET Framework                  | 1.1     | 65.33%                   | It is not recommended to use it on projects.                           |
| .NET Framework                  | 2.0     | 78.67%                   | It is not recommended to use it on projects.                           |
| .NET Framework                  | 3.0     | 78.67%                   | It is not recommended to use it on projects.                           |
| .NET Framework                  | 3.5     | 82%                      | It is not recommended to use it on projects.                           |
| .NET Framework                  | 4.0     | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.5     | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.5.1   | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.5.2   | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.6     | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.6.1   | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.6.2   | 96%                      | Json StringEnumConverter not working.                                  |
| .NET Framework                  | 4.7     | 96%                      | Json StringEnumConverter not working.                                  |
| Silverlight                     | 2.0     | 78.67%                   | It is not recommended to use it on projects.                           |
| Silverlight                     | 3.0     | 80%                      | It is not recommended to use it on projects.                           |
| Silverlight                     | 4.0     | 91.33%                   | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Silverlight                     | 5.0     | 94%                      | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Windows                         | 8.0     | 92%                      | MD5 issues.                                                            |
| Windows                         | 8.1     | 92%                      | MD5 issues.                                                            |
| Windows                         | 10.0    | 93.33%                   | MD5 issues.                                                            |
| Windows Phone Silverlight       | 7.0     | 79.33%                   | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Windows Phone Silverlight       | 7.1     | 80%                      | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Windows Phone Silverlight       | 8.0     | 92.67%                   | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Windows Phone Silverlight       | 8.1     | 92.67%                   | MD5, Json serialization, Json deserialization and SSL protocol errors. |
| Windows Phone                   | -       | 90.67%                   | MD5 issues.                                                            |
| Xamarin Android                 | -       | 96%                      | Json serialization and deserialization issues.                         |
| Xamarin iOS                     | -       | 96%                      | Json serialization and deserialization issues.                         |


## Installation
The library provides in NuGet.

```
Install-Package SwitchAPI.Connector
```

<a name="getting-started"></a>
## Getting Started

```csharp
using Switch;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Example
{
    public class Example
    {
        static void Main(string[] args)
        {
            SwitchDBClient db = new SwitchDBClient(
                    "ENTER_YOUR_LOCATION_CODE",
                    new DatabaseOptions
                    {
                        APIKey = "ENTER_YOUR_API_KEY",
                        APISecret = "ENTER_YOUR_API_SECRET",
                        ConnectionType = ConnectionType.HTTPS,
                        ConnectionExpire = DateTime.Now.AddDays(1)
                    }
                );
            db.Connect();
            
            // Filters & List
            var filters = new List<Where>();
            filters.Add(new Where { column = "isActive", type = WhereOperations.equal, value = true });

            List<dynamic> returnedItems = db.List(new Query { count = itemCount, list = "Users", order = new Order { by = "id", type = OrderTypes.DESC }, page = 0, where = filters });
            
            // Add
            db.Add("TestList", new Test { Name = "Lorem", Surname = "Ipsum" });
            
            // Set
            db.Set("TestList", new Test { id = "ListItemID", Name = "Ipsum", Surname = "Lorem" });
            
            // Delete
            db.Delete("TestList", "ListItemID");
            
            // SendGrid Mail Send
            SendGridMail mail = new SendGridMail();
            List<Content> mailContents = new List<Content>();
            List<Personalization> mailPersonalizations = new List<Personalization>();
            List<To> mailTo = new List<To>();

            mailContents.Add(new Content { type = "text/html", value = "<b>Your HTML mail body here</b>" });
            mailContents.Add(new Content { type = "text/plain", value = "Your Text mail body here" });

            mailTo.Add(new To { email = "person1@example.com" });
            mailTo.Add(new To { email = "person2@example.com" });
            mailTo.Add(new To { email = "person3@example.com" });

            mailPersonalizations.Add(new Personalization { subject = "Sample Mail", to = mailTo });

            mail.content = mailContents;
            mail.from = new From { email = "noreply@domain.com", name = "Acme Inc." };
            mail.personalizations = mailPersonalizations;

            SwitchDBClient.ThirdPartyServices.SendGrid.SendMail(db, mail);
            
            db.Abort();
        }
    }
}
```
 
 ## Authors

* **[Mert Sarac](https://github.com/saracmert)** - *Initial work*
