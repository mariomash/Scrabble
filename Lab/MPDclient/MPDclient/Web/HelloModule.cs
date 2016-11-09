using Nancy;

namespace MPDclient.Web {
    public class HelloModule : NancyModule {
        public HelloModule()
        {
            Get["/"] = parameters => $"Hello World!";
        }
    }
}