using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Filters
{
    /// <summary>
    /// Stamps a response header when the API has substituted test data for live data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>OASIS:UseTestDataWhenLiveDataNotAvailable</c> is on -- it is on in
    /// appsettings.Development.json -- a failed lookup returns plausible fabricated
    /// data with <c>IsError = false</c>. Around 198 places do this. The only trace is
    /// the phrase "(using test data)" appended to the result message, which no client
    /// reads, so a broken query, a dead provider and a healthy system all look
    /// identical from the outside.
    /// </para>
    /// <para>
    /// That is the root cause behind a run of endpoint bugs that survived for months:
    /// nothing ever looked broken. Rather than editing every call site, this filter
    /// detects the marker on the way out and exposes it as
    /// <c>X-OASIS-Test-Data: true</c> so callers can tell the difference and say so
    /// in their UI.
    /// </para>
    /// </remarks>
    public class TestDataResponseFilter : IResultFilter
    {
        private const string HeaderName = "X-OASIS-Test-Data";
        private const string Marker = "(using test data)";

        public void OnResultExecuting(ResultExecutingContext context)
        {
            try
            {
                object value = context.Result switch
                {
                    ObjectResult objectResult => objectResult.Value,
                    _ => null
                };

                if (value == null)
                    return;

                // Responses are OASISResult<T> for many different T, so the Message
                // property is read reflectively rather than by casting to a closed type.
                PropertyInfo messageProperty = value.GetType().GetProperty("Message");
                if (messageProperty == null || messageProperty.PropertyType != typeof(string))
                    return;

                if (messageProperty.GetValue(value) is not string message)
                    return;

                if (message.IndexOf(Marker, StringComparison.OrdinalIgnoreCase) < 0)
                    return;

                context.HttpContext.Response.Headers[HeaderName] = "true";
            }
            catch
            {
                // Never let a diagnostic header break a response.
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
