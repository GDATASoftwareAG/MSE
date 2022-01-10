using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace SampleExchangeApi.Console.Models;

[DataContract]
public partial class Sample : IEquatable<Sample>
{
    [DataMember(Name = "Sample")]
    public string _Sample { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class Sample {\n");
        sb.Append("  _Sample: ").Append(_Sample).Append("\n");
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

        return obj.GetType() == GetType() && Equals((Sample)obj);
    }

    public bool Equals(Sample other)
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
            _Sample == other._Sample ||
            _Sample != null &&
            _Sample.Equals(other._Sample)
        );
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            var hashCode = 41;
            if (_Sample != null)
            {
                hashCode = hashCode * 59 + _Sample.GetHashCode();
            }

            return hashCode;
        }
    }

    #region Operators

#pragma warning disable 1591

    public static bool operator ==(Sample left, Sample right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Sample left, Sample right)
    {
        return !Equals(left, right);
    }

#pragma warning restore 1591

    #endregion Operators
}
