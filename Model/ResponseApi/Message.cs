namespace Cinema.Model
{
    public class Message
    {
        public  string  message_id { get; set; }
        public From  from { get; set; }
        public Chat chat { get; set; }
        public string date { get; set; }
        public string text { get; set; }
    }
}