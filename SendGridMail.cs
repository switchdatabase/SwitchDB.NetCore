using System;
using System.Collections.Generic;
using System.Text;

namespace Switch
{
    public class To
    {
        public string email { get; set; }
    }

    public class Personalization
    {
        public List<To> to { get; set; }
        public string subject { get; set; }
    }

    public class From
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class Content
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class SendGridMail
    {
        public List<Personalization> personalizations { get; set; }
        public From from { get; set; }
        public List<Content> content { get; set; }
    }
}
