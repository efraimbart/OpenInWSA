namespace OpenInWSA.Classes
{
    public class Browser
    {
        public string Name { get; set; }
        public string ProgId { get; set; }

        public Browser()
        {
        }
            
        public Browser(string browser)
        {
            var protocolParts = browser.Split(',');

            Name = protocolParts[0];
            ProgId = protocolParts[1];
        }

        public override string ToString()
        {
            return $@"{Name},{ProgId}";
        }
    }
}