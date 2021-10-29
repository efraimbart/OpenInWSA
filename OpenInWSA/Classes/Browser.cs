using System;

namespace OpenInWSA.Classes
{
    public class Browser : IEquatable<Browser>
    {
        public string Name { get; init; }
        public string ProgId { get; init; }

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
        
        public bool Equals(Browser other) => 
            other is not null && (ReferenceEquals(this, other) || Name == other.Name && ProgId == other.ProgId);

        public override bool Equals(object obj) => obj is Browser other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Name, ProgId);

        public static bool operator ==(Browser rightBrowser, Browser leftBrowser) => 
            (rightBrowser is null && leftBrowser is null) || (rightBrowser?.Equals(leftBrowser) ?? false);

        public static bool operator !=(Browser rightBrowser, Browser leftBrowser) => !(rightBrowser == leftBrowser);
    }
}