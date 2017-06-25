using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Switch
{
    public class QueryBuilder
    {
        public static string Build(Query query)
        {
            bool IsValidQuery = !string.IsNullOrEmpty(query.list) && null != query.where && null != query.order;

            if (IsValidQuery)
            {
                return JsonConvert.SerializeObject(query);
            }
            else
            {
                throw new Exception("Query not valid.");
            }
        }
    }

    public class Query
    {
        public string list { get; set; }
        public int count { get; set; } = -1;
        public int page { get; set; } = 0;
        [JsonConverter(typeof(StringEnumConverter))]
        public WhereTypes whereType { get; set; } = WhereTypes.AND;
        public List<Where> where { get; set; }
        public Order order { get; set; }
    }

    public class Where
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public WhereOperations type { get; set; }
        public string column { get; set; }
        public object value { get; set; }
    }

    public class Order
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderTypes type { get; set; }
        public string by { get; set; }
    }

    public enum OrderTypes
    {
        ASC, DESC
    }

    public enum WhereOperations
    {
        equal, notEqual, like, greaterThan, lessThan
    }

    public enum WhereTypes
    {
        AND, OR
    }
}