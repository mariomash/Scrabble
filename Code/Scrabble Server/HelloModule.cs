using Nancy;

namespace Scrabble_Server {
    public class HelloModule : NancyModule {
        public HelloModule()
        {
            Get["/"] = parameters => $"Hello World!";
        }
    }
}