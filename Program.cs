using OpenFga.Sdk.Client;
using Dtos;
using Microsoft.Extensions.Configuration;
using OpenFga.Sdk.Exceptions;

class Program
{
    static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<Program>()                           
            .Build();

        var clientConfiguration = new Config.ClientConfiguration();
        configuration.GetSection("ClientConfiguration").Bind(clientConfiguration);

        var fgaConfig = new ClientConfiguration
        {
            ApiUrl = clientConfiguration.ApiUrl,
            StoreId = clientConfiguration.StoreId,
            AuthorizationModelId = clientConfiguration.AuthorizationModelId,
            Credentials = new OpenFga.Sdk.Configuration.Credentials
            {
                Method = OpenFga.Sdk.Configuration.CredentialsMethod.ClientCredentials,
                Config = new OpenFga.Sdk.Configuration.CredentialsConfig
                {
                    ApiTokenIssuer = clientConfiguration.Credentials.Config.ApiTokenIssuer,
                    ApiAudience = clientConfiguration.Credentials.Config.ApiAudience,
                    ClientId = clientConfiguration.Credentials.Config.ClientId,
                    ClientSecret = clientConfiguration.Credentials.Config.ClientSecret
                }
            }
        };

        Auth0FgaService fgaService = new Auth0FgaService(fgaConfig);

        await TestUserApplyingSameRoleTwice(fgaService);
    }

    private static async Task TestUserApplyingSameRoleTwice(Auth0FgaService fgaService)
    {
        var scenario = await TestScenarioHelper.SetupScenario(fgaService);
        try
        {
            // Give Bob temporary access to Account2 as an Editor   
            await AddTemporaryUserTo(fgaService, scenario.Account2, scenario.Bob, Role.Editor, TimeSpan.FromSeconds(2));

            // Validate that Bob has the access 
            bool? hasAccessNow = await fgaService.CheckAccess(scenario.Bob, Role.Editor, scenario.Account2);
            Console.WriteLine($"Immediate access: {ToResultSymbol(hasAccessNow)}");

            //Wait for the access to run out
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Validate that Bob no longer has the access 
            hasAccessNow = await fgaService.CheckAccess(scenario.Bob, Role.Editor, scenario.Account2);
            Console.WriteLine($"Immediate access: {ToResultSymbol(hasAccessNow)}");

            try
            {
                // Once again let's give Bob temporary access to Account2 as an Editor   
                // This will as expected give me an error since the record is there, but the access has run out.
                await AddTemporaryUserTo(fgaService, scenario.Account2, scenario.Bob, Role.Editor,
                    TimeSpan.FromSeconds(10));
            }
            catch (FgaApiValidationError apiValidationError)
            {
                string errorCode = apiValidationError.ApiError.ErrorCode;
                string errorMessage = apiValidationError.ApiError.Message;
                
                Console.WriteLine($@"
-------------------------
   API Validation Error
-------------------------
Error Code : {errorCode}
Message    : {errorMessage}
");
                
                // With the errorCode being "write_failed_due_to_invalid_input" a very common error
                // Then we are forced to parse the errorMessage to catch that the error is a Duplicate
                if (errorMessage.IndexOf("tuple to be written already existed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("Duplicate tuple detected.");
                    
                    // Now we know we have to delete the old tuple/record first
                    await RemoveTemporaryUserFrom(fgaService, scenario.Account2, scenario.Bob, Role.Editor);
                }
                
            }
        }
        finally
        {
            await TestScenarioHelper.CleanupScenario(fgaService, scenario);
        }
    }

        private static async Task AddTemporaryUserTo(Auth0FgaService fgaService, object resource, User user, Role role,
            TimeSpan duration)
        {
            switch (resource)
            {
                case Account a: await fgaService.AddUserTo(a, user, role, duration); break;
                case Workspace w: await fgaService.AddUserTo(w, user, role, duration); break;
                case Policy p: await fgaService.AddUserTo(p, user, role, duration); break;
                case Configuration c: await fgaService.AddUserTo(c, user, role, duration); break;
                default: throw new ArgumentException("Unknown resource type");
            }
        }

        private static async Task RemoveTemporaryUserFrom(Auth0FgaService fgaService, object resource, User user,
            Role role)
        {
            switch (resource)
            {
                case Account a: await fgaService.RemoveUserFrom(a, user, role); break;
                case Workspace w: await fgaService.RemoveUserFrom(w, user, role); break;
                case Policy p: await fgaService.RemoveUserFrom(p, user, role); break;
                case Configuration c: await fgaService.RemoveUserFrom(c, user, role); break;
                default: throw new ArgumentException("Unknown resource type");
            }
        }

        private static string ToResultSymbol(bool? value)
        {
            if (value == true)
                return "\u001b[32mTrue\u001b[0m";
            if (value == false)
                return "\u001b[31mFalse\u001b[0m";
            return "\u001b[33m?\u001b[0m";
        }
    }