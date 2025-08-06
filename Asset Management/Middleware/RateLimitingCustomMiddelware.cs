using System.Collections.Concurrent;

namespace Asset_Management.Middleware
{
    public class RateLimitingCustomMiddelware
    {
        private readonly RequestDelegate _next;
        //stores UserKey(IP) -> Time when user made a request in _lastcall 
        private static readonly ConcurrentDictionary<string, DateTime> _lastcall = new ConcurrentDictionary<string, DateTime>();
        public RateLimitingCustomMiddelware(RequestDelegate next)
        {
            _next = next;

        }
        public async Task InvokeAsync(HttpContext context)
        {
            
            if ((context.Request.Method == "POST" && context.Request.Path.StartsWithSegments("/api/AssetHierarchy")))
            {
                string UserKey = context.Connection.RemoteIpAddress.ToString(); //store IP
                if(_lastcall.TryGetValue(UserKey, out var lastCall))
                {
                    // If request is made under 1 minute from last call don't add the node.
                    if(DateTime.UtcNow-lastCall < TimeSpan.FromMinutes(1))
                    {
                        await context.Response.WriteAsync("Please wait one minute before making a request");
                        return;


                    }
                }
                _lastcall[UserKey] = DateTime.UtcNow;


            }
            await _next(context);
        }
    }
}
