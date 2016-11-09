using Nancy;

namespace Scrabble {
    public class HelloModule : NancyModule {
        public HelloModule()
        {
            Get["/"] = parameters => $"Hello World!";
        }
    }
}