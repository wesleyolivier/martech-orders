using System.Reflection;
using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace MarTech.Orders.Architecture.Tests;

public sealed class LayeringTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Application.GetName().Name, Infrastructure.GetName().Name, Api.GetName().Name)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Domain_DoesNotDependOnInfrastructureConcerns()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "Microsoft.AspNetCore", "FluentValidation")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure.GetName().Name, Api.GetName().Name)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Application_DoesNotDependOnEntityFrameworkCore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn(typeof(DbContext).Namespace)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(Api.GetName().Name)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Controllers_DoNotTouchPersistenceDirectly()
    {
        var result = Types.InAssembly(Api)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure.GetName().Name, typeof(DbContext).Namespace)
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }
}
