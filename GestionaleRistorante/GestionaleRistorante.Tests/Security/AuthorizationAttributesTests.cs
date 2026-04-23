using System.Reflection;
using GestionaleRistorante.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GestionaleRistorante.Tests.Security;

public sealed class AuthorizationAttributesTests
{
    [Theory]
    [InlineData(typeof(UsersController), "99")]
    [InlineData(typeof(IngredientsController), "2,99")]
    public void ClassLevelAuthorizeRoles_AreConfigured(Type controllerType, string expectedRoles)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(expectedRoles, authorize!.Roles);
    }

    [Theory]
    [InlineData(nameof(OrdersController.GetMine), "1")]
    [InlineData(nameof(OrdersController.GetAll), "2,3,99")]
    [InlineData(nameof(OrdersController.Create), "3,99")]
    [InlineData(nameof(OrdersController.UpdateStatus), "1,2,3,99")]
    [InlineData(nameof(OrdersController.Delete), "1,3,99")]
    public void OrdersController_AuthorizeRoles_AreConfigured(string methodName, string expectedRoles)
    {
        var method = GetPublicInstanceMethod(typeof(OrdersController), methodName);
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(expectedRoles, authorize!.Roles);
    }

    [Theory]
    [InlineData(nameof(DishesController.Create), "2,99")]
    [InlineData(nameof(DishesController.Update), "2,99")]
    [InlineData(nameof(DishesController.Delete), "2,99")]
    [InlineData(nameof(DishesController.UploadImage), "2,99")]
    [InlineData(nameof(DishesController.GetRecipe), "2,99")]
    [InlineData(nameof(DishesController.ReplaceRecipe), "2,99")]
    public void DishesController_ProtectedEndpoints_AuthorizeRoles_AreConfigured(string methodName, string expectedRoles)
    {
        var method = GetPublicInstanceMethod(typeof(DishesController), methodName);
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(expectedRoles, authorize!.Roles);
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Signup))]
    [InlineData(typeof(AuthController), nameof(AuthController.Login))]
    [InlineData(typeof(AuthController), nameof(AuthController.Refresh))]
    [InlineData(typeof(DishesController), nameof(DishesController.GetAll))]
    [InlineData(typeof(DishesController), nameof(DishesController.GetById))]
    public void PublicEndpoints_HaveAllowAnonymous(Type controllerType, string methodName)
    {
        var method = GetPublicInstanceMethod(controllerType, methodName);
        var allowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.NotNull(allowAnonymous);
    }

    [Theory]
    [InlineData(nameof(AuthController.Logout))]
    [InlineData(nameof(AuthController.ChangePasswordFirstLogin))]
    public void ProtectedAuthEndpoints_RequireAuthenticationWithoutRoleRestriction(string methodName)
    {
        var method = GetPublicInstanceMethod(typeof(AuthController), methodName);
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrWhiteSpace(authorize!.Roles));
    }

    private static MethodInfo GetPublicInstanceMethod(Type type, string methodName)
    {
        return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on type '{type.FullName}'.");
    }
}
