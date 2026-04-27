using System.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireFeatureAttribute(string route, string permission, bool multiBranch = false) : TypeFilterAttribute(typeof(RequireFeatureFilter))
{
    public new object[] Arguments => [route, permission, multiBranch];
}