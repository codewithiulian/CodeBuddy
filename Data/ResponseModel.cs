namespace CodeBuddy.Data
{
    public class ResponseModel
    {
        public string id { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public string text { get; set; }
        public int index { get; set; }
    }
}
