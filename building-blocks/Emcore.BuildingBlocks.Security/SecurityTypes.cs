namespace Emcore.BuildingBlocks.Security;

public interface ICurrentUser { }
public class CurrentUserContext : ICurrentUser { }
public interface IOrganizationContext { }
public class OrganizationContext : IOrganizationContext { }
public interface IPermissionChecker { }
public class PermissionDecision { }
public interface IServiceIdentity { }
public class SensitiveValueMasker { }
public class AuthenticationOptions { }
public static class AuthorizationRegistrationExtensions { }
