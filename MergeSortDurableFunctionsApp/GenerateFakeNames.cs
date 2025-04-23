
using Microsoft.Extensions.Logging;
using Bogus;
using Microsoft.Azure.Functions.Worker;

namespace DurableMergeSortApp
{

    public static class GenerateFakeNames
    {
        [Function("GenerateFakeNames")]
        public static List<string> Run(
            [ActivityTrigger] int count, FunctionContext context)
        {
            var logger = context.GetLogger("GenerateFakeNames");
            logger.LogInformation($"Generating {count} fake names...");

            var faker = new Faker();
            var names = new List<string>();

            for (int i = 0; i < count; i++)
            {
                names.Add(faker.Name.FullName());
            }

            logger.LogInformation("Fake name generation complete.");
            return names;
        }
    }
}
