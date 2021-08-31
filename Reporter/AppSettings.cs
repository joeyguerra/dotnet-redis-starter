namespace Reporter {
    public class AppSettings{
        public AppSettings(){}
        public string RedisHost {get;set;}
        public string RedisPassword {get;set;}
        public string ConsumerGroupId {get;set;}
        public string ConsumerGroup {get;set;}
        public string StreamKey {get;set;}
    }
}