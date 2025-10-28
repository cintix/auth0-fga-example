using Dtos;


public class TestScenario
{
    public User Bob { get; } = new("Bob");
    public User Sara { get; } = new("Sara");

    public Account Account1 { get; } = new("1");
    public Account Account2 { get; } = new("2");

    public Workspace Workspace1 { get; } = new("1");
    public Workspace Workspace2 { get; } = new("2");

    public Policy Policy1A { get; } = new("1a");
    public Policy Policy1B { get; } = new("1b");

    public Policy Policy2A { get; } = new("2a");
    public Policy Policy2B { get; } = new("2b");

    public Configuration Config1A { get; } = new("1a");
    public Configuration Config1B { get; } = new("1b");
    public Configuration Config2A { get; } = new("2a");
    public Configuration Config2B { get; } = new("2b");

    // Ny helpers:
    public IEnumerable<User> GetAllUsers()
    {
        yield return Bob;
        yield return Sara;
    }

    public IEnumerable<object> GetAllResources()
    {
        yield return Account1;
        yield return Account2;

        yield return Workspace1;
        yield return Workspace2;

        yield return Policy1A;
        yield return Policy1B;
        yield return Policy2A;
        yield return Policy2B;

        yield return Config1A;
        yield return Config1B;
        yield return Config2A;
        yield return Config2B;
    }

    public IEnumerable<T> GetResourcesOfType<T>()
    {
        return GetAllResources().OfType<T>();
    }
}

public static class TestScenarioHelper
{
    public static async Task<TestScenario> SetupScenario(Auth0FgaService fgaService)
    {
        var s = new TestScenario();

        // Accounts
        await fgaService.AddAccountToUser(s.Account1, s.Bob);
        await fgaService.AddAccountToUser(s.Account2, s.Sara);

        // Workspaces
        await fgaService.AddWorkspaceToAccount(s.Workspace1, s.Account1);
        await fgaService.AddWorkspaceToAccount(s.Workspace2, s.Account2);

        // Policies
        await fgaService.AddPolicyToWorkspace(s.Policy1A, s.Workspace1);
        await fgaService.AddPolicyToWorkspace(s.Policy1B, s.Workspace1);
        await fgaService.AddPolicyToWorkspace(s.Policy2A, s.Workspace2);
        await fgaService.AddPolicyToWorkspace(s.Policy2B, s.Workspace2);


        // Configurations
        await fgaService.AddConfigurationToWorkspace(s.Config1A, s.Workspace1);
        await fgaService.AddConfigurationToWorkspace(s.Config1B, s.Workspace1);
        await fgaService.AddConfigurationToWorkspace(s.Config2A, s.Workspace2);
        await fgaService.AddConfigurationToWorkspace(s.Config2B, s.Workspace2);

        return s;
    }

    public static async Task CleanupScenario(Auth0FgaService fgaService, TestScenario s)
    {
        // Remove configurations
        await fgaService.RemoveConfigurationFromWorkspace(s.Config2B, s.Workspace2);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config2A, s.Workspace2);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config1B, s.Workspace1);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config1A, s.Workspace1);

        // Remove policies
        await fgaService.RemovePolicyFromWorkspace(s.Policy2B, s.Workspace2);
        await fgaService.RemovePolicyFromWorkspace(s.Policy2A, s.Workspace2);
        await fgaService.RemovePolicyFromWorkspace(s.Policy1B, s.Workspace1);
        await fgaService.RemovePolicyFromWorkspace(s.Policy1A, s.Workspace1);

        // Remove workspaces
        await fgaService.RemoveWorkspaceFromAccount(s.Workspace2, s.Account2);
        await fgaService.RemoveWorkspaceFromAccount(s.Workspace1, s.Account1);

        // Remove accounts
        await fgaService.RemoveAccountFromUser(s.Account2, s.Sara);
        await fgaService.RemoveAccountFromUser(s.Account1, s.Bob);
    }
}