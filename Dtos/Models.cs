namespace Dtos;

public enum Role
{
    Admin,
    Editor,
    Reader,
    Reviewer
}

public class User(string name)
{
    public string Id { get; set; } = name;
}

public class Account(string accountId)
{
    public string Id { get; set; } = accountId;
}

public class Workspace(string workspaceId)
{
    public string Id { get; set; } = workspaceId;
}

public class Policy(string policyId)
{
    public string Id { get; set; } = policyId;
}

public class Configuration(string configId)
{
    public string Id { get; set; } = configId;
}