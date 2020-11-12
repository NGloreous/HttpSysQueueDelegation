namespace AspnetframeworkHello
{
    using System.Diagnostics;
    using System.Web;

    public class HelloHandler : IHttpHandler
    {
        private readonly int pid = Process.GetCurrentProcess().Id;

        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write($"Hello world from ASP.NET Framework ({pid})");
        }
    }
}