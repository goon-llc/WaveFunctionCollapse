using System.ComponentModel;
using System.Xml.Linq;

namespace WFCTests;

public static class XElementExtensions
{
  public static T Get<T>( this XElement xElement, string attribute, T defaultT = default )
  {
    XAttribute a = xElement.Attribute( attribute );
    return a == null ? defaultT : ( T )TypeDescriptor.GetConverter( typeof(T) ).ConvertFromInvariantString( a.Value );
  }
}