using Config;
using Dtos;
using OpenFga.Sdk.Model;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

using User = Dtos.User;
using ClientConfiguration = OpenFga.Sdk.Client.ClientConfiguration;

public class Auth0FgaService(ClientConfiguration configuration) 
{
    private readonly OpenFgaClient _fgaClient = new(configuration);

    private string RoleToRelation(Role role)
    {
        return role switch
        {
            Role.Admin => "admin",
            Role.Editor => "editor",
            Role.Reader => "reader",
            Role.Reviewer => "reviewer",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private static string ToFgaDuration(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1) return $"{(int)timeSpan.TotalDays}d";
        if (timeSpan.TotalHours >= 1) return $"{(int)timeSpan.TotalHours}h";
        if (timeSpan.TotalMinutes >= 1) return $"{(int)timeSpan.TotalMinutes}m";
        return $"{(int)timeSpan.TotalSeconds}s";
    }

    private async Task WriteTuple(string user, string relation, string obj, TimeSpan duration = default)
    {
        var now = DateTime.UtcNow;
        ClientTupleKey tuple;
        if (duration != TimeSpan.Zero)
        {
            tuple = new ClientTupleKey
            {
                User = user,
                Relation = relation,
                Object = obj,
                Condition = new RelationshipCondition()
                {
                    Name = "temporary_user_grant",
                    Context = new Dictionary<string, object>
                    {
                        { "grant_time", now },
                        { "grant_duration", ToFgaDuration(duration) }
                    }
                }
            };
        }
        else
        {
            tuple = new ClientTupleKey
            {
                User = user,
                Relation = relation,
                Object = obj
            };
        }

        var writeRequest = new ClientWriteRequest { Writes = [tuple] };
        await _fgaClient.Write(writeRequest);
    }

    private async Task DeleteTuple(string user, string relation, string obj)
    {
        var tuple = new ClientTupleKeyWithoutCondition
        {
            User = user,
            Relation = relation,
            Object = obj
        };

        var deleteRequest = new ClientWriteRequest { Deletes = [tuple] };
        await _fgaClient.Write(deleteRequest);
    }

    // === Account relations ===

    public Task AddAccountToUser(Account account, User user) =>
        WriteTuple($"User:{user.Id}", "admin", $"Account:{account.Id}");

    public Task RemoveAccountFromUser(Account account, User user) =>
        DeleteTuple($"User:{user.Id}", "admin", $"Account:{account.Id}");

    public Task AddUserTo(Account account, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}");

    public Task RemoveUserFrom(Account account, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}");

    public Task AddUserTo(Account account, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}", duration);

    // === Workspace relations ===
    public Task AddWorkspaceToAccount(Workspace workspace, Account account) =>
        WriteTuple($"Account:{account.Id}", "parent", $"Workspace:{workspace.Id}");

    public Task RemoveWorkspaceFromAccount(Workspace workspace, Account account) =>
        DeleteTuple($"Account:{account.Id}", "parent", $"Workspace:{workspace.Id}");

    public Task AddUserTo(Workspace workspace, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}");

    public Task RemoveUserFrom(Workspace workspace, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}");

    public Task AddUserTo(Workspace workspace, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}", duration);

    // === Policy relations ===
    public Task AddPolicyToWorkspace(Policy policy, Workspace workspace) =>
        WriteTuple($"Workspace:{workspace.Id}", "parent", $"PpgPolicy:{policy.Id}");

    public Task RemovePolicyFromWorkspace(Policy policy, Workspace workspace) =>
        DeleteTuple($"Workspace:{workspace.Id}", "parent", $"PpgPolicy:{policy.Id}");

    public Task AddUserTo(Policy policy, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}");

    public Task RemoveUserFrom(Policy policy, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}");

    public Task AddUserTo(Policy policy, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}", duration);

    // === Configuration relations ===
    public Task AddConfigurationToWorkspace(Configuration config, Workspace workspace) =>
        WriteTuple($"Workspace:{workspace.Id}", "parent", $"CmpConfiguration:{config.Id}");

    public Task RemoveConfigurationFromWorkspace(Configuration config, Workspace workspace) =>
        DeleteTuple($"Workspace:{workspace.Id}", "parent", $"CmpConfiguration:{config.Id}");

    public Task AddUserTo(Configuration config, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}");

    public Task RemoveUserFrom(Configuration config, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}");

    public Task AddUserTo(Configuration config, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}", duration);


    public async Task<bool?> CheckAccess(User user, Role role, object obj)
    {
        try
        {
            var objectType = GetFgaObjectType(obj);
            var objectId = (obj as dynamic).Id;
            var checkRequest = new ClientCheckRequest
            {
                User = $"User:{user.Id}",
                Relation = role.ToString().ToLower(),
                Object = $"{objectType}:{objectId}",
                Context = new
                {
                    current_time = DateTime.Now
                }
            };

            var checkTask = _fgaClient.Check(checkRequest);
            var completedTask = await Task.WhenAny(checkTask, Task.Delay(10000));
            if (completedTask != checkTask)
            {
                Console.WriteLine($"Timeout checking access for {user.Id} {role} on {obj}");
                return null;
            }

            await Task.Delay(100);
            return checkTask.Result.Allowed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR checking access for {user.Id} {role} on {obj}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<(string Object, Role Role, bool Direct)>> ListResourcesForUser(User user)
    {
        var resources = new List<(string Resource, Role Role, bool Direct)>();
        var resourceTypes = new Dictionary<string, Role[]>()
        {
            { "Account", [Role.Admin, Role.Editor, Role.Reader] },
            { "Workspace", [Role.Admin, Role.Editor, Role.Reader] },
            { "PpgPolicy", [Role.Admin, Role.Editor, Role.Reader] },
            { "CmpConfiguration", [Role.Admin, Role.Editor, Role.Reader, Role.Reviewer] }
        };

        
        var crr = new ClientReadRequest
        {
            Object = $"CmpConfiguration:2a"
        };

        var response = await _fgaClient.Read(crr);
        Console.WriteLine(response);
        
        foreach (var (typeName, roles) in resourceTypes)
        {
            foreach (var role in roles)
            {
                var listRequest = new ClientListObjectsRequest
                {
                    User = $"User:{user.Id}",
                    Relation = role.ToString().ToLower(),
                    Type = typeName
                };

                var listResponse = await _fgaClient.ListObjects(listRequest);

                await Task.Delay(250);
                
                if (listResponse is { Objects.Count: > 0 })
                foreach (var resourceId in listResponse.Objects)
                {
                    var readTasks = listResponse.Objects.Select(async resourceId =>
                    {
                        var directCheckRequest = new ClientReadRequest
                        {
                            User = $"User:{user.Id}",
                            Object = $"{resourceId}"
                        };

                        var directCheckResponse = await _fgaClient.Read(directCheckRequest);
                        await Task.Delay(200);
                        bool isDirect = directCheckResponse.Tuples.Any(t =>
                            t.Key.Relation == role.ToString().ToLower() && t.Key.User == $"User:{user.Id}");

                        return ($"{typeName}:{resourceId}", role, isDirect);
                    });

                    var readResults = await Task.WhenAll(readTasks);
                    resources.AddRange(readResults);
                }

                if (listResponse.Objects.Any())
                    break;
            }
        }

        return resources;
    }

    public async Task<List<(string Object, Role Role, bool Direct)>> ListResourcesForUserByType(User user, string objectType)
    {
        var result = new List<(string, Role, bool)>();
        var roles = Enum.GetValues<Role>();

        foreach (var role in roles)
        {
            var relation = RoleToRelation(role);

            var listReq = new ClientListObjectsRequest
            {
                User = $"User:{user.Id}",
                Relation = relation,
                Type = objectType,
                Context = new { current_time = DateTime.UtcNow }
            };

            var listResp = await _fgaClient.ListObjects(listReq);

            foreach (var obj in listResp.Objects)
            {
                var readReq = new ClientReadRequest
                {
                    User = $"User:{user.Id}",
                    Relation = relation,
                    Object = obj
                };
                var direct = (await _fgaClient.Read(readReq)).Tuples.Any();

                result.Add((obj, role, direct));
            }
        }

        return result;
    }

    public async Task<List<(User User, string Relation, bool Direct)>> ListUsersWithAccess(object obj)
    {
        var result = new List<(User, string, bool)>();
        var objectType = GetFgaObjectType(obj);
        var objectId = (obj as dynamic).Id;

        var relations = new[] { "admin", "writer", "reader", "reviewer" };

        foreach (var relation in relations)
        {
            var listReq = new ClientListUsersRequest
            {
                Object = new FgaObject(objectType, objectId),
                Relation = relation
            };

            var listResp = await _fgaClient.ListUsers(listReq);

            foreach (var userRef in listResp.Users)
            {
                var userId = userRef.Userset.Id.Replace("User:", "");
                var u = new User(userId);

                var readReq = new ClientReadRequest
                {
                    User = $"User:{userId}",
                    Relation = relation,
                    Object = objectType
                };

                var readResp = await _fgaClient.Read(readReq);
                var direct = readResp.Tuples.Any();

                result.Add((u, relation, direct));
            }
        }

        return result;
    }

    public async Task ListUsersFromAccess(object obj)
    {
        var objectType = GetFgaObjectType(obj);
        var objectId = (obj as dynamic).Id;
        var request = new ClientReadRequest() 
        {
            Object = $"{objectType}:{objectId}",
        };

        var response = await _fgaClient.Read(request);
        foreach (var tuple in response.Tuples)
        {
            Console.WriteLine(ObjectFormatter.FormatProperties(tuple.Key));
        }
        
        var request2 = new ClientListUsersRequest () 
        {
            Object = new FgaObject {
                Type = objectType,
                Id = objectId
            },
            Context = new 
            {
                current_time  = DateTime.UtcNow  
            },
            Relation = RoleToRelation(Role.Reader),
            UserFilters =
            [
                new()
                {
                    Type = "User"
                }
            ]
        };

        var response2 = await _fgaClient.ListUsers(request2);
        foreach (var user in response2.Users)
        {
            Console.WriteLine(ObjectFormatter.FormatProperties(user.Object));
        }
        
        Console.WriteLine(response);
    }

    private string GetFgaObjectType<T>(T obj)
    {
        return obj switch
        {
            Policy => "PpgPolicy",
            Configuration => "CmpConfiguration",
            Workspace => "Workspace",
            Account => "Account",
            User => "User",
            _ => throw new ArgumentException("Uknown type", nameof(obj))
        };
    }
}