using System.Reflection;
using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Domain.Common;
using MediatR;
using NetArchTest.Rules;

namespace MarTech.Orders.Architecture.Tests;

public sealed class ConventionTests
{
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Domain = typeof(Entity).Assembly;

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types.InAssembly(Application)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Handlers_LiveNextToTheRequestTheyHandle()
    {
        var handlers = Application.GetTypes()
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.IsGenericType
                && (contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    || contract.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .ToArray();

        handlers.ShouldNotBeEmpty();
        handlers.ShouldAllBe(handler => handler.Name.EndsWith("Handler", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainEntities_HaveNoPublicSetters()
    {
        var offenders = Domain.GetTypes()
            .Where(type => type.IsClass && type.IsSubclassOf(typeof(Entity)))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType?.Name}.{property.Name}")
            .ToArray();

        offenders.ShouldBeEmpty();
    }
}
