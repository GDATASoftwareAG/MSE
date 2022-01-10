using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace SampleExchangeApi.Console.Models;

[DataContract]
public class Token : IEquatable<Token>
{
    [DataMember(Name = "token")]
    public string _Token { get; set; } = "";
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class Token {\n");
        sb.Append("  _Token: ").Append(_Token).Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((Token)obj);
    }

    public bool Equals(Token other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
        (
            _Token == other._Token ||
            _Token != null &&
            _Token.Equals(other._Token)
        );
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            // Suitable nullity checks etc, of course :)
            if (_Token != null)
            {
                hashCode = hashCode * 59 + _Token.GetHashCode();
            }

            return hashCode;
        }
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(Token left, Token right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Token left, Token right)
    {
        return !Equals(left, right);
    }

#pragma warning restore 1591

    #endregion Operators
}
